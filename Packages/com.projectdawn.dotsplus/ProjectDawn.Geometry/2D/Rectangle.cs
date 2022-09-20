using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// A rectangle shape.
    /// </summary>
    [DebuggerDisplay("Position = {Position}, Size = {Size}")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Rectangle : IEquatable<Rectangle>
    {
        /// <summary>
        /// The minimum point of rectangle.
        /// </summary>
        public float2 Position;

        /// <summary>
        /// The size of the rectangle.
        /// </summary>
        public float2 Size;

        /// <summary>
        /// The half size of the rectangle.
        /// </summary>
        public float2 Extent
        {
            get => Size * 0.5f;
            set => Size = value * 2;
        }

        /// <summary>
        /// The center of the rectangle.
        /// </summary>
        public float2 Center
        {
            get => Position + Extent;
            set => Position = value - Extent;
        }

        /// <summary>
        /// The minimum point of the rectangle.
        /// </summary>
        public float2 Min
        {
            get => Position;
            set => Position = value;
        }

        /// <summary>
        /// The maximum point of the rectangle.
        /// </summary>
        public float2 Max
        {
            get => Position + Size;
            set => Position = value - Size;
        }

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public float Width
        {
            get => Size.x;
            set => Size.x = value;
        }

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public float Height
        {
            get => Size.y;
            set => Size.y = value;
        }

        /// <summary>
        /// Returns perimeter of the rectangle.
        /// </summary>
        public float Perimeter => 2f * (Size.x + Size.y);

        /// <summary>
        /// Returns area of the rectangle.
        /// </summary>
        public float Area => Size.x * Size.y;

        public Rectangle(float2 position, float2 size)
        {
            Position = position;
            Size = size;
        }

        /// <inheritdoc />
        public bool Equals(Rectangle other) => math.all(Position == other.Position & Size == other.Size);

        /// <inheritdoc />
        public override bool Equals(object other) => throw new NotImplementedException();

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc />
        public static bool operator ==(Rectangle lhs, Rectangle rhs) => math.all(lhs.Position == rhs.Position & lhs.Size == rhs.Size);

        /// <inheritdoc />
        public static bool operator !=(Rectangle lhs, Rectangle rhs) => !(lhs == rhs);

        /// <summary>
        /// Returns a point on the perimeter of this rectangle that is closest to the specified point.
        /// </summary>
        public float2 ClosestPoint(float2 point) => math.clamp(point, Min, Max);

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public float Distance(Circle circle) => ShapeUtility.DistanceRectangleAndCircle(this, circle);

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public float Distance(Rectangle rectangle) => ShapeUtility.DistanceRectangleAndRectangle(this, rectangle);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(float2 point) => ShapeUtility.OverlapRectangleAndPoint(this, point);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Line line) => ShapeUtility.OverlapRectangleAndLine(this, line);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Circle circle) => ShapeUtility.OverlapRectangleAndCircle(this, circle);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Rectangle rectangle) => ShapeUtility.OverlapRectangleAndRectangle(this, rectangle);

        /// <summary>
        /// Returns rectangle lines.
        /// </summary>
        public void GetLines(out Line a, out Line b, out Line c, out Line d) => ShapeUtility.RectangleLines(this, out a, out b, out c, out d);

        /// <summary>
        /// Returns rectangle points in clockwise order. First point is rectangle position.
        /// </summary>
        public void GetPoints(out float2 a, out float2 b, out float2 c, out float2 d)
        {
            var min = Min;
            var max = Max;
            a = new float2(min.x, min.y);
            b = new float2(min.x, max.y);
            c = new float2(max.x, max.y);
            d = new float2(max.x, min.y);
        }

        /// <summary>
        /// Returns minimum circle that fully covers shape.
        /// </summary>
        public Circle CircumscribedCircle() => new Circle(Center, math.length(Size) * 0.5f);

        /// <summary>
        /// Returns maximum circle that is inside the shape.
        /// </summary>
        public Circle InscribedCircle() => new Circle(Center, math.min(Size.x, Size.y) * 0.5f);

        /// <summary>
        /// Returns minimum bounding rectangle that contains both rectangles.
        /// </summary>
        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            float2 min = math.min(a.Min, b.Min);
            float2 max = math.max(a.Max, b.Max);
            return new Rectangle(min, max - min);
        }
    }
}
