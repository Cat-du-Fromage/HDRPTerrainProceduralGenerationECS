using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ProjectDawn.Mathematics
{
    /// <summary>
    /// A static class to contain various math functions.
    /// </summary>
    public static partial class math2
    {
        /// <summary>
        /// Returns perpendicular from right side to direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 perpendicularright(float2 direction) => new float2(direction.y, -direction.x);

        /// <summary>
        /// Returns perpendicular from left side to direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 perpendicularleft(float2 direction) => new float2(-direction.y, direction.x);

        /// <summary>
        /// Returns perpendicular from left side to direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool iscollinear(float2 a, float2 b) => abs(determinant(a, b)) < EPSILON;

        /// <summary>
        /// Returns perpendicular from left side to direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool iscollinear(float3 a, float3 b) => abs(determinant(a, b)) < EPSILON;

        /// <summary>
        /// Returns angle of direction vector.
        /// </summary>
        /// <param name="direction">Direction vector used for finding angle.</param>
        /// <returns>Returns angle of direction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float angle(float2 direction) => atan2(direction.y, direction.x);

        /// <summary>
        /// Returns direction from the angle.
        /// </summary>
        /// <param name="angle">Angle in radians used to construct direction.</param>
        /// <returns>Returns direction from the angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 direction(float angle) => new float2(cos(angle), sin(angle));

        /// <summary>
        /// Returns minimum angle between two direction vectors.
        /// </summary>
        /// <param name="a">Direction vector used for finding angle.</param>
        /// <param name="b">Direction vector used for finding angle.</param>
        /// <returns>Returns minimum angle between two direction vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float angle(float2 a, float2 b) => acos(dot(a, b));

        /// <summary>
        /// Returns minimum angle needed to rotate from direction a to direction b.
        /// </summary>
        /// <param name="a">Direction vector used for finding angle.</param>
        /// <param name="b">Direction vector used for finding angle.</param>
        /// <returns>Returns minimum angle needed to rotate from direction a to direction b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sangle(float2 a, float2 b) => angle(b) - angle(a);

        /// <summary>
        /// Returns point that is rotated by the angle.
        /// </summary>
        /// <param name="value">Point to rotate.</param>
        /// <param name="angle">Rotation angle in radians.</param>
        /// <returns>Returns point that is rotated by the angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 rotate(float2 value, float angle)
        {
            float ca = cos(angle);
            float sa = sin(angle);
            return new float2(ca * value.x - sa * value.y, sa * value.x + ca * value.y);
        }
    }
}
