using ProjectDawn.Collections.LowLevel.Unsafe;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    /// <summary>
    /// An unmanaged, resizable aabb tree.
    /// AABB tree (short for axis aligned bounding box tree) is a space-partitioning data structure for organizing bounding shapes in space.
    /// As structure uses generic it is not only usable for boxes, but any shape that implements interfaces.
    /// AABB trees are a useful data structure for fast searching bounding shapes in space.
    /// AABB trees are a special case of binary space partitioning trees.
    /// Based on https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf.
    /// </summary>
    /// <typeparam name="T">The type of the bounding shape.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct NativeAABBTree<T>
        : IDisposable
        where T : unmanaged, ISurfaceArea<T>, IUnion<T>
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeAABBTree<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Whether the tree is empty.
        /// </summary>
        /// <value>True if the tree is empty or the tree has not been constructed.</value>
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
        /// Whether this tree has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this tree has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data != null;

        /// <summary>
        /// Returns the tree root.
        /// </summary>
        public Iterator Root
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return new Iterator(m_Data, m_Data->Root);
            }
        }

        /// <summary>
        /// Initialized and returns an instance of NativeAABBTree.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeAABBTree(int initialCapacity, Allocator allocator)
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

            m_Data = UnsafeAABBTree<T>.Create(initialCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Builds aabb tree with given array of values.
        /// </summary>
        /// <param name="value">The array of values.</param>
        public void Build(in NativeArray<T> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Build(array);
        }

        /// <summary>
        /// Add element to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public Iterator Add(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            var newHandle = m_Data->Add(value);
            return new Iterator(m_Data, newHandle);
        }

        /// <summary>
        /// Removes node from the tree at givent iterator value.
        /// </summary>
        /// <param name="iterator">Position from which node will be removed.</param>
        public void RemoveAt(Iterator iterator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->RemoveAt(iterator);
        }

        /// <summary>
        /// Find all bounding shapes that overlap with value.
        /// </summary>
        /// <param name="value">Value that will be used for testing overlap.</param>
        /// <param name="result">Array of the bounding shapes that overlap.</param>
        /// <typeparam name="U"></typeparam>
        /// <returns>Returns the number of bounding shapes overlap.</returns>
        public int FindOverlap<U>(U value, ref NativeList<T> result)
            where U : unmanaged, IOverlap<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->FindOverlap(value, ref result);
        }

        /// <summary>
        /// Returns the sum of all non leaf surface area.
        /// The lower the number is, the more optimal a tree will be.
        /// </summary>
        public float Cost()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->Cost();
        }

        /// <summary>
        /// Removes all nodes of this tree.
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
            UnsafeAABBTree<T>.Destroy(m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Linked list iterator.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Iterator
        {
            [NativeDisableUnsafePtrRestriction]
            UnsafeAABBTree<T>* m_Data;

            UnsafeAABBTree<T>.Handle m_Handle;

            /// <summary>
            /// Iterator referenced value.
            /// </summary>
            public T Value => (*m_Data)[m_Handle];

            /// <summary>
            /// Returns true if handle is valid.
            /// </summary>
            public bool Valid => m_Handle.Valid;

            /// <summary>
            /// Returns iterator that references to parent.
            /// </summary>
            public Iterator Parent => new Iterator(m_Data, m_Data->Parent(m_Handle));

            /// <summary>
            /// Returns iterator that references to left child.
            /// </summary>
            public Iterator Left => new Iterator(m_Data, m_Data->Left(m_Handle));

            /// <summary>
            /// Returns iterator that references to right child.
            /// </summary>
            public Iterator Right => new Iterator(m_Data, m_Data->Right(m_Handle));

            internal Iterator(UnsafeAABBTree<T>* data, UnsafeAABBTree<T>.Handle handle)
            {
                m_Data = data;
                m_Handle = handle;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode() => m_Handle;

            public static implicit operator UnsafeAABBTree<T>.Handle(Iterator iterator) => iterator.m_Handle;
            public static bool operator ==(Iterator lhs, Iterator rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Iterator lhs, Iterator rhs) => lhs.m_Handle != rhs.m_Handle;
        }
    }
}
