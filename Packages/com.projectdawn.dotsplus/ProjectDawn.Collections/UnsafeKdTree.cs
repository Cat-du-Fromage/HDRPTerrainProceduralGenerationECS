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
    /// An unmanaged, resizable k-d tree, without any thread safety check features.
    /// K-d tree (short for k-dimensional tree) is a space-partitioning data structure for organizing points in a k-dimensional space.
    /// K-d trees are a useful data structure for several applications, such as searches involving a multidimensional search key (e.g. range searches and nearest neighbor searches) and creating point clouds. 
    /// K-d trees are a special case of binary space partitioning trees.
    /// </summary>
    /// <typeparam name="TValue">The type of the elements.</typeparam>
    /// <typeparam name="TComparer">The type of the comparer for sorting elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeKdTree<TValue, TComparer>
        : IDisposable
        where TValue : unmanaged
        where TComparer : unmanaged, IKdTreeComparer<TValue>
    {
        UnsafeList<Node> m_Nodes;
        UnsafeStack<int> m_FreeHandles;
        TComparer m_Comparer;
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
        public TValue this[Handle handle]
        {
            get
            {
                CheckHandle(handle);
                return (m_Nodes.Ptr + handle)->Value;
            }
            set
            {
                CheckHandle(handle);
                (m_Nodes.Ptr + handle)->Value = value;
            }
        }

        /// <summary>
        /// Initialized and returns an instance of NativeKdTree.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public UnsafeKdTree(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer = default)
        {
            m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            m_Comparer = comparer;
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
        public static UnsafeKdTree<TValue, TComparer>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer = default)
        {
            UnsafeKdTree<TValue, TComparer>* data = AllocatorManager.Allocate<UnsafeKdTree<TValue, TComparer>>(allocator);

            data->m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            data->m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            data->m_Comparer = comparer;
            data->m_Length = 0;
            data->m_RootHandle = Node.Null;

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeKdTree<TValue, TComparer>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->m_Nodes.Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Builds balanced tree from elements.
        /// </summary>
        /// <param name="values">The values that will be used for building tree.</param>
        public void Build(NativeArray<TValue> values)
        {
            if (!IsEmpty)
                Clear();

            var sortedValues = new NativeArray<TValue>(values.Length, Allocator.Temp);
            sortedValues.CopyFrom(values);

            m_RootHandle = BuildRecursive(sortedValues, Node.Null, 0);

            sortedValues.Dispose();
        }

        /// <summary>
        /// Add element to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public Handle Add(TValue value)
        {
            var newHandle = Allocate(value, Node.Null, Node.Null, Node.Null, 0);
            AddHandle(newHandle);
            return new Handle(newHandle);
        }

        /// <summary>
        /// Removes node from the tree at givent iterator value.
        /// </summary>
        /// <param name="iterator">Position from which node will be removed.</param>
        public void RemoveAt(Handle iterator)
        {
            int handle = iterator;
            Node* node = m_Nodes.Ptr + handle;

            // Update connections
            if (node->ParentHandle != Node.Null)
            {
                Node* parentNode = m_Nodes.Ptr + node->ParentHandle;
                if (parentNode->LeftChildHandle == handle)
                    parentNode->LeftChildHandle = Node.Null;
                if (parentNode->RightChildHandle == handle)
                    parentNode->RightChildHandle = Node.Null;
            }
            else
            {
                // It is a root
                Assert.AreEqual(handle, m_RootHandle);
                m_RootHandle = Node.Null;
            }

            // Free the actual node
            Free(handle);

            // Re-add left branch
            int leftChildHandle = node->LeftChildHandle;
            if (leftChildHandle != Node.Null)
            {
                AddHandle(leftChildHandle);
            }

            // Re-add right branch
            int rightChildHandle = node->RightChildHandle;
            if (rightChildHandle != Node.Null)
            {
                AddHandle(rightChildHandle);
            }
        }

        /// <summary>
        /// The k-d nearest neighbor search.
        /// Returns iterator to nearest element.
        /// </summary>
        /// <param name="value">The value used for searching element.</param>
        /// <param name="numSearch">The number of elements searched.</param>
        /// <param name="maxSearch">The maximum number elements that c
        public Handle FindNearest(TValue value, out int numSearch, int maxSearch = int.MaxValue)
        {
            var branches = new UnsafePriorityQueue<Branch, BranchComparer>(Length, Allocator.Temp, new BranchComparer());
            int bestHandle = Node.Null;
            float bestDistance = float.MaxValue;
            int count = 0;

            int handle = m_RootHandle;
            while (count < maxSearch)
            {
                var node = m_Nodes.Ptr + handle;

                count++;

                // Update best handle
                float distance = m_Comparer.DistanceSq(node->Value, value);
                if (distance < bestDistance)
                {
                    bestHandle = handle;
                    bestDistance = distance;
                }

                // Find which child is next handle based on comparer
                int nextHandle;
                int otherHandle;
                if (m_Comparer.Compare(node->Value, value, node->Height) > 0)
                {
                    // Left
                    nextHandle = node->LeftChildHandle;
                    otherHandle = node->RightChildHandle;
                }
                else
                {
                    // Right
                    nextHandle = node->RightChildHandle;
                    otherHandle = node->LeftChildHandle;
                }

                // Record unwind step so it can be take later on
                float distanceToSplit = m_Comparer.DistanceToSplitSq(node->Value, value, node->Height);
                if (otherHandle != Node.Null && distanceToSplit < bestDistance)
                {
                    branches.Enqueue(new Branch(otherHandle, distanceToSplit));
                }

                if (nextHandle == Node.Null)
                {
                    // This is unwind step where algorithm attemps to get to previous visited parents other branches
                    // Always takes the first branch that has smallest distance to split
                    if (!branches.TryDequeue(out Branch branch))
                        break;

                    // If the distance to split is bigger than best distance, it means we already got the best node
                    if (branch.DistanceToSplit > bestDistance)
                        break;

                    handle = branch.Handle;
                }
                else
                {
                    handle = nextHandle;
                }
            }
            branches.Dispose();

            numSearch = count;

            return new Handle(bestHandle);
        }

        /// <summary>
        /// The k-d nearest n neighbor search.
        /// </summary>
        /// <param name="value">The value used for searching element.</param>
        /// <param name="maxResult">The number of nearest neighbors to search.</param>
        /// <param name="result">Outputs the found elements.</param>
        /// <param name="numSearch">The number of elements searched.</param>
        /// <param name="maxSearch">The maximum number elements that can be searched. Quite useful for approximate search.</param>
        public void FindNearestRange(TValue value, int maxResult, ref NativeList<TValue> result, out int numSearch, int maxSearch = int.MaxValue)
        {
            var branches = new UnsafePriorityQueue<Branch, BranchComparer>(Length, Allocator.Temp, new BranchComparer());
            var bestBranches = new UnsafePriorityQueue<Branch, BranchComparerDescending>(maxResult, Allocator.Temp, new BranchComparerDescending());
            int count = 0;

            int handle = m_RootHandle;
            while (count < maxSearch)
            {
                var node = m_Nodes.Ptr + handle;

                count++;

                // Update best handle
                float distance = m_Comparer.DistanceSq(node->Value, value);
                float bestDistance = bestBranches.Length != maxResult ? float.MaxValue : bestBranches.Peek().DistanceToSplit;
                if (distance < bestDistance)
                {
                    bestBranches.Enqueue(new Branch(handle, distance));

                    // Remove if exceeds the maximum result count
                    if (bestBranches.Length > maxResult)
                        bestBranches.Dequeue();

                    // Update after the queue have changed
                    bestDistance = bestBranches.Peek().DistanceToSplit;
                }

                // Find which child is next handle based on comparer
                int nextHandle;
                int otherHandle;
                if (m_Comparer.Compare(node->Value, value, node->Height) > 0)
                {
                    // Left
                    nextHandle = node->LeftChildHandle;
                    otherHandle = node->RightChildHandle;
                }
                else
                {
                    // Right
                    nextHandle = node->RightChildHandle;
                    otherHandle = node->LeftChildHandle;
                }

                // Record unwind step so it can be take later on
                float distanceToSplit = m_Comparer.DistanceToSplitSq(node->Value, value, node->Height);
                if (otherHandle != Node.Null && distanceToSplit < bestDistance)
                {
                    branches.Enqueue(new Branch(otherHandle, distanceToSplit));
                }

                if (nextHandle == Node.Null)
                {
                    // This is unwind step where algorithm attemps to get to previous visited parents other branches
                    // Always takes the first branch that has smallest distance to split
                    if (!branches.TryDequeue(out Branch branch))
                        break;

                    // If the distance to split is bigger than best distance, it means we already got the best node
                    if (branch.DistanceToSplit > bestDistance)
                        break;

                    handle = branch.Handle;
                }
                else
                {
                    handle = nextHandle;
                }
            }
            branches.Dispose();

            while (bestBranches.TryDequeue(out Branch branch))
            {
                result.Add(m_Nodes[branch.Handle].Value);
            }
            bestBranches.Dispose();
            numSearch = count;
        }

        /// <summary>
        /// The k-d radius search.
        /// </summary>
        /// <param name="value">The value used for searching element.</param>
        /// <param name="maxDistance">The maximum distance from value used for searching.</param>
        /// <param name="result">Outputs the found elements.</param>
        /// <param name="numSearch">The number of elements searched.</param>
        /// <param name="maxSearch">The maximum number elements that can be searched. Quite useful for approximate search.</param>
        public void FindRadius(TValue value, float maxDistance, ref NativeList<TValue> result, out int numSearch, int maxSearch = int.MaxValue)
        {
            var branches = new UnsafePriorityQueue<Branch, BranchComparer>(Length, Allocator.Temp, new BranchComparer());
            int count = 0;
            float maxDistanceSq = maxDistance * maxDistance;

            result.Clear();

            int handle = m_RootHandle;
            while (count < maxSearch)
            {
                var node = m_Nodes.Ptr + handle;

                count++;

                // Update best handle
                float distance = m_Comparer.DistanceSq(node->Value, value);
                if (distance <= maxDistanceSq)
                {
                    result.Add(m_Nodes[handle].Value);
                }

                // Find which child is next handle based on comparer
                int nextHandle;
                int otherHandle;
                if (m_Comparer.Compare(node->Value, value, node->Height) > 0)
                {
                    // Left
                    nextHandle = node->LeftChildHandle;
                    otherHandle = node->RightChildHandle;
                }
                else
                {
                    // Right
                    nextHandle = node->RightChildHandle;
                    otherHandle = node->LeftChildHandle;
                }

                // Record unwind step so it can be take later on
                float distanceToSplit = m_Comparer.DistanceToSplitSq(node->Value, value, node->Height);
                if (otherHandle != Node.Null && distanceToSplit <= maxDistanceSq)
                {
                    branches.Enqueue(new Branch(otherHandle, distanceToSplit));
                }

                if (nextHandle == Node.Null)
                {
                    // This is unwind step where algorithm attemps to get to previous visited parents other branches
                    // Always takes the first branch that has smallest distance to split
                    if (!branches.TryDequeue(out Branch branch))
                        break;

                    // If the distance to split is bigger than best distance, it means we already got the best node
                    if (branch.DistanceToSplit > maxDistanceSq)
                        break;

                    handle = branch.Handle;
                }
                else
                {
                    handle = nextHandle;
                }
            }
            branches.Dispose();

            numSearch = count;
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
        /// Returns the depth of the tree. It is the maximum height of all nodes.
        /// </summary>
        public int GetDepth()
        {
            // No need to use iterators as we can simply go through all nodes
            int depth = 0;
            for (int i = 0; i < m_Nodes.Length; ++i)
            {
                int height = m_Nodes[i].Height;
                if (depth < height)
                    depth = height;
            }
            return depth;
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

        /// <summary>
        /// Returns new allocated node handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Allocate(in TValue value, int parentHandle, int leftChildHandle, int rightChildHandle, int height)
        {
            int handle;
            if (m_FreeHandles.TryPop(out handle))
            {
                m_Nodes[handle] = new Node
                {
                    Value = value,
                    ParentHandle = parentHandle,
                    LeftChildHandle = leftChildHandle,
                    RightChildHandle = rightChildHandle,
                    Height = height,
                };
            }
            else
            {
                handle = m_Nodes.Length;
                m_Nodes.Add(new Node
                {
                    Value = value,
                    ParentHandle = parentHandle,
                    LeftChildHandle = leftChildHandle,
                    RightChildHandle = rightChildHandle,
                    Height = height,
                });
            }

            m_Length++;

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
            m_Nodes.Ptr[nodeHandle].Height = 0; // Required by GetDepth
            m_FreeHandles.Push(nodeHandle);
            m_Length--;
        }

        int BuildRecursive(NativeSlice<TValue> values, int parentHandle, int heigth)
        {
            heigth++;

            values.Sort(new BuildComparer(m_Comparer, heigth));

            int index = values.Length / 2;
            int handle = Allocate(values[index], parentHandle, Node.Null, Node.Null, heigth);

            // Build recursive
            if (values.Length > 2)
            {
                int leftChildHandle = BuildRecursive(values.Slice(0, index), handle, heigth);
                int rightChildHandle = BuildRecursive(values.Slice(index + 1, values.Length - (index + 1)), handle, heigth);

                // Update connections
                Node* node = m_Nodes.Ptr + handle;
                node->LeftChildHandle = leftChildHandle;
                node->RightChildHandle = rightChildHandle;
            }
            else if (values.Length == 2)
            {
                int leftChildHandle = Allocate(values[0], handle, Node.Null, Node.Null, heigth + 1);

                // Update connections
                Node* node = m_Nodes.Ptr + handle;
                node->LeftChildHandle = leftChildHandle;
            }

            return handle;
        }

        void AddHandle(int newHandle)
        {
            Node* newNode = m_Nodes.Ptr + newHandle;

            // Create root if it does not exist
            if (m_RootHandle == Node.Null)
            {
                m_RootHandle = newHandle;
                newNode->Height = 0;
                newNode->ParentHandle = Node.Null;
                return;
            }

            int handle = m_RootHandle;
            while (true)
            {
                var node = m_Nodes.Ptr + handle;
                if (m_Comparer.Compare(node->Value, newNode->Value, node->Height) > 0)
                {
                    // Left
                    if (node->LeftChildHandle == Node.Null)
                    {
                        node->LeftChildHandle = newHandle;
                        newNode->Height = node->Height + 1;
                        newNode->ParentHandle = handle;
                        break;
                    }
                    else
                    {
                        handle = node->LeftChildHandle;
                    }
                }
                else
                {
                    // Right
                    if (node->RightChildHandle == Node.Null)
                    {
                        node->RightChildHandle = newHandle;
                        newNode->Height = node->Height + 1;
                        newNode->ParentHandle = handle;
                        break;
                    }
                    else
                    {
                        handle = node->RightChildHandle;
                    }
                }
            }
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
        public struct Handle
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
            public static implicit operator int(Handle handled) => handled.m_Handle;
            public static bool operator ==(Handle lhs, Handle rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Handle lhs, Handle rhs) => lhs.m_Handle != rhs.m_Handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct Node
        {
            public TValue Value;
            public int ParentHandle;
            public int LeftChildHandle;
            public int RightChildHandle;
            public int Height;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public bool IsFree;
#endif

            public static int Null => -1;
        }

        struct Branch
        {
            public int Handle;
            public float DistanceToSplit;

            public Branch(int handle, float distanceToSplit)
            {
                Handle = handle;
                DistanceToSplit = distanceToSplit;
            }
        }

        struct BranchComparer : IComparer<Branch>
        {
            public int Compare(Branch x, Branch y)
            {
                return x.DistanceToSplit.CompareTo(y.DistanceToSplit);
            }
        }

        struct BuildComparer : IComparer<TValue>
        {
            public TComparer Comparer;
            public int Height;

            public BuildComparer(TComparer comparer, int height)
            {
                Comparer = comparer;
                Height = height;
            }

            public int Compare(TValue x, TValue y)
            {
                return Comparer.Compare(x, y, Height);
            }
        }

        struct BranchComparerDescending : IComparer<Branch>
        {
            public int Compare(Branch x, Branch y)
            {
                return y.DistanceToSplit.CompareTo(x.DistanceToSplit);
            }
        }
    }
}
