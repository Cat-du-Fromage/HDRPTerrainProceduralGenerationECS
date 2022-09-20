using System.Runtime.InteropServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static ProjectDawn.Mathematics.math2;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProjectDawn.Geometry3D
{
    /// <summary>
    /// A triangle is a polygon with three edges and three vertices. It is one of the basic shapes in geometry. A triangle with vertices A, B, and C is denoted.
    /// </summary>
    [DebuggerDisplay("{VertexA} {VertexB} {VertexC}")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public float3 VertexA;
        public float3 VertexB;
        public float3 VertexC;

        /// <summary>
        /// Returns normal of triangle.
        /// Normal direction depends from triangle clockwise order.
        /// </summary>
        public float3 Normal => normalize(cross(VertexC - VertexA, VertexB - VertexA));

        /// <summary>
        /// Returns the perimeter of the triangle.
        /// </summary>
        public float Perimeter => length(VertexB - VertexA) + length(VertexC - VertexB) + length(VertexA - VertexC);

        /// <summary>
        /// Returns the area of the triangle.
        /// </summary>
        public float Area => determinant(VertexC - VertexA, VertexB - VertexA) * 0.5f;

        public Triangle(float3 a, float3 b, float3 c)
        {
            VertexA = a;
            VertexB = b;
            VertexC = c;
        }

        /// <summary>
        /// Returns if triangle vertices are counter clockwise ordered.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCounterClockwise() => iscclockwise(VertexA, VertexB, VertexC);

        /// <summary>
        /// Returns if triangle vertices are clockwise ordered.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClockwise() => isclockwise(VertexA, VertexB, VertexC);

        /// <summary>
        /// Returns triangle lines.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLines(out Line a, out Line b, out Line c)
        {
            a = new Line(VertexA, VertexB);
            b = new Line(VertexB, VertexC);
            c = new Line(VertexC, VertexA);
        }

        /// <summary>
        /// Returns point barycentric coordinates.
        /// </summary>
        /// <param name="point">Point coverted to barycentric.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 BarycentricCoordinates(float3 point) => barycentric(VertexA, VertexB, VertexC, point);

        /// <summary>
        /// Returns true if triangle is valid.
        /// </summary>
        /// <returns>Returns true if triangle is valid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => Area > 0;

        /// <summary>
        /// Returns true if triangles intersect.
        /// </summary>
        /// <param name="triangle">Triangle.</param>
        /// <param name="line">Intersection line.</param>
        /// <returns>Returns true if triangles intersect.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersection(Triangle triangle, out Line line) => ShapeUtility.IntersectionTriangleAndTriangle(this, triangle, out line);

        /// <summary>
        /// Returns minimum rectangle that fully covers shape.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box BoundingBox()
        {
            float3 min = math.min(VertexA, math.min(VertexB, VertexC));
            float3 max = math.max(VertexA, math.max(VertexB, VertexC));
            return new Box(min, max - min);
        }
    }
}
