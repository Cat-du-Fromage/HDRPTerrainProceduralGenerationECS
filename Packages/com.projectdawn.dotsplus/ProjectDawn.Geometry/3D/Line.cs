using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

namespace ProjectDawn.Geometry3D
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
        public float3 From;

        /// <summary>
        /// Line end position.
        /// </summary>
        public float3 To;

        /// <summary>
        /// Line vector.
        /// </summary>
        public float3 Towards => To - From;

        /// <summary>
        /// Returns direction of the line.
        /// </summary>
        public float3 Direction => math.normalize(Towards);

        /// <summary>
        /// Mid point of the line.
        /// </summary>
        public float3 MidPoint => (To + From) * 0.5f;

        /// <summary>
        /// Returns length of the line.
        /// </summary>
        public float Length => math.distance(To, From);

        public Line(float3 from, float3 to)
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
        /// Converts to ray.
        /// </summary>
        public Ray ToRay() => new Ray(From, Towards);

        /// <summary>
        /// Returns true if line intersects triangle.
        /// </summary>
        /// <param name="triangle">Triangle.</param>
        /// <param name="point">Intersection point.</param>
        /// <returns>Returns true if line intersects triangle.</returns>
        public bool Intersection(Triangle triangle, out float3 point) => ShapeUtility.IntersectionLineAndTriangle(this, triangle, out point);
    }
}
