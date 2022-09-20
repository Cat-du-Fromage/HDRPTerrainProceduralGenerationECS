using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ProjectDawn.Mathematics
{
    /// <summary>
    /// A static class to contain various fast math functions that has lower precision.
    /// </summary>
    public static partial class fastmath
    {
        /// <summary>
        /// Int and Float shares same memory.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct IntFloatUnion
        {
            [FieldOffset(0)]
            public float f;
            [FieldOffset(0)]
            public int i;
        }

        /// <summary>
        /// Returns 1/sqrt(value).
        /// Based on https://en.wikipedia.org/wiki/Fast_inverse_square_root.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float rsqrt(float value)
        {
            IntFloatUnion u = new IntFloatUnion();
            u.f = value;
            u.i = 1597463174 - (u.i >> 1);
            return u.f * (1.5f - (0.5f * value * u.f * u.f));
        }

        const float RFAC2 = 1f / 2f;
        const float RFAC3 = 1f / 6f;
        const float RFAC4 = 1f / 24f;
        const float RFAC5 = 1f / 120f;
        const float RFAC6 = 1f / 720f;
        const float RFAC7 = 1f / 5040f;

        /// <summary>
        /// Returns cosine of value.
        /// Based on Maclaurin Series 4 iterations https://blogs.ubc.ca/infiniteseriesmodule/units/unit-3-power-series/taylor-series/the-maclaurin-expansion-of-cosx/.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cos(float x)
        {
            float x2 = x * x;
            float x4 = x2 * x2;
            float x6 = x4 * x2;
            // Maclaurin Series
            return 1 - (x2 * RFAC2) + (x4 * RFAC4) - (x6 * RFAC6);
        }

        /// <summary>
        /// Returns cosine of value.
        /// Based on Maclaurin Series 4 iterations https://blogs.ubc.ca/infiniteseriesmodule/units/unit-3-power-series/taylor-series/the-maclaurin-expansion-of-cosx/.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sin(float x)
        {
            float x2 = x * x;
            float x4 = x2 * x2;
            float x6 = x4 * x2;
            // Maclaurin Series
            return x * (1 - (x2 * RFAC3) + (x4 * RFAC5) - (x6 * RFAC7));
        }
    }
}
