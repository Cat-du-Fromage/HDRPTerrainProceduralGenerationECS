using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ProjectDawn.Geometry3D
{
    /// <summary>
    /// Parametric line that is specified by center and direction.
    /// </summary>
    [DebuggerDisplay("Origin = {Origin}, Direction = {Direction}")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Ray
    {
        /// <summary>
        /// Line center point.
        /// </summary>
        public float3 Origin;

        /// <summary>
        /// Line direction vector.
        /// </summary>
        public float3 Direction;

        public Ray(float3 origin, float3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        /// <summary>
        /// Returns point along the ray at given time.
        /// </summary>
        public float3 GetPoint(float t) => Origin + Direction * t;

        /// <summary>
        /// Returns true if ray intersects triangle.
        /// Based on https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm.
        /// </summary>
        /// <param name="triangle">Triangle.</param>
        /// <param name="t">Intersection time.</param>
        /// <returns>Returns true if ray intersects triangle.</returns>
        public bool Intersection(Triangle triangle, out float t) => ShapeUtility.IntersectionRayAndTriangle(this, triangle, out t);

        /// <summary>
        /// Returns true if ray intersects surface.
        /// </summary>
        /// <param name="surface">Surface.</param>
        /// <param name="intersection">Intersection data.</param>
        /// <returns>Returns true if ray intersects surface.</returns>
        public bool Intersection<T>(TriangularSurface<T> surface, out SurfacePointIntersection intersection) where T : unmanaged, ITransformFloat3 => ShapeUtility.IntersectionRayAndTriangularSurface(this, surface, out intersection);
    }
}
