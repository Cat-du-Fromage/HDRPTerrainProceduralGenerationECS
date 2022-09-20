using NUnit.Framework;
using Unity.Mathematics;
using static ProjectDawn.Mathematics.math2;

namespace ProjectDawn.Mathematics.Tests
{
    internal class MathTests
    {
        [Test]
        public unsafe void MathTests_DirectionToAngle()
        {
            Assert.AreEqual(math.radians(0), math2.angle(new float2(1, 0)));
            Assert.AreEqual(math.radians(90), math2.angle(new float2(0, 1)));
            Assert.AreEqual(math.radians(180), math2.angle(new float2(-1, 0)));
            Assert.AreEqual(math.radians(-90), math2.angle(new float2(0, -1)));
        }

        [Test]
        public unsafe void MathTests_Perpendicular()
        {
            AreEqual(new float2(0, 1), math2.perpendicularleft(new float2(1, 0)));
            AreEqual(new float2(0, -1), math2.perpendicularright(new float2(1, 0)));
        }

        [Test]
        public unsafe void MathTests_AngleToDirection()
        {
            AreEqual(new float2(1, 0), math2.direction(math.radians(0)));
            AreEqual(new float2(0, 1), math2.direction(math.radians(90)));
            AreEqual(new float2(-1, 0), math2.direction(math.radians(180)));
            AreEqual(new float2(0, -1), math2.direction(math.radians(270)));
        }

        [Test]
        public unsafe void MathTests_AngleBetween()
        {
            Assert.AreEqual(math.radians(45), math2.angle(math2.direction(math.radians(0)), math2.direction(math.radians(45))));
            Assert.AreEqual(math.radians(90), math2.angle(math2.direction(math.radians(90)), math2.direction(math.radians(0))));
            Assert.AreEqual(math.radians(170), math2.angle(math2.direction(math.radians(190)), math2.direction(math.radians(0))));
            Assert.AreEqual(math.radians(45), math2.sangle(math2.direction(math.radians(0)), math2.direction(math.radians(45))));
        }

        [Test]
        public unsafe void MathTests_AngleToRotate()
        {
            Assert.AreEqual(math.radians(45), math2.sangle(math2.direction(math.radians(0)), math2.direction(math.radians(45))));
            Assert.AreEqual(-math.radians(90), math2.sangle(math2.direction(math.radians(90)), math2.direction(math.radians(0))));
            Assert.AreEqual(math.radians(170), math2.sangle(math2.direction(math.radians(190)), math2.direction(math.radians(0))));
            Assert.AreEqual(-math.radians(45), math2.sangle(math2.direction(math.radians(0)), math2.direction(-math.radians(45))));
        }

        [Test]
        public unsafe void MathTests_Rotate()
        {
            AreEqual(new float2(0, 1), math2.rotate(new float2(1, 0), math.radians(90)));
        }

        [Test]
        public unsafe void MathTests_InvLerp()
        {
            float lerp = math.lerp(0.5f, 1, 0.5f);
            Assert.AreEqual(0.75f, lerp);

            float invLerp = math2.invlerp(0.5f, 1, lerp);
            Assert.AreEqual(0.5f, invLerp);

            Assert.AreEqual(1, math2.invlerp(1, 1, 1));
            Assert.AreEqual(0, math2.invlerp(1, 1, 0));
        }

        [Test]
        public unsafe void MathTests_Barycentric()
        {
            float2 a = math2.direction(math.radians(0));
            float2 b = math2.direction(math.radians(135));
            float2 c = math2.direction(math.radians(225));

            AreEqual(new float3(0.5f, 0.5f, 0), math2.barycentric(a, b, c, new float2((a + b) / 2)));
            AreEqual(new float3(0, 0.5f, 0.5f), math2.barycentric(a, b, c, new float2((b + c) / 2)));
            AreEqual(new float3(0.5f, 0, 0.5f), math2.barycentric(a, b, c, new float2((a + c) / 2)));

            AreEqual(new float3(1, 0, 0), math2.barycentric(a, b, c, a));
            AreEqual(new float3(0, 1, 0), math2.barycentric(a, b, c, b));
            AreEqual(new float3(0, 0, 1), math2.barycentric(a, b, c, c));

            AreEqual(1f / 3f, math2.barycentric(a, b, c, new float2((a + b + c) / 3)));
        }

        [Test]
        public unsafe void MathTests_Blend()
        {
            float2 a = math2.direction(math.radians(0));
            float2 b = math2.direction(math.radians(135));
            float2 c = math2.direction(math.radians(225));

            AreEqual(a, math2.blend(a, b, c, new float3(1, 0, 0)));
            AreEqual(b, math2.blend(a, b, c, new float3(0, 1, 0)));
            AreEqual(c, math2.blend(a, b, c, new float3(0, 0, 1)));
        }

        [Test]
        public void MathTests_Factorial()
        {
            Assert.AreEqual(1, math2.factorial(0));
            Assert.AreEqual(1, math2.factorial(1));
            Assert.AreEqual(2, math2.factorial(2));
            Assert.AreEqual(6, math2.factorial(3));
            Assert.AreEqual(24, math2.factorial(4));
            Assert.AreEqual(120, math2.factorial(5));
        }

        [Test]
        public void MathTests_Even()
        {
            Assert.IsTrue(math.all(new float2(1f, 2f).even() == new bool2(false, true)));
        }

        [Test]
        public void MathTests_Odd()
        {
            Assert.IsTrue(math.all(new float2(1f, 2f).odd() == new bool2(true, false)));
        }

        [Test]
        public void MathTests_Sum()
        {
            Assert.AreEqual(1 + 2 + 3 + 4, math2.sum(new float4(1, 2, 3, 4)));
        }

        [Test]
        public void MathTests_IsCollinear()
        {
            Assert.IsTrue(iscollinear(new float2(1, 0), new float2(1, 0)));
            Assert.IsTrue(iscollinear(new float2(1, 1), new float2(1, 1)));
            Assert.IsTrue(iscollinear(new float2(1, 1), new float2(-1, -1)));

            Assert.IsTrue(iscollinear(new float3(1, 0, 0), new float3(1, 0, 0)));
            Assert.IsTrue(iscollinear(new float3(1, 1, 1), new float3(1, 1, 1)));
            Assert.IsTrue(iscollinear(new float3(1, 1, 1), new float3(-1, -1, -1)));
        }

        [Test]
        public void MathTests_IsDelaunay()
        {
            Assert.IsTrue(isdelaunay(new float2(-1, 1), new float2(-1, -1), new float2(1, -1), new float2(1, 1)));
            Assert.IsTrue(isdelaunay(new float2(-1, 1), new float2(-1, -1), new float2(1, -1), new float2(0.8f, 0.8f)));
            Assert.IsFalse(isdelaunay(new float2(-1, 1), new float2(-1, -1), new float2(1, -1), new float2(2, 2)));
        }

        [Test]
        public void MathTests_Sort()
        {
            AreEqual(new float2(1, 2), sort(new float2(1, 2)));
            AreEqual(new float2(1, 2), sort(new float2(2, 1)));

            AreEqual(new float3(1, 2, 3), sort(new float3(1, 2, 3)));
            AreEqual(new float3(1, 2, 3), sort(new float3(2, 1, 3)));
            AreEqual(new float3(1, 2, 3), sort(new float3(2, 3, 1)));
            AreEqual(new float3(1, 2, 3), sort(new float3(3, 1, 2)));
            AreEqual(new float3(1, 2, 3), sort(new float3(3, 2, 1)));

            AreEqual(new float4(1, 2, 3, 4), sort(new float4(1, 2, 3, 4)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(2, 1, 3, 4)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(2, 3, 1, 4)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(3, 1, 2, 4)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(3, 2, 1, 4)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(4, 1, 2, 3)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(4, 2, 1, 3)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(4, 2, 3, 1)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(4, 3, 1, 2)));
            AreEqual(new float4(1, 2, 3, 4), sort(new float4(4, 3, 2, 1)));
        }

        [Test]
        public void MathTests_IsTriangle()
        {
            Assert.IsTrue(istriangle(1, 1, math.sqrt(2))); // Triangle
            Assert.IsFalse(istriangle(1, 1, 2)); // Line
            Assert.IsFalse(istriangle(1, 1, 0)); // Line
            Assert.IsFalse(istriangle(0, 0, 0)); // Point
        }

        static void AreEqual(float2 expected, float2 actual, float delta = 0.00001f)
        {
            Assert.AreEqual(expected.x, actual.x, delta);
            Assert.AreEqual(expected.y, actual.y, delta);
        }

        static void AreEqual(float3 expected, float3 actual, float delta = 0.00001f)
        {
            Assert.AreEqual(expected.x, actual.x, delta);
            Assert.AreEqual(expected.y, actual.y, delta);
            Assert.AreEqual(expected.z, actual.z, delta);
        }

        static void AreEqual(float4 expected, float4 actual, float delta = 0.00001f)
        {
            Assert.AreEqual(expected.x, actual.x, delta);
            Assert.AreEqual(expected.y, actual.y, delta);
            Assert.AreEqual(expected.z, actual.z, delta);
            Assert.AreEqual(expected.w, actual.w, delta);
        }
    }
}
