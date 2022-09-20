using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    /// <summary>
    /// An unmanaged, resizable priority queue, without any thread safety check features.
    /// Priority queue main difference from regular queue that before element enqueue it executes insert sort.
    /// It is implemented using linked nodes as a result, inserting does not require pushing data around.
    /// </summary>
    /// <typeparam name="TValue">The type of the elements.</typeparam>
    /// <typeparam name="TComparer">The type of comparer used for comparing elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafePriorityQueue<TValue, TComparer>
        : IDisposable
        , IEnumerable<TValue>
        where TValue : unmanaged
        where TComparer : unmanaged, IComparer<TValue>
    {
        public UnsafeLinkedList<TValue> m_Data;
        TComparer m_Comparer;

        /// <summary>
        /// Whether the queue is empty.
        /// </summary>
        /// <value>True if the queue is empty or the queue has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length => m_Data.Length;

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        public int Capacity => m_Data.Capacity;

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Allocator used by this data structure.
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator => m_Data.Allocator;

        /// <summary>
        /// Initialized and returns an instance of NativePriorityQueue.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public UnsafePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer = default)
        {
            m_Data = new UnsafeLinkedList<TValue>(initialCapacity, allocator);
            m_Comparer = comparer;
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public static UnsafePriorityQueue<TValue, TComparer>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer = default)
        {
            UnsafePriorityQueue<TValue, TComparer>* data = AllocatorManager.Allocate<UnsafePriorityQueue<TValue, TComparer>>(allocator);

            data->m_Data = new UnsafeLinkedList<TValue>(initialCapacity, allocator);
            data->m_Comparer = comparer;

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafePriorityQueue<TValue, TComparer>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        public void Enqueue(in TValue value)
        {
            for (var itr = m_Data.Begin; itr != m_Data.End; itr = m_Data.Next(itr))
            {
                if (m_Comparer.Compare(m_Data[itr], value) > 0)
                {
                    m_Data.Insert(itr, value);
                    return;
                }
            }

            // Fall back to push
            m_Data.Add(value);
        }

        /// <summary>
        /// Adds an unique element at the front of the queue.
        /// Returns false if element already exists in queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <returns>Returns false if element already exists in queue.</returns>
        public bool EnqueueUnique(in TValue value)
        {
            for (var itr = m_Data.Begin; itr != m_Data.End; itr = m_Data.Next(itr))
            {
                var result = m_Comparer.Compare(m_Data[itr], value);
                if (result > 0)
                {
                    m_Data.Insert(itr, value);
                    return true;
                }
                else if (result == 0)
                    return false;
            }

            // Fall back to push
            m_Data.Add(value);
            return true;
        }

        /// <summary>
        /// Returns the element at the end of this queue without removing it.
        /// </summary>
        /// <returns>The element at the end of this queue.</returns>
        public TValue Peek()
        {
            if (IsEmpty)
                ThrowQueueEmpty();

            var begin = m_Data.Begin;
            var value = m_Data[begin];
            return value;
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue was empty.</exception>
        /// <returns>Returns the removed element.</returns>
        public TValue Dequeue()
        {
            if (!TryDequeue(out var value))
            {
                ThrowQueueEmpty();
            }
            return value;
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="value">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryDequeue(out TValue value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            var begin = m_Data.Begin;
            value = m_Data[begin];
            m_Data.RemoveAt(begin);
            return true;
        }

        /// <summary>
        /// Removes all elements of this queue.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Data.Clear();
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            m_Data.Dispose();
        }

        /// <summary>
        /// Returns an enumerator over the elements of this linked list.
        /// </summary>
        public IEnumerator<TValue> GetEnumerator()
        {
            return m_Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an array containing a copy of this queue's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this queue's content.</returns>
        public NativeArray<TValue> ToArray(Allocator allocator)
        {
            return m_Data.ToArray(allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowQueueEmpty()
        {
            throw new InvalidOperationException("Trying to dequeue from an empty queue");
        }
    }
}