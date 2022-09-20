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
        /// PI multiplied by two.
        /// </summary>
        public const float PI2 = 6.28318530718F;

        /// <summary>
        /// PI multiplied by two.
        /// </summary>
        public const double PI2_D = 6.2831853071795864769;

        /// <summary>
        /// Returns cross product of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cross(float2 a, float2 b) => new float2(a.x * b.y,  - a.y * b.x);

        /// <summary>
        /// Returns determinant of two vectors.
        /// Sum of cross product elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float determinant(float2 a, float2 b) => a.x * b.y - a.y * b.x;

        /// <summary>
        /// Returns determinant of two vectors.
        /// Sum of cross product elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float determinant(float3 a, float3 b)
        {
            return ((a.y * b.z) - (a.z * b.y)) - ((a.z * b.x) - (a.x * b.z)) + ((a.x * b.y) - (a.y * b.x));
        }

        /// <summary>
        /// Returns true if points ordered counter clockwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool iscclockwise(float2 a, float2 b, float2 c) => determinant(c - a, b - a) < 0;

        /// <summary>
        /// Returns true if points ordered counter clockwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool iscclockwise(float3 a, float3 b, float3 c) => determinant(c - a, b - a) < 0;

        /// <summary>
        /// Returns true if points ordered clockwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isclockwise(float2 a, float2 b, float2 c) => determinant(c - a, b - a) > 0;

        /// <summary>
        /// Returns true if points ordered clockwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isclockwise(float3 a, float3 b, float3 c) => determinant(c - a, b - a) > 0;

        /// <summary>
        /// Returns true if valid triangle exists knowing three edge lengths.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool istriangle(float a, float b, float c)
        {
            // Sum of two triangle edge is always lower than third
            return all(new bool3(
                a + b > c,
                a + c > b,
                b + c > a));
        }

        /// <summary>
        /// Returns if quad meets the Delaunay condition. Where a, b, c forms clockwise sorted triangle.
        /// Based on https://en.wikipedia.org/wiki/Delaunay_triangulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isdelaunay(float2 a, float2 b, float2 c, float2 d)
        {
            float2 ad = a - d;
            float2 bd = b - d;
            float2 cd = c - d;

            float2 d2 = d * d;

            float2 ad2 = a * a - d2;
            float2 bd2 = b * b - d2;
            float2 cd2 = c * c - d2;

            float determinant = math.determinant(new float3x3(
                new float3(ad.x, ad.y, ad2.x + ad2.y),
                new float3(bd.x, bd.y, bd2.x + bd2.y),
                new float3(cd.x, cd.y, cd2.x + cd2.y)
                ));

            return determinant >= 0;
        }

        /// <summary>
        /// Returns factorial of the value (etc 0! = 1, 1! = 1, 2! = 2, 3! = 6, 4! = 24 ...)
        /// Based on https://en.wikipedia.org/wiki/Factorial.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int factorial(int value)
        {
            int factorial = 1;
            int count = value + 1;
            for (int i = 1; i < count; ++i)
                factorial *= i;
            return factorial;
        }

        /// <summary>
        /// Exchanges the values of a and b.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }
    }
}
