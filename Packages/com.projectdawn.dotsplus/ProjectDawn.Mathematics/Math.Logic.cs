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
        public static bool even(this float value) => ((int)value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 even(this float2 value) => ((int2)value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 even(this float3 value) => ((int3)value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 even(this float4 value) => ((int4)value & 1) != 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool even(this int value) => (value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 even(this int2 value) => (value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 even(this int3 value) => (value & 1) != 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 even(this int4 value) => (value & 1) != 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool odd(this float value) => ((int)value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 odd(this float2 value) => ((int2)value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 odd(this float3 value) => ((int3)value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 odd(this float4 value) => ((int4)value & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool odd(this int value) => (value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 odd(this int2 value) => (value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 odd(this int3 value) => (value & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 odd(this int4 value) => (value & 1) != 0;
    }
}
