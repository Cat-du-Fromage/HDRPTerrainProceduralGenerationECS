using NUnit.Framework;
using Unity.Mathematics;
using Unity.Collections;

namespace ProjectDawn.Geometry2D.Tests
{
    internal class ConvexPolygonTests
    {
        struct Transform : ITransformFloat2
        {
            float2 ITransformFloat2.Transform(float2 point) => point;
        }

        [Test]
        public void ConvexPolygonTests_Rectangle_IsValid_NonConvex()
        {
            var polygon = new ConvexPolygon<Transform>(4, Allocator.Temp);

            polygon[0] = new float2(-1, 1);
            polygon[1] = new float2(-1, -1);
            polygon[3] = new float2(1, -1);
            polygon[2] = new float2(1, 1);

            Assert.IsFalse(polygon.IsValid());

            polygon.Dispose();
        }

        [Test]
        public void ConvexPolygonTests_Rectangle_IsValid_NonCounterClockwise()
        {
            var polygon = new ConvexPolygon<Transform>(4, Allocator.Temp);

            polygon[3] = new float2(-1, 1);
            polygon[2] = new float2(-1, -1);
            polygon[1] = new float2(1, -1);
            polygon[0] = new float2(1, 1);

            Assert.IsFalse(polygon.IsValid());

            polygon.Dispose();
        }

        [Test]
        public void ConvexPolygonTests_Triangle_IsValid_NonCounterClockwise()
        {
            var polygon = new ConvexPolygon<Transform>(3, Allocator.Temp);

            polygon[2] = new float2(-1, -1);
            polygon[1] = new float2(1, -1);
            polygon[0] = new float2(1, 1);

            Assert.IsFalse(polygon.IsValid());

            polygon.Dispose();
        }

        [Test]
        public void ConvexPolygonTests_Rectangle_Area()
        {
            var polygon = new ConvexPolygon<Transform>(4, Allocator.Temp);

            // Rectangle
            var rectangle = new Rectangle(new float2(-1, -1), new float2(2, 2));
            polygon[0] = new float2(-1, 1);
            polygon[1] = new float2(-1, -1);
            polygon[2] = new float2(1, -1);
            polygon[3] = new float2(1, 1);

            Assert.IsTrue(polygon.IsValid());
            Assert.AreEqual(rectangle.Area, polygon.GetArea());
            Assert.AreEqual(rectangle.Center.x, polygon.GetCentroid().x);
            Assert.AreEqual(rectangle.Center.y, polygon.GetCentroid().y);

            polygon.Dispose();
        }
    }
}
