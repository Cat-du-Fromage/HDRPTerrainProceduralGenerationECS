using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// A convex polygon composed of counter clockwise ordered points.
    /// </summary>
    [DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ConvexPolygon<T>
        : IDisposable
        where T : unmanaged, ITransformFloat2
    {
        NativeArray<float2> m_Points;
        T m_Transform;

        /// <summary>
        /// Whether this polygon has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this polygon has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Points.IsCreated;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length => m_Points.Length;

        /// <summary>
        /// The element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <value>The element at the index.</value>
        public float2 this[int index]
        {
            get => m_Points[index];
            set
            {
                m_Points[index] = value;
            }
        }

        /// <summary>
        /// Tranform used for modifying points before each operation.
        /// </summary>
        public T Transform
        {
            get => m_Transform;
            set
            {
                m_Transform = value;
            }
        }

        public NativeSlice<float2> Points => m_Points;

        public ConvexPolygon(int length, Allocator allocator, T transform = default, NativeArrayOptions option = NativeArrayOptions.ClearMemory)
        {
            m_Points = new NativeArray<float2>(length, allocator, option);
            m_Transform = transform;
        }

        public void Dispose()
        {
            m_Points.Dispose();
        }

        /// <summary>
        /// Sort points into counter clockwise convex polygon.
        /// </summary>
        public void SortCounterClockwise() => ConvexPolygonUtility.SortCounterClockwise(m_Points);

        /// <summary>
        /// Returns true if points form convex polygon.
        /// </summary>
        public bool IsValid() => ConvexPolygonUtility.IsCounterClockwiseConvexPolygon(m_Points);

        /// <summary>
        /// Returns area of convex polygon.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public float GetArea() => ConvexPolygonUtility.GetArea(m_Points, m_Transform);

        /// <summary>
        /// Returns centroid of convex polygon.
        /// Polygon points must be convex and sorted counter clockwise.
        /// Based on https://en.wikipedia.org/wiki/Centroid.
        /// </summary>
        public float2 GetCentroid() => ConvexPolygonUtility.GetCentroid(m_Points, m_Transform);

        /// <summary>
        /// Returns minimum distance between two convex polygons.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public bool ContainsPoint(float2 point) => ConvexPolygonUtility.ContainsPoint(m_Points, m_Transform, point);

        /// <summary>
        /// Returns minimum distance between two convex polygons.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public float Distance(ConvexPolygon<T> polygon) => ConvexPolygonUtility.Distance(m_Points, m_Transform, polygon.m_Points, polygon.m_Transform);

        /// <summary>
        /// Returns minimum rectangle that fully covers shape.
        /// </summary>
        public Rectangle BoundingRectangle() => ConvexPolygonUtility.BoundingRectangle(m_Points, m_Transform);

        /// <summary>
        /// Returns maximum circle that is inside the shape.
        /// </summary>
        public Circle InscribedCircle() => ConvexPolygonUtility.InscribedCircle(m_Points, m_Transform);
    }
}
