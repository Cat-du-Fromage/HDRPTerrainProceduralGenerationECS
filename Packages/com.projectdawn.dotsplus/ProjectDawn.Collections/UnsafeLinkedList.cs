using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.LowLevel.Unsafe
{
    internal sealed class UnsafeLinkedListTDebugView<T> where T : unmanaged
    {
        UnsafeLinkedList<T> Data;

        public UnsafeLinkedListTDebugView(UnsafeLinkedList<T> data)
        {
            Data = data;
        }

        public unsafe T[] Items
        {
            get
            {
                T[] result = new T[Data.Length];

                int index = 0;
                foreach (var item in Data)
                {
                    result[index++] = item;
                }

                return result;
            }
        }
    }

    /// <summary>
    /// An unmanaged, resizable linked list, without any thread safety check features.
    /// </summary>
    /// <remarks>The elements are not stored contiguously in a buffer rather than in true linked nodes.</remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(UnsafeLinkedListTDebugView<>))]
    public unsafe struct UnsafeLinkedList<T>
        : IDisposable
        , IEnumerable<T>
        where T : unmanaged
    {
        int m_BeginHandle;
        int m_EndHandle;
        UnsafeList<Node> m_Nodes;
        UnsafeStack<int> m_FreeHandles;
        int m_Length;

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
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Nodes.IsCreated && m_FreeHandles.IsCreated;

        /// <summary>
        /// Returns an iterator pointing to the first element in the list container.
        /// </summary>
        public Handle Begin => new Handle(m_BeginHandle);

        /// <summary>
        /// Returns an iterator referring to the past-the-end element in the list container.
        /// The past-the-end element is the theoretical element that would follow the last element in the list container. 
        /// It does not point to any element, and thus shall not be dereferenced.
        /// </summary>
        public Handle End => new Handle(m_EndHandle);

        /// <summary>
        /// Allocator used by this data structure.
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator => m_Nodes.Allocator;

        /// <summary>
        /// Initializes and returns an instance of UnsafeLinkedList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeLinkedList(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_BeginHandle = m_EndHandle = Node.Null;
            m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            m_Length = -1; // Allocate will increase it to 0
            m_BeginHandle = m_EndHandle = Allocate(default, Node.Null, Node.Null);
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public static UnsafeLinkedList<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeLinkedList<T>* data = AllocatorManager.Allocate<UnsafeLinkedList<T>>(allocator);

            data->m_Nodes = new UnsafeList<Node>(initialCapacity, allocator);
            data->m_FreeHandles = new UnsafeStack<int>(initialCapacity, allocator);
            data->m_Length = -1; // Allocate will increase it to 0
            data->m_BeginHandle = data->m_EndHandle = data->Allocate(default, Node.Null, Node.Null);

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeLinkedList<T>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->m_Nodes.Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// The element at a given position.
        /// </summary>
        /// <param name="handle">Handle of the element.</param>
        public T this[Handle handle]
        {
            get
            {
                CheckHandle(handle);
                return (m_Nodes.Ptr + handle)->Value ;
            }
            set
            {
                CheckHandle(handle);
                (m_Nodes.Ptr + handle)->Value = value;
            }
        }

        public T* GetUnsafePtr(Handle handle)
        {
            return &(m_Nodes.Ptr + handle)->Value;
        }

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public Handle Add(in T value)
        {
            var newHandle = InsertHandle(m_EndHandle, value);
            return new Handle(newHandle);
        }

        /// <summary>
        /// The container is extended by inserting new elements before the element at the specified position.
        /// </summary>
        /// <param name="iterator">Position in the container where the new elements are inserted.</param>
        /// <param name="value">Value to be copied (or moved) to the inserted elements.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public Handle Insert(in Handle iterator, in T value)
        {
            CheckHandle(iterator);
            var newHandle = InsertHandle(iterator, value);
            return new Handle(newHandle);
        }

        /// <summary>
        /// Removes the element at an position. Decrements the length by 1.
        /// </summary>
        /// <param name="iterator">Position in the container where the element will be removed.</param>
        public void RemoveAt(in Handle iterator)
        {
            CheckHandle(iterator);

            int handle = iterator;
            CheckNotEndHandle(handle);
            var node = m_Nodes.ElementAt(handle);
            var previousHandle = node.PreviousHandle;
            var nextHandle = node.NextHandle;

            // Update connections
            if (previousHandle != Node.Null)
                m_Nodes.ElementAt(previousHandle).NextHandle = nextHandle;
            if (nextHandle != Node.Null)
                m_Nodes.ElementAt(nextHandle).PreviousHandle = previousHandle;

            // Update begin and end
            if (handle == m_BeginHandle)
                m_BeginHandle = nextHandle;

            Free(handle);
        }

        /// <summary>
        /// Removes all elements of this queue.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Nodes.Clear();
            m_FreeHandles.Clear();
            m_Length = 0;
            m_BeginHandle = m_EndHandle = Allocate(default, Node.Null, Node.Null);
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
        /// Returns an array containing a copy of this list's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this queue's content.</returns>
        public NativeArray<T> ToArray(Allocator allocator)
        {
            var output = new NativeArray<T>(Length, allocator);

            int index = 0;
            foreach (var item in this)
            {
                output[index++] = item;
            }
            return output;
        }

        /// <summary>
        /// Returns next handle.
        /// </summary>
        public Handle Next(Handle handle)
        {
            CheckHandle(handle);
            return new Handle(m_Nodes[handle].NextHandle);
        }

        /// <summary>
        /// Returns previous handle.
        /// </summary>
        public Handle Previous(Handle handle)
        {
            CheckHandle(handle);
            return new Handle(m_Nodes[handle].PreviousHandle);
        }

        /// <summary>
        /// Sorts this list using a custom comparison.
        /// Uses insertion sort algorithm.
        /// </summary>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="begin">Sort begin iterator.</param>
        /// <param name="end">Sort end iterator.</param>
        /// <param name="comparer">The comparison function used to determine the relative order of the elements.</param>
        public void Sort<U>(ref Handle begin, ref Handle end, U comparer) where U : unmanaged, IComparer<T>
        {
            CheckHandle(begin);
            CheckHandle(end);

            // List is empty
            if (begin == end)
                return;

            var split = Next(begin);

            while (split != end)
            {
                var next = Next(split);

                Node splitNode = m_Nodes.Ptr[split];
                T splitValue = splitNode.Value;

                for (var current = begin; current != split; current = Next(current))
                {
                    if (comparer.Compare(this[current], splitValue) > -1)
                    {
                        // Disconnect last node from linked list
                        m_Nodes.Ptr[splitNode.PreviousHandle].NextHandle = splitNode.NextHandle;
                        m_Nodes.Ptr[splitNode.NextHandle].PreviousHandle = splitNode.PreviousHandle;

                        // Re insert split before current node
                        Node currentNode = m_Nodes.Ptr[current];
                        if (currentNode.PreviousHandle != Node.Null)
                            m_Nodes.Ptr[currentNode.PreviousHandle].NextHandle = split;
                        m_Nodes.Ptr[current].PreviousHandle = split;

                        // Update the split node connections
                        m_Nodes.Ptr[split].PreviousHandle = currentNode.PreviousHandle;
                        m_Nodes.Ptr[split].NextHandle = current;

                        // Update begin and end
                        if (current == m_BeginHandle)
                            m_BeginHandle = split;

                        // Update if it was head
                        if (current == begin)
                            begin = split;

                        break;
                    }
                }

                // Move split to next
                split = next;
            }
        }

        /// <summary>
        /// Returns an enumerator over the elements of this linked list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(m_Nodes, m_BeginHandle, m_EndHandle);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int InsertHandle(int handle, in T value)
        {
            var node = m_Nodes.Ptr + handle;
            var previousHandle = node->PreviousHandle;

            var newHandle = Allocate(value, handle, previousHandle);

            // Update connections
            m_Nodes.Ptr[handle].PreviousHandle = newHandle;
            if (previousHandle != Node.Null)
                m_Nodes.Ptr[previousHandle].NextHandle = newHandle;

            // Update begin and end
            if (handle == m_BeginHandle)
                m_BeginHandle = newHandle;

            return newHandle;
        }

        /// <summary>
        /// Returns new allocated node handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Allocate(in T value, int nextNodeHandle, int previousNodeHandle)
        {
            int handle;
            if (m_FreeHandles.TryPop(out handle))
            {
                m_Nodes[handle] = new Node
                {
                    Value = value,
                    NextHandle = nextNodeHandle,
                    PreviousHandle = previousNodeHandle
                };
            }
            else
            {
                handle = m_Nodes.Length;
                m_Nodes.Add(new Node {
                    Value = value,
                    NextHandle = nextNodeHandle,
                    PreviousHandle = previousNodeHandle
                });
            }

            m_Length++;

            return handle;
        }

        /// <summary>
        /// Releases node with given handle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Free(int handle)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Nodes.Ptr[handle].IsFree = true;
#endif
            m_FreeHandles.Push(handle);
            m_Length--;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNotEndHandle(int handle)
        {
            if (handle == m_EndHandle)
                throw new ArgumentException($"Iterator is not valid with handle {handle}.");
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
        /// Linked list iterator.
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

            public Handle(int handle)
            {
                m_Handle = handle;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode() => m_Handle;
            public static implicit operator int(Handle handled) => handled.m_Handle;
            public static bool operator==(Handle lhs, Handle rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Handle lhs, Handle rhs) => lhs.m_Handle != rhs.m_Handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Enumerator : IEnumerator<T>
        {
            UnsafeList<Node> m_Nodes;
            int m_BeginHandle;
            int m_EndHandle;
            Node m_Node;

            internal Enumerator(UnsafeList<Node> nodes, int begin, int end)
            {
                m_Nodes = nodes;
                m_BeginHandle = begin;
                m_EndHandle = end;

                // Create dummy node that targets begin node
                // This is needed, because enumerator will execute MoveNext before going into loop scope
                m_Node = new Node { NextHandle = begin, PreviousHandle = Node.Null };
            }

            public T Current => m_Node.Value;

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                var current = m_Node.NextHandle;
                m_Node = m_Nodes.ElementAt(current);
                return current != m_EndHandle;
            }

            public void Reset()
            {
                // Create dummy node that targets begin node
                // This is needed, because enumerator will execute MoveNext before going into loop scope
                m_Node = new Node { NextHandle = m_BeginHandle, PreviousHandle = Node.Null };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct Node
        {
            public T Value;
            public int NextHandle;
            public int PreviousHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public bool IsFree;
#endif

            public static int Null => -1;
        }
    }
}
