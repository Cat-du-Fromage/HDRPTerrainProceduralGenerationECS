using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ProjectDawn.Geometry3D
{
    /// <summary>
    /// A box shape.
    /// </summary>
    [DebuggerDisplay("Position = {Position}, Size = {Size}")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Box : IEquatable<Box>
    {
        /// <summary>
        /// The minimum point of box.
        /// </summary>
        public float3 Position;

        /// <summary>
        /// The size of the box.
        /// </summary>
        public float3 Size;

        /// <summary>
        /// The half size of the box.
        /// </summary>
        public float3 Extent
        {
            get => Size * 0.5f;
            set => Size = value * 2;
        }

        /// <summary>
        /// The center of the box.
        /// </summary>
        public float3 Center
        {
            get => Position + Extent;
            set => Position = value - Extent;
        }

        /// <summary>
        /// The minimum point of the box.
        /// </summary>
        public float3 Min
        {
            get => Position;
            set => Position = value;
        }

        /// <summary>
        /// The maximum point of the box.
        /// </summary>
        public float3 Max
        {
            get => Position + Size;
            set => Position = value - Size;
        }

        /// <summary>
        /// The width of the box.
        /// </summary>
        public float Width
        {
            get => Size.x;
            set => Size.x = value;
        }

        /// <summary>
        /// The height of the box.
        /// </summary>
        public float Height
        {
            get => Size.y;
            set => Size.y = value;
        }

        /// <summary>
        /// Returns volume of the box.
        /// </summary>
        public float Volume => Size.x * Size.y * Size.z;

        /// <summary>
        /// Returns area of the box.
        /// </summary>
        public float Area => 2f * (Size.x * Size.y + Size.y * Size.z + Size.y * Size.x);

        public Box(float3 position, float3 size)
        {
            Position = position;
            Size = size;
        }

        /// <inheritdoc />
        public bool Equals(Box other) => math.all(Position == other.Position & Size == other.Size);

        /// <inheritdoc />
        public override bool Equals(object other) => throw new NotImplementedException();

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc />
        public static bool operator ==(Box lhs, Box rhs) => math.all(lhs.Position == rhs.Position & lhs.Size == rhs.Size);

        /// <inheritdoc />
        public static bool operator !=(Box lhs, Box rhs) => !(lhs == rhs);

        /// <summary>
        /// Returns a point on the perimeter of this box that is closest to the specified point.
        /// </summary>
        public float3 ClosestPoint(float3 point) => math.clamp(point, Min, Max);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(float3 point) => ShapeUtility.OverlapBoxAndPoint(this, point);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
       //public bool Overlap(Line line) => ShapeUtility.OverlapBoxAndLine(this, line);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Sphere sphere) => ShapeUtility.OverlapBoxAndSphere(this, sphere);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Box box) => ShapeUtility.OverlapBoxAndBox(this, box);

        /// <summary>
        /// Returns minimum sphere that fully covers shape.
        /// </summary>
        public Sphere CircumscribedSphere() => new Sphere(Center, math.length(Size) * 0.5f);

        /// <summary>
        /// Returns maximum sphere that is inside the shape.
        /// </summary>
        public Sphere InscribedSphere() => new Sphere(Center, math.min(Size.x, Size.y) * 0.5f);

        /// <summary>
        /// Returns minimum bounding box that contains both boxes.
        /// </summary>
        public static Box Union(Box a, Box b)
        {
            float3 min = math.min(a.Min, b.Min);
            float3 max = math.max(a.Max, b.Max);
            return new Box(min, max - min);
        }
    }
}
