using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ProjectDawn.Geometry3D
{
    /// <summary>
    /// A sphere is a shape consisting of all points in a plane that are at a given distance from a given point, the centre.
    /// </summary>
    [DebuggerDisplay("Center = {Center}, Radius = {Radius}")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Sphere : IEquatable<Sphere>
    {
        /// <summary>
        /// Center of the sphere.
        /// </summary>
        public float3 Center;

        /// <summary>
        /// Radius of the sphere.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Diameter of the sphere. Diameter = 2 * Radius. 
        /// </summary>
        public float Diameter
        {
            get => 2f * Radius;
            set => Radius = value * 0.5f;
        }

        /// <summary>
        /// Returns the volume of the sphere.
        /// </summary>
        public float Volume => 4f/3f * math.PI * Radius * Radius * Radius;

        /// <summary>
        /// Returns the area of the sphere.
        /// </summary>
        public float Area => 4f * math.PI * Radius * Radius;

        public Sphere(float3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <inheritdoc />
        public bool Equals(Sphere other) => math.all(Center == other.Center & Radius == other.Radius);

        /// <inheritdoc />
        public override bool Equals(object other) => throw new NotImplementedException();

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc />
        public static bool operator ==(Sphere lhs, Sphere rhs) => math.all(lhs.Center == rhs.Center & lhs.Radius == rhs.Radius);

        /// <inheritdoc />
        public static bool operator !=(Sphere lhs, Sphere rhs) => !(lhs == rhs);

        /// <summary>
        /// Returns a point on the perimeter of this sphere that is closest to the specified point.
        /// </summary>
        public float3 ClosestPoint(float3 point)
        {
            float3 towards = point - Center;
            float length = math.length(towards);
            if (length < math.EPSILON)
                return point;

            // TODO: Performance check branch vs bursted max
            if (length < Radius)
                return point;

            return Center + Radius*(towards/length);
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(float3 pointb) => ShapeUtility.OverlapSphereAndPoint(this, pointb);

        /// Returns true if shapes surfaces overlap.
        /// </summary>
        //public bool Overlap(Line line) => ShapeUtility.OverlapSphereAndLine(this, line);

        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Sphere sphere) => ShapeUtility.OverlapSphereAndSphere(this, sphere);

        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Box rectangle) => ShapeUtility.OverlapBoxAndSphere(rectangle, this);

        /// <summary>
        /// Returns minimum rectangle that fully covers shape.
        /// </summary>
        public Box BoundingBox() => new Box(Center - Radius, Diameter);

        /// <summary>
        /// Returns minimum bounding sphere that contains both spheres.
        /// </summary>
        public static Sphere Union(Sphere a, Sphere b)
        {
            return new Sphere((a.Center + b.Center) * 0.5f, math.distance(a.Center, b.Center) * 0.5f + math.max(a.Radius, b.Radius));
        }
    }
}
