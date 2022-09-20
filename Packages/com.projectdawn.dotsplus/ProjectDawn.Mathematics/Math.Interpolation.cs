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
        /// Inverse lerp returns a fraction, based on a value between start and end values.
        /// As example InvLerp(0.5, 1, 0.75) will result in 0.5, because it is middle of range 0.5 and 1.
        /// This is quite useful function if you want linear falloff that has start and end values.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="value">The value between start and end.</param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float invlerp(float start, float end, float value) => saturate((value - start) / (end - start));

        /// <summary>
        /// Inverse lerp returns a fraction, based on a value between start and end values.
        /// As example InvLerp(0.5, 1, 0.75) will result in 0.5, because it is middle of range 0.5 and 1.
        /// This is quite useful function if you want linear falloff that has start and end values.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="value">The value between start and end.</param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 invlerp(float2 start, float2 end, float2 value) => saturate((value - start) / (end - start));

        /// <summary>
        /// Inverse lerp returns a fraction, based on a value between start and end values.
        /// As example InvLerp(0.5, 1, 0.75) will result in 0.5, because it is middle of range 0.5 and 1.
        /// This is quite useful function if you want linear falloff that has start and end values.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="value">The value between start and end.</param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 invlerp(float3 start, float3 end, float3 value) => saturate((value - start) / (end - start));

        /// <summary>
        /// Returns true if value is barycentric coordinates.
        /// </summary>
        /// <param name="value">Value used for finding if its barycentric coordinates.</param>
        /// <returns>Returns true if value is barycentric coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isbarycentric(float3 value) => (value.x + value.y + value.z) <= 1 - EPSILON;

        /// <summary>
        /// Returns barycentric coordinates of triangle point.
        /// Based on Christer Ericson's Real-Time Collision Detection.
        /// </summary>
        /// <param name="a">Triangle point.</param>
        /// <param name="b">Triangle point.</param>
        /// <param name="c">Triangle point.</param>
        /// <param name="p">Point inside the triangle</param>
        /// <returns>Returns barycentric coordinates of triangle point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 barycentric(float2 a, float2 b, float2 c, float2 p)
        {
            float2 v0 = b - a; 
            float2 v1 = c - a;
            float2 v2 = p - a;

            float d00 = dot(v0, v0);
            float d01 = dot(v0, v1);
            float d11 = dot(v1, v1);
            float d20 = dot(v2, v0);
            float d21 = dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;

            float3 barycentric;
            barycentric.y = (d11 * d20 - d01 * d21) / denom;
            barycentric.z = (d00 * d21 - d01 * d20) / denom;
            barycentric.x = 1.0f - barycentric.z - barycentric.y;
            return barycentric;
        }

        /// <summary>
        /// Returns barycentric coordinates of triangle point.
        /// Based on Christer Ericson's Real-Time Collision Detection.
        /// </summary>
        /// <param name="a">Triangle point.</param>
        /// <param name="b">Triangle point.</param>
        /// <param name="c">Triangle point.</param>
        /// <param name="p">Point inside the triangle</param>
        /// <returns>Returns barycentric coordinates of triangle point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 barycentric(float3 a, float3 b, float3 c, float3 p)
        {
            float3 v0 = b - a;
            float3 v1 = c - a;
            float3 v2 = p - a;

            float d00 = dot(v0, v0);
            float d01 = dot(v0, v1);
            float d11 = dot(v1, v1);
            float d20 = dot(v2, v0);
            float d21 = dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;

            float3 barycentric;
            barycentric.x = (d11 * d20 - d01 * d21) / denom;
            barycentric.y = (d00 * d21 - d01 * d20) / denom;
            barycentric.z = 1.0f - barycentric.x - barycentric.y;
            return barycentric;
        }

        /// <summary>
        /// Returns blended point between triangle points using barycentric coordinates.
        /// It is basically lerp for three points.
        /// </summary>
        /// <param name="a">Triangle point.</param>
        /// <param name="b">Triangle point.</param>
        /// <param name="c">Triangle point.</param>
        /// <param name="barycentric">Barycentric coordinates of triangle point.</param>
        /// <returns>Returns blended point between triangle points using barycentric coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 blend(float2 a, float2 b, float2 c, float3 barycentric) => a * barycentric.x + b * barycentric.y + c * barycentric.z;

        /// <summary>
        /// Returns blended point between triangle points using barycentric coordinates.
        /// It is basically lerp for three points.
        /// </summary>
        /// <param name="a">Triangle point.</param>
        /// <param name="b">Triangle point.</param>
        /// <param name="c">Triangle point.</param>
        /// <param name="barycentric">Barycentric coordinates of triangle point.</param>
        /// <returns>Returns blended point between triangle points using barycentric coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 blend(float3 a, float3 b, float3 c, float3 barycentric) => a * barycentric.x + b * barycentric.y + c * barycentric.z;
    }
}
