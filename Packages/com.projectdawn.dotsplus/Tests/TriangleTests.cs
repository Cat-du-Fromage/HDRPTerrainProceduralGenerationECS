using NUnit.Framework;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ProjectDawn.Geometry3D.Tests
{
    internal class TriangleTests
    {
        [Test]
        public void TriangleTests_Normal()
        {
            var triangle = new Triangle(new float3(0, 0, 0), new float3(2, 2, 0), new float3(2, 0, 0));
            Assert.AreEqual(new float3(0, 0, 1), triangle.Normal);
        }

        [Test]
        public void TriangleTests_Area()
        {
            var triangle = new Triangle(new float3(0, 0, 0), new float3(2, 2, 0), new float3(2, 0, 0));
            Assert.AreEqual(2, triangle.Area);
        }

        [Test]
        public void TriangleTests_Perimeter()
        {
            var triangle = new Triangle(new float3(0, 0, 0), new float3(2, 2, 0), new float3(2, 0, 0));
            Assert.AreEqual(2 + 2 + sqrt(8), triangle.Perimeter);
        }

        [Test]
        public void TriangleTests_BoundingBox()
        {
            var triangle = new Triangle(new float3(0, 0, 0), new float3(2, 2, 2), new float3(2, 0, 0));
            Assert.IsTrue(new Box(0, 2) == triangle.BoundingBox());
        }

        [Test]
        public void TriangleTests_IsCounterClockwise()
        {
            Assert.IsTrue(new Triangle(new float3(0, 0, 0), new float3(1, 1, 0), new float3(1, 0, 0)).IsClockwise());
            Assert.IsTrue(new Triangle(new float3(1, 0, 0), new float3(1, 1, 0), new float3(0, 0, 0)).IsCounterClockwise());
        }

        [Test]
        public void TriangleTests_IsValid()
        {
            Assert.IsTrue(new Triangle(new float3(0, 0, 0), new float3(2, 2, 2), new float3(2, 0, 0)).IsValid());
            Assert.IsFalse(new Triangle(new float3(0, 0, 0), new float3(0, 0, 0), new float3(2, 0, 0)).IsValid());
        }
    }
}
