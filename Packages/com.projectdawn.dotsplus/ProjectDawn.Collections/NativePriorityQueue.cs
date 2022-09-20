using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    /// <summary>
    /// An unmanaged, resizable priority queue.
    /// Priority queue main difference from regular queue that before element enqueue it executes insert sort.
    /// It is implemented using linked nodes as a result, inserting does not require pushing data around.
    /// </summary>
    /// <typeparam name="TValue">The type of the elements.</typeparam>
    /// <typeparam name="TComparer">The type of comparer used for comparing elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct NativePriorityQueue<TValue, TComparer>
        : IDisposable
        where TValue : unmanaged
        where TComparer : unmanaged, IComparer<TValue>
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafePriorityQueue<TValue, TComparer>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Whether the queue is empty.
        /// </summary>
        /// <value>True if the queue is empty or the queue has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Data->Length;
            }
        }

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Data->Capacity;
            }
        }

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data != null;

        /// <summary>
        /// Initialized and returns an instance of NativePriorityQueue.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public NativePriorityQueue(Allocator allocator, TComparer comparer = default) : this(1, allocator, comparer) {}

        /// <summary>
        /// Initialized and returns an instance of NativePriorityQueue.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public NativePriorityQueue(int initialCapacity, Allocator allocator, TComparer comparer = default)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckCapacity(initialCapacity);
            CollectionChecks.CheckIsUnmanaged<TValue>();
            CollectionChecks.CheckIsUnmanaged<TComparer>();
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = AtomicSafetyHandle.Create();
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 2, allocator);
#endif
#endif

            m_Data = UnsafePriorityQueue<TValue, TComparer>.Create(initialCapacity, allocator, comparer);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        public void Enqueue(in TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Enqueue(value);
        }

        /// <summary>
        /// Adds an unique element at the front of the queue.
        /// Returns false if element already exists in queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <returns>Returns false if element already exists in queue.</returns>
        public bool EnqueueUnique(in TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->EnqueueUnique(value);
        }

        /// <summary>
        /// Returns the element at the end of this queue without removing it.
        /// </summary>
        /// <returns>The element at the end of this queue.</returns>
        public TValue Peek()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->Peek();
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue was empty.</exception>
        /// <returns>Returns the removed element.</returns>
        public TValue Dequeue()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->Dequeue();
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="item">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryDequeue(out TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->TryDequeue(out value);
        }

        /// <summary>
        /// Removes all elements of this queue.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Clear();
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            AtomicSafetyHandle.Release(m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            UnsafePriorityQueue<TValue, TComparer>.Destroy(m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Returns an enumerator over the elements of this linked list.
        /// </summary>
        public IEnumerator<TValue> GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->GetEnumerator();
        }

        /// <summary>
        /// Returns an array containing a copy of this queue's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this queue's content.</returns>
        public NativeArray<TValue> ToArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->ToArray(allocator);
        }
    }
}