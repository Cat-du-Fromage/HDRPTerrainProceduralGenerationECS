using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ProjectDawn.Geometry2D
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
        public float2 Origin;

        /// <summary>
        /// Line direction vector.
        /// </summary>
        public float2 Direction;

        public Ray(float2 origin, float2 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        /// <summary>
        /// Returns point along the ray at given time.
        /// </summary>
        public float2 GetPoint(float t)
        {
            return Origin + Direction * t;
        }

        /// <summary>
        /// Returns a point on the perimeter of this rectangle that is closest to the specified point.
        /// </summary>
        public static float2 ClosestPoint(in Ray ray, float2 point)
        {
            float2 towards = ray.Direction;

            float lengthSq = math.lengthsq(towards);
            if (lengthSq < math.EPSILON)
                return point;

            float t = math.dot(point - ray.Origin, towards) / lengthSq;

            return ray.GetPoint(t);
        }

        /// <summary>
        /// Returns intersection point between two rays.
        /// </summary>
        public static bool IntersectionPoint(Ray a, Ray b, out float2 point)
        {
            if (ShapeUtility.Intersection(a, b, out float2 t))
            {
                point = a.GetPoint(t.x);
                return true;
            }
            point = 0;
            return false;
        }

        /// <summary>
        /// Returns intersection time between ray and shape.
        /// </summary>
        public bool Intersection(Line line, out float t) => ShapeUtility.Intersection(this, line, out t);

        /// <summary>
        /// Returns intersection times between ray and shape.
        /// </summary>
        public bool Intersection(Circle circle, out float2 t) => ShapeUtility.Intersection(this, circle, out t);

        /// <summary>
        /// Returns intersection times between ray and shape.
        /// </summary>
        public bool Intersection(Rectangle rectangle, out float2 t) => ShapeUtility.Intersection(this, rectangle, out t);

        /// <summary>
        /// Returns intersection line between ray and shape.
        /// </summary>
        public bool IntersectionLine(Circle circle, out Line intersection) => ShapeUtility.Intersection(this, circle, out intersection);

        /// <summary>
        /// Returns intersection line between ray and shape.
        /// </summary>
        public bool IntersectionLine(Rectangle rectangle, out Line intersection) => ShapeUtility.Intersection(this, rectangle, out intersection);

        /// <summary>
        /// Returns line at specific time range.
        /// </summary>
        public Line ToLine(float2 t) => new Line(GetPoint(t.x), GetPoint(t.y));
    }
}
