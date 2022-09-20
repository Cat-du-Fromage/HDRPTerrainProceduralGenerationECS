using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    /// <summary>
    /// An unmanaged, resizable k-d tree.
    /// K-d tree (short for k-dimensional tree) is a space-partitioning data structure for organizing points in a k-dimensional space.
    /// K-d trees are a useful data structure for several applications, such as searches involving a multidimensional search key (e.g. range searches and nearest neighbor searches) and creating point clouds. 
    /// K-d trees are a special case of binary space partitioning trees.
    /// </summary>
    /// <code>
    /// struct TreeComparer : IKdTreeComparer
    /// {
    ///     public int Compare(float2 x, float2 y, int depth)
    ///     {
    ///         int axis = depth % 2;
    ///         return x[axis].CompareTo(y[axis]);
    ///     }
    ///     public float DistanceSq(float2 x, float2 y)
    ///     {
    ///         return math.distancesq(x, y);
    ///     }
    ///     public float DistanceToSplitSq(float2 x, float2 y, int depth)
    ///     {
    ///         int axis = depth % 2;
    ///         return (x[axis] - y[axis]) * (x[axis] - y[axis]);
    ///     }
    /// }
    /// </code>
    /// <typeparam name="TValue">The type of the elements.</typeparam>
    /// <typeparam name="TComparer">The type of the comparer for sorting elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct NativeKdTree<TValue, TComparer>
        : IDisposable
        where TValue : unmanaged
        where TComparer : unmanaged, IKdTreeComparer<TValue>
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeKdTree<TValue, TComparer>* m_Data;

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
        /// Initialized and returns an instance of NativeKdTree.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public NativeKdTree(Allocator allocator, TComparer comparer = default) : this(1, allocator, comparer) { }

        /// <summary>
        /// Initialized and returns an instance of NativeKdTree.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the priority queue.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="comparer">The element comparer to use.</param>
        public NativeKdTree(int initialCapacity, Allocator allocator, TComparer comparer = default)
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

            m_Data = UnsafeKdTree<TValue, TComparer>.Create(initialCapacity, allocator, comparer);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Builds balanced tree from elements.
        /// </summary>
        /// <param name="values">The values that will be used for building tree.</param>
        public void Build(NativeArray<TValue> values)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Build(values);
        }

        /// <summary>
        /// Add element to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public Iterator Add(TValue value)
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
        /// The k-d nearest neighbor search.
        /// Returns iterator to nearest element.
        /// </summary>
        /// <param name="value">The value used for searching element.</param>
        /// <param name="numSearch">The number of elements searched.</param>
        /// <param name="maxSearch">The maximum number elements that can be searched. Quite useful for approximate search.</param>
        /// <returns>Returns iterator to nearest element.</returns>
        public Iterator FindNearest(TValue value, out int numSearch, int maxSearch = int.MaxValue)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var handle = m_Data->FindNearest(value, out numSearch, maxSearch);
            return new Iterator(m_Data, handle);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_Data->FindNearestRange(value, maxResult, ref result, out numSearch, maxSearch);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_Data->FindRadius(value, maxDistance, ref result, out numSearch, maxSearch);
        }

        /// <summary>
        /// Returns the depth of the tree. It is the maximum height of all nodes.
        /// </summary>
        public int GetDepth()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Data->GetDepth();
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
            UnsafeKdTree<TValue, TComparer>.Destroy(m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Linked list iterator.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Iterator
        {
            [NativeDisableUnsafePtrRestriction]
            UnsafeKdTree<TValue, TComparer>* m_Data;

            UnsafeKdTree<TValue, TComparer>.Handle m_Handle;

            /// <summary>
            /// Iterator referenced value.
            /// </summary>
            public TValue Value => (*m_Data)[m_Handle];

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

            internal Iterator(UnsafeKdTree<TValue, TComparer>* data, UnsafeKdTree<TValue, TComparer>.Handle handle)
            {
                m_Data = data;
                m_Handle = handle;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode() => m_Handle;

            public static implicit operator UnsafeKdTree<TValue, TComparer>.Handle(Iterator iterator) => iterator.m_Handle;
            public static bool operator ==(Iterator lhs, Iterator rhs) => lhs.m_Handle == rhs.m_Handle;
            public static bool operator !=(Iterator lhs, Iterator rhs) => lhs.m_Handle != rhs.m_Handle;
        }
    }
}