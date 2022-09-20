using System;
using System.Diagnostics;
using ProjectDawn.Collections;
using ProjectDawn.Geometry2D.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Voronoi builder used for construcing varonoi shapes.
    /// </summary>
    [DebuggerDisplay("NumSites = {NumSites}, IsCreated = {IsCreated}")]
    [NativeContainer]
    public unsafe struct VoronoiBuilder : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeVoronoiBuilder* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Returns the number of sites.
        /// </summary>
        public int NumSites => m_Data->NumSites;

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data != null;

        public VoronoiBuilder(int initialCapacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckCapacity(initialCapacity);
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = AtomicSafetyHandle.Create();
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 2, allocator);
#endif
#endif

            m_Data = UnsafeVoronoiBuilder.Create(initialCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Adds new site into voronoi builder.
        /// Returns false if point already exists.
        /// </summary>
        /// <param name="point">New site point.</param>
        public bool Add(double2 point)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->Add(point);
        }

        /// <summary>
        /// Constructs voronoi and outputs the data into the generic structure.
        /// </summary>
        /// <typeparam name="T">Type type of voronoi output.</typeparam>
        /// <param name="output">The output.</param>
        public void Construct<T>(ref T output) where T : IVoronoiOutput
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_Data->Construct(ref output);
        }

        /// <summary>
        /// Removes all elements of this queue.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
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
            UnsafeVoronoiBuilder.Destroy(m_Data);
            m_Data = null;
        }
    }
}