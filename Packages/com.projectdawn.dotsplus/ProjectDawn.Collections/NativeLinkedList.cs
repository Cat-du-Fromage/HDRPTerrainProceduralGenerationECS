using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    internal sealed class NativeLinkedListTDebugView<T> where T : unmanaged
    {
        NativeLinkedList<T> Data;

        public NativeLinkedListTDebugView(NativeLinkedList<T> data)
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
    /// An unmanaged, resizable linked list.
    /// Linked list is efficient at inserting and removing elements. However, not so efficient with cache usage.
    /// Linked list is implemented using double linked nodes, where each node knows its next-node link and previous-node link.
    /// </summary>
    /// <remarks>The elements are not stored contiguously in a buffer rather than in true linked nodes.</remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(NativeLinkedListTDebugView<>))]
    public unsafe struct NativeLinkedList<T>
        : IDisposable
        , IEnumerable<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeLinkedList<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

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
        /// Returns an iterator pointing to the first element in the list container.
        /// </summary>
        public Iterator Begin
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                return new Iterator(m_Data, m_Data->Begin, false);
            }
        }

        /// <summary>
        /// Returns an iterator referring to the past-the-end element in the list container.
        /// The past-the-end element is the theoretical element that would follow the last element in the list container. 
        /// It does not point to any element, and thus shall not be dereferenced.
        /// </summary>
        public Iterator End
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                return new Iterator(m_Data, m_Data->End, false);
            }
        }

        /// <summary>
        /// Returns an iterator pointing to the first element in the list container.
        /// Read only iterator.
        /// </summary>
        public Iterator BeginRO
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return new Iterator(m_Data, m_Data->Begin, true);
            }
        }

        /// <summary>
        /// Returns an iterator referring to the past-the-end element in the list container.
        /// The past-the-end element is the theoretical element that would follow the last element in the list container. 
        /// It does not point to any element, and thus shall not be dereferenced.
        /// Read only iterator.
        /// </summary>
        public Iterator EndRO
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return new Iterator(m_Data, m_Data->End, true);
            }
        }

        /// <summary>
        /// Initializes and returns an instance of NativeLinkedList.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        public NativeLinkedList(Allocator allocator) : this(1, allocator) { }

        /// <summary>
        /// Initializes and returns an instance of NativeLinkedList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeLinkedList(int initialCapacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckCapacity(initialCapacity);
            CollectionChecks.CheckIsUnmanaged<T>();
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = AtomicSafetyHandle.Create();
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 2, allocator);
#endif
#endif

            m_Data = UnsafeLinkedList<T>.Create(initialCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public Iterator Add(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            var handle = m_Data->Add(value);
            return new Iterator(m_Data, handle);
        }

        /// <summary>
        /// The container is extended by inserting new elements before the element at the specified position.
        /// </summary>
        /// <param name="iterator">Position in the container where the new elements are inserted.</param>
        /// <param name="value">Value to be copied (or moved) to the inserted elements.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public Iterator Insert(in Iterator iterator, in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            var handle = m_Data->Insert(iterator, value);
            return new Iterator(m_Data, handle);
        }

        /// <summary>
        /// Removes the element at an position. Decrements the length by 1.
        /// </summary>
        /// <param name="iterator">Position in the container where the element will be removed.</param>
        public void RemoveAt(in Iterator iterator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->RemoveAt(iterator);
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
            UnsafeLinkedList<T>.Destroy(m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Returns an array containing a copy of this queue's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this queue's content.</returns>
        public NativeArray<T> ToArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->ToArray(allocator);
        }

        /// <summary>
        /// Sorts this list using a custom comparison.
        /// Uses insertion sort algorithm.
        /// </summary>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="begin">Sort begin iterator.</param>
        /// <param name="end">Sort end iterator.</param>
        /// <param name="comparer">The comparison function used to determine the relative order of the elements.</param>
        public void Sort<U>(ref Iterator begin, ref Iterator end, U comparer) where U : unmanaged, IComparer<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_Data->Sort(ref begin.m_Handle, ref end.m_Handle, comparer);
        }

        /// <summary>
        /// Returns an enumerator over the elements of this linked list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Linked list iterator.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Iterator
        {
            [NativeDisableUnsafePtrRestriction]
            UnsafeLinkedList<T>* m_Data;

            internal UnsafeLinkedList<T>.Handle m_Handle;

            bool m_ReadOnly;

            /// <summary>
            /// Returns true if iterator is read only.
            /// </summary>
            public bool ReadOnly => m_ReadOnly;

            /// <summary>
            /// Iterator referenced value.
            /// </summary>
            public T Value
            {
                get => (*m_Data)[m_Handle];
                set
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (m_ReadOnly)
                        throw new InvalidOperationException("Iterator was created as readonly.");
#endif
                    (*m_Data)[m_Handle] = value;
                }
            }

            /// <summary>
            /// Returns iterator to next element.
            /// </summary>
            public Iterator Next => new Iterator(m_Data, m_Data->Next(m_Handle), m_ReadOnly);

            /// <summary>
            /// Returns iterator to previous element.
            /// </summary>
            public Iterator Previous => new Iterator(m_Data, m_Data->Previous(m_Handle), m_ReadOnly);

            internal Iterator(UnsafeLinkedList<T>* data, UnsafeLinkedList<T>.Handle handle, bool readOnly = false)
            {
                m_Data = data;
                m_Handle = handle;
                m_ReadOnly = readOnly;
            }

            /// <summary>
            /// Move iterator to next element.
            /// </summary>
            /// <returns>Returns iterator to next element.</returns>
            public Iterator MoveNext()
            {
                m_Handle = m_Data->Next(m_Handle);
                return this;
            }

            /// <summary>
            /// Move iterator to previous element.
            /// </summary>
            /// <returns>Returns iterator to previous element.</returns>
            public Iterator MovePrevious()
            {
                m_Handle = m_Data->Previous(m_Handle);
                return this;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode() => m_Handle;

            public static implicit operator UnsafeLinkedList<T>.Handle(Iterator iterator) => iterator.m_Handle;
            public static bool operator ==(Iterator lhs, Iterator rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Iterator lhs, Iterator rhs) => lhs.m_Handle != rhs.m_Handle;
        }
    }
}
