using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Line segment that has start and end points.
    /// </summary>
    [DebuggerDisplay("From = {From}, To = {To}")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Line : IEquatable<Line>
    {
        /// <summary>
        /// Line start position.
        /// </summary>
        public float2 From;

        /// <summary>
        /// Line end position.
        /// </summary>
        public float2 To;

        /// <summary>
        /// Line vector.
        /// </summary>
        public float2 Towards => To - From;

        /// <summary>
        /// Returns direction of the line.
        /// </summary>
        public float2 Direction => math.normalize(Towards);

        /// <summary>
        /// Mid point of the line.
        /// </summary>
        public float2 MidPoint => (To + From) * 0.5f;

        /// <summary>
        /// Returns length of the line.
        /// </summary>
        public float Length => math.distance(To, From);

        public Line(float2 from, float2 to)
        {
            From = from;
            To = to;
        }

        /// <inheritdoc />
        public bool Equals(Line other) => math.all(From == other.From & To == other.To);

        /// <inheritdoc />
        public override bool Equals(object other) => throw new NotImplementedException();

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc />
        public static bool operator ==(Line lhs, Line rhs) => math.all(lhs.From == rhs.From & lhs.To == rhs.To);

        /// <inheritdoc />
        public static bool operator !=(Line lhs, Line rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public static implicit operator Line(float value) => new Line(value, value);

        /// <summary>
        /// Returns a point on the perimeter of this rectangle that is closest to the specified point.
        /// </summary>
        public float2 ClosestPoint(float2 point)
        {
            float2 towards = Towards;

            float lengthSq = math.lengthsq(towards);
            if (lengthSq < math.EPSILON)
                return point;

            float t = math.dot(point - From, towards) / lengthSq;

            // Force within the segment
            t = math.saturate(t);

            return From + t * towards;
        }

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public float Distance(float2 point) => math.distance(ClosestPoint(point), point);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Line line) => ShapeUtility.OverlapLineAndLine(this, line);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Circle circle) => ShapeUtility.OverlapCircleAndLine(circle, this);

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public bool Overlap(Rectangle rectangle) => ShapeUtility.OverlapRectangleAndLine(rectangle, this);

        /// <summary>
        /// Converts to ray.
        /// </summary>
        public Ray ToRay() => new Ray(From, Towards);

        /// <summary>
        /// Returns minimum rectangle that fully covers shape.
        /// </summary>
        public Rectangle BoundingRectangle()
        {
            var min = math.min(From, To);
            var max = math.max(From, To);
            return new Rectangle(min, max - min);
        }

        /// <summary>
        /// Returns minimum circle that fully covers shape.
        /// </summary>
        public Circle CircumscribedCircle()
        {
            return new Circle(MidPoint, math.length(Towards) * 0.5f);
        }
    }
}
