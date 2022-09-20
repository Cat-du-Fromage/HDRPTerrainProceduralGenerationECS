using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace ProjectDawn.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unmanaged, resizable aabb tree, without any thread safety check features.
    /// AABB tree (short for axis aligned bounding box tree) is a space-partitioning data structure for organizing bounding shapes in space.
    /// As structure uses generic it is not only usable for boxes, but any shape that implements interfaces.
    /// AABB trees are a useful data structure for fast searching bounding shapes in space.
    /// AABB trees are a special case of binary space partitioning trees.
    /// Based on https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf.
    /// </summary>
    /// <typeparam name="T">The type of the bounding shape.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeAABBTree<T>
        : IDisposable
        where T : unmanaged, ISurfaceArea<T>, IUnion<T>
    {
        UnsafeList<Node> m_Nodes;
        UnsafeStack<int> m_FreeHandles;
        int m_Length;
        int m_RootHandle;

        /// <summary>
        /// Whether the tree is empty.
        /// </summary>
        /// <value>True if the tree is empty or the tree has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length => m_Length;

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        public int Capacity => m_Nodes.Capacity;

        /// <summary>
        /// Whether this tree has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this tree has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Nodes.IsCreated && m_FreeHandles.IsCreated;

        /// <summary>
        /// Returns the tree root.
        /// </summary>
        public Handle Root => new Handle(m_RootHandle);

        /// <summary>
        /// The element at a given position.
        /// </summary>
        /// <param name="handle">Handle of the element.</param>
        public T this[Handle handle]
        {
            get
            {
                CheckHandle(handle);
                return (m_Nodes.Ptr + handle)->Value;
            }
        }

        /// <summary>
        /// Initialized and returns an instance of NativeAABBTree.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeAABBTree(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            m_Length = 0;
            m_RootHandle = Node.Null;
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public static UnsafeAABBTree<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeAABBTree<T>* data = AllocatorManager.Allocate<UnsafeAABBTree<T>>(allocator);

            data->m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            data->m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            data->m_Length = 0;
            data->m_RootHandle = Node.Null;

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeAABBTree<T>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->m_Nodes.Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Builds aabb tree with given array of values.
        /// </summary>
        /// <param name="value">The array of values.</param>
        public void Build(in NativeArray<T> array)
        {
            if (!IsEmpty)
                Clear();

            var branches = new UnsafePriorityQueue<Branch, BranchComparer>(array.Length, Allocator.Temp);
            for (int i = 0; i < array.Length; ++i)
            {
                var value = array[i];

                // Create root if it does not exist
                if (m_RootHandle == Node.Null)
                {
                    m_RootHandle = Allocate(value);
                    m_Length = 1;
                    Node* rootNode = m_Nodes.Ptr + m_RootHandle;
                    rootNode->ParentHandle = Node.Null;
                    rootNode->LeftChildHandle = Node.Null;
                    rootNode->RightChildHandle = Node.Null;
                    continue;
                }

                // Stage 1: find the best sibling for the new leaf
                // This handle will have the lowest cost to insert new value
                int bestHandle = FindBestHandle(value, ref branches);
                //int bruteForceBestHandle = FindBestHandleBruteForce(value);
                //Assert.AreEqual(GetCost(value, bestHandle), GetCost(value, bruteForceBestHandle));

                var newLeafHandle = Allocate(value);
                var newParentHandle = Allocate(Union(m_Nodes[bestHandle].Value, value));

                Node* bestNode = m_Nodes.Ptr + bestHandle;
                Node* newLeafNode = m_Nodes.Ptr + newLeafHandle;
                Node* newParentNode = m_Nodes.Ptr + newParentHandle;

                // Stage 2: create a new parent
                // Add newParentNode and set their childs bestNode and newLeafNode
                if (bestHandle == m_RootHandle)
                {
                    m_RootHandle = newParentHandle;
                    newParentNode->ParentHandle = Node.Null;
                }
                else
                {
                    int grandParentHandle = bestNode->ParentHandle;
                    Node* grandParentNode = m_Nodes.Ptr + grandParentHandle;

                    // Update grand parent connections
                    if (grandParentNode->LeftChildHandle == bestHandle)
                        grandParentNode->LeftChildHandle = newParentHandle;
                    else
                        grandParentNode->RightChildHandle = newParentHandle;

                    newParentNode->ParentHandle = grandParentHandle;

                    // Stage 3: walk back up the tree refitting AABBs
                    int handle = grandParentHandle;
                    while (handle != Node.Null)
                    {
                        Node* node = m_Nodes.Ptr + handle;
                        node->Value = Union(m_Nodes.Ptr[node->LeftChildHandle].Value, m_Nodes.Ptr[node->RightChildHandle].Value);

                        // TODO: Add self balancing here by rotating tree
                        // https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf

                        handle = node->ParentHandle;
                    }
                }

                // Update new parent connections
                newParentNode->LeftChildHandle = bestHandle;
                newParentNode->RightChildHandle = newLeafHandle;

                bestNode->ParentHandle = newParentHandle;

                // Update new leaf connections
                newLeafNode->ParentHandle = newParentHandle;
                newLeafNode->LeftChildHandle = Node.Null;
                newLeafNode->RightChildHandle = Node.Null;

                m_Length++;

            }
            branches.Dispose();
        }

        /// <summary>
        /// Add element to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public Handle Add(T value)
        {
            // Create root if it does not exist
            if (m_RootHandle == Node.Null)
            {
                m_RootHandle = Allocate(value);
                m_Length = 1;
                Node* rootNode = m_Nodes.Ptr + m_RootHandle;
                rootNode->ParentHandle = Node.Null;
                rootNode->LeftChildHandle = Node.Null;
                rootNode->RightChildHandle = Node.Null;
                return new Handle(m_RootHandle);
            }

            // Stage 1: find the best sibling for the new leaf
            // This handle will have the lowest cost to insert new value
            var branches = new UnsafePriorityQueue<Branch, BranchComparer>(Length, Allocator.Temp);
            int bestHandle = FindBestHandle(value, ref branches);
            branches.Dispose();
            //int bruteForceBestHandle = FindBestHandleBruteForce(value);
            //Assert.AreEqual(GetCost(value, bestHandle), GetCost(value, bruteForceBestHandle));

            var newLeafHandle = Allocate(value);
            var newParentHandle = Allocate(Union(m_Nodes[bestHandle].Value, value));

            Node* bestNode = m_Nodes.Ptr + bestHandle;
            Node* newLeafNode = m_Nodes.Ptr + newLeafHandle;
            Node* newParentNode = m_Nodes.Ptr + newParentHandle;

            // Stage 2: create a new parent
            // Add newParentNode and set their childs bestNode and newLeafNode
            if (bestHandle == m_RootHandle)
            {
                m_RootHandle = newParentHandle;
                newParentNode->ParentHandle = Node.Null;
            }
            else
            {
                int grandParentHandle = bestNode->ParentHandle;
                Node* grandParentNode = m_Nodes.Ptr + grandParentHandle;

                // Update grand parent connections
                if (grandParentNode->LeftChildHandle == bestHandle)
                    grandParentNode->LeftChildHandle = newParentHandle;
                else
                    grandParentNode->RightChildHandle = newParentHandle;

                newParentNode->ParentHandle = grandParentHandle;

                // Stage 3: walk back up the tree refitting AABBs
                int handle = grandParentHandle;
                while (handle != Node.Null)
                {
                    Node* node = m_Nodes.Ptr + handle;
                    node->Value = Union(m_Nodes.Ptr[node->LeftChildHandle].Value, m_Nodes.Ptr[node->RightChildHandle].Value);

                    // TODO: Add self balancing here by rotating tree
                    // https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf

                    handle = node->ParentHandle;
                }
            }

            // Update new parent connections
            newParentNode->LeftChildHandle = bestHandle;
            newParentNode->RightChildHandle = newLeafHandle;

            bestNode->ParentHandle = newParentHandle;

            // Update new leaf connections
            newLeafNode->ParentHandle = newParentHandle;
            newLeafNode->LeftChildHandle = Node.Null;
            newLeafNode->RightChildHandle = Node.Null;

            m_Length++;

            return new Handle(newLeafHandle);
        }

        /// <summary>
        /// Removes node from the tree at givent iterator value.
        /// </summary>
        /// <param name="iterator">Position from which node will be removed.</param>
        public void RemoveAt(Handle iterator)
        {
            int handle = iterator;
            CheckHandle(handle);
            CheckLeafHandle(handle);
            Node* node = m_Nodes.Ptr + handle;

            // Remove root if node is root
            if (handle == m_RootHandle)
            {
                // Only leaf removing is allowed as result removing root will indicate it is last node
                m_RootHandle = Node.Null;
                m_Length = 0;
                Free(handle);
                return;
            }

            int parentHandle = node->ParentHandle;
            Node* parentNode = m_Nodes.Ptr + parentHandle;

            // Find other parent child
            int siblingHandle = parentNode->LeftChildHandle == handle ? parentNode->RightChildHandle : parentNode->LeftChildHandle;
            Node* siblingNode = m_Nodes.Ptr + siblingHandle;

            // Remove parentNode and node
            if (parentHandle == m_RootHandle)
            {
                m_RootHandle = siblingHandle;
                siblingNode->ParentHandle = Node.Null;
            }
            else
            {
                int grandParentHandle = parentNode->ParentHandle;
                Node* grandParentNode = m_Nodes.Ptr + grandParentHandle;

                // Update grand parent connections
                if (grandParentNode->LeftChildHandle == parentHandle)
                    grandParentNode->LeftChildHandle = siblingHandle;
                else
                    grandParentNode->RightChildHandle = siblingHandle;

                siblingNode->ParentHandle = grandParentHandle;
            }

            Free(handle);
            Free(parentHandle);

            m_Length--;
        }

        /// <summary>
        /// Find all bounding shapes that overlap with value.
        /// </summary>
        /// <param name="value">Value that will be used for testing overlap.</param>
        /// <param name="result">Array of the bounding shapes that overlap.</param>
        /// <typeparam name="U"></typeparam>
        /// <returns>Returns the number of bounding shapes overlap.</returns>
        public int FindOverlap<U>(in U value, ref NativeList<T> result)
            where U : unmanaged, IOverlap<T>
        {
             if (!result.IsCreated && result.Capacity == 0)
                throw new InvalidOperationException("FindOverlapping result array must be created and with non zero capacity!");
            result.Clear();

            if (IsEmpty)
                return 0;

            FindOverlapRecursive(Root, value, ref result);
            return result.Length;
        }

        void FindOverlapRecursive<U>(int handle, in U value, ref NativeList<T> result)
            where U : unmanaged, IOverlap<T>
        {
            Node* node = m_Nodes.Ptr + handle;

            if (!value.Overlap(node->Value))
                return;

            if (node->IsLeaf)
            {
                if (result.Length != result.Capacity)
                    result.Add(node->Value);
            }
            else
            {
                FindOverlapRecursive(node->LeftChildHandle, value, ref result);
                FindOverlapRecursive(node->RightChildHandle, value, ref result);
            }
        }

        /// <summary>
        /// Returns the sum of all non leaf surface area.
        /// The lower the number is, the more optimal a tree will be.
        /// </summary>
        public float Cost()
        {
            float cost = 0;

            for (int handle = 0; handle < m_Nodes.Length; ++handle)
            {
                Node* leafNode = m_Nodes.Ptr + handle;
                if (leafNode->IsFree)
                    continue;

                if (leafNode->IsLeaf)
                    continue;

                cost += leafNode->Value.SurfaceArea();
            }

            return cost;
        }

        /// <summary>
        /// Returns parents fo the handle/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Handle Parent(Handle handle)
        {
            CheckHandle(handle);
            return new Handle(m_Nodes[handle].ParentHandle);
        }

        /// <summary>
        /// Returns left child of handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Handle Left(Handle handle)
        {
            CheckHandle(handle);
            return new Handle(m_Nodes[handle].LeftChildHandle);
        }

        /// <summary>
        /// Returns right child of handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Handle Right(Handle handle)
        {
            CheckHandle(handle);
            return new Handle(m_Nodes[handle].RightChildHandle);
        }

        /// <summary>
        /// Returns true if handle does not have childs.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLeaf(Handle handle)
        {
            CheckHandle(handle);
            return m_Nodes[handle].IsLeaf;
        }

        /// <summary>
        /// Removes all nodes of this tree.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Nodes.Clear();
            m_FreeHandles.Clear();
            m_Length = 0;
            m_RootHandle = Node.Null;
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            m_Nodes.Dispose();
            m_FreeHandles.Dispose();
        }

        int FindBestHandleBruteForce(T value)
        {
            int bestHandle = Node.Null;
            float minSurfaceArea = float.MaxValue;

            for (int handle = 0; handle < m_Nodes.Length; ++handle)
            {
                Node* leafNode = m_Nodes.Ptr + handle;
                if (leafNode->IsFree)
                    continue;

                float surfaceArea = GetCost(value, handle);

                if (surfaceArea < minSurfaceArea)
                {
                    minSurfaceArea = surfaceArea;
                    bestHandle = handle;
                }
            }

            return bestHandle;
        }

        float GetCost(T value, int handle)
        {
            Node* leafNode = m_Nodes.Ptr + handle;
            if (leafNode->IsFree)
                throw new Exception();

            float directCost = SurfaceArea(leafNode->Value, value);

            float inheritedCost = InheritedCost(leafNode->ParentHandle, value);

            return directCost + inheritedCost;
        }

        /// <summary>
        /// Returns handle that would be best to insert this new value.
        /// Inserting to this handle will have the lowest <see cref="Cost"/> compared to other leaf nodes.
        /// </summary>
        int FindBestHandle(T value, ref UnsafePriorityQueue<Branch, BranchComparer> branches)
        {
            // Add as first root
            int bestHandle = m_RootHandle;
            float bestCost = float.MaxValue;

            if (Length == 1)
                return bestHandle;

            Assert.IsTrue(branches.IsEmpty);

            branches.Enqueue(new Branch(bestHandle, bestCost));
            while (branches.TryDequeue(out Branch branch))
            {
                int handle = branch.Handle;
                Node* node = m_Nodes.Ptr + handle;

                Assert.IsFalse(node->IsFree);

                // The direct cost is the surface area of the new internal node that will be created for the siblings
                float directCost = SurfaceArea(node->Value, value);

                // The inherited cost is the increased surface area caused by refitting the ancestor�s boxes
                float inheritedCost = InheritedCost(node->ParentHandle, value);

                // Here is the cost of inserting this new value
                float cost = directCost + inheritedCost;

                // Update best node
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestHandle = handle;
                }

                if (!node->IsLeaf)
                {
                    // Compare lower bound cost with best cost to find out if its worth to check child nodes
                    float lowerBoundCost = value.SurfaceArea() + DeltaSurfaceArea(node->Value, value) + inheritedCost;
                    if (lowerBoundCost < bestCost)
                    {
                        branches.Enqueue(new Branch(node->LeftChildHandle, lowerBoundCost));
                        branches.Enqueue(new Branch(node->RightChildHandle, lowerBoundCost));
                    }
                }
            }
            return bestHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float InheritedCost(int handle, T value)
        {
            float cost = 0;
            while (handle != Node.Null)
            {
                Node* node = m_Nodes.Ptr + handle;
                cost += DeltaSurfaceArea(node->Value, value);
                handle = node->ParentHandle;
            }
            return cost;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float DeltaSurfaceArea(T a, T b) => a.Union(b).SurfaceArea() - a.SurfaceArea();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float SurfaceArea(T a, T b) => a.Union(b).SurfaceArea();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Union(T a, T b) => a.Union(b);

        /// <summary>
        /// Returns new allocated node handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Allocate(in T value)
        {
            int handle;
            if (m_FreeHandles.TryPop(out handle))
            {
                m_Nodes[handle] = new Node
                {
                    Value = value,
                };
            }
            else
            {
                handle = m_Nodes.Length;
                m_Nodes.Add(new Node
                {
                    Value = value,
                });
            }

            return handle;
        }

        /// <summary>
        /// Releases node with given handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Free(int nodeHandle)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Nodes.Ptr[nodeHandle].IsFree = true;
#endif
            m_FreeHandles.Push(nodeHandle);
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckLeafHandle(int handle)
        {
            if (!m_Nodes[handle].IsLeaf)
                throw new ArgumentException($"Handle referencing {handle} is not leaf. Only leafs can be removed.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckHandle(int handle)
        {
            if (handle > m_Nodes.Length || handle < 0)
                throw new ArgumentException($"Handle is not valid with {handle}.");
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Nodes[handle].IsFree)
                throw new ArgumentException($"Handle referencing {handle} that is already removed.");
#endif
        }

        /// <summary>
        /// Kd Tree iterator.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("{m_Handle}")]
        public struct Handle : IEquatable<Handle>
        {
            int m_Handle;

            /// <summary>
            /// Returns true if handle is valid.
            /// </summary>
            public bool Valid => m_Handle != Node.Null;

            internal Handle(int handle)
            {
                m_Handle = handle;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode() => m_Handle;

            public bool Equals(Handle other) => m_Handle == other.m_Handle;

            public static implicit operator int(Handle handled) => handled.m_Handle;
            public static bool operator ==(Handle lhs, Handle rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Handle lhs, Handle rhs) => lhs.m_Handle != rhs.m_Handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct Node
        {
            public T Value;
            public int ParentHandle;
            public int LeftChildHandle;
            public int RightChildHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public bool IsFree;
#endif
            public bool IsLeaf => LeftChildHandle == Null;

            public static int Null => -1;
        }

        struct Branch
        {
            public float Cost;
            public int Handle;

            public Branch(int handle, float cost)
            {
                Handle = handle;
                Cost = cost;
            }
        }

        unsafe struct BranchComparer : IComparer<Branch>
        {
            public int Compare(Branch x, Branch y) => x.Cost.CompareTo(y.Cost);
        }
    }
}
