using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace ProjectDawn.Mathematics
{
    /// <summary>
    /// A static class to contain various math functions.
    /// </summary>
    public static partial class math2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sum(this float2 value) => value.x + value.y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sum(this float3 value) => value.x + value.y + value.z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sum(this float4 value) => value.x + value.y + value.z + value.w;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sum(this int2 value) => value.x + value.y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sum(this int3 value) => value.x + value.y + value.z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sum(this int4 value) => value.x + value.y + value.z + value.w;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sq(this float value) => value * value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sq(this int value) => value * value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 sort(int2 value) => value.x > value.y ? value.yx : value.xy;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sort(float2 value) => value.x > value.y ? value.yx : value.xy;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 sort(int3 value)
        {
            value.xy = sort(value.xy);
            if (value.z < value.x)
                return value.zxy;
            if (value.z < value.y)
                return value.xzy;
            return value.xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sort(float3 value)
        {
            value.xy = sort(value.xy);
            if (value.z < value.x)
                return value.zxy;
            if (value.z < value.y)
                return value.xzy;
            return value.xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 sort(int4 value)
        {
            value.xyz = sort(value.xyz);
            if (value.w < value.x)
                return value.wxyz;
            if (value.w < value.y)
                return value.xwyz;
            if (value.w < value.z)
                return value.xywz;
            return value.xyzw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sort(float4 value)
        {
            value.xyz = sort(value.xyz);
            if (value.w < value.x)
                return value.wxyz;
            if (value.w < value.y)
                return value.xwyz;
            if (value.w < value.z)
                return value.xywz;
            return value.xyzw;
        }
    }
}
