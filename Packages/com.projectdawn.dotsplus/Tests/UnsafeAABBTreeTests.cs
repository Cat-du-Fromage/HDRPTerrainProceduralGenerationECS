using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using System;
using ProjectDawn.Collections.LowLevel.Unsafe;
using ProjectDawn.Geometry2D;

namespace ProjectDawn.Collections.Tests
{
    struct AABRectangle : ISurfaceArea<AABRectangle>, IUnion<AABRectangle>
    {
        public Rectangle Rectangle;

        public AABRectangle(Rectangle rectangle)
        {
            Rectangle = rectangle;
        }
        public float SurfaceArea() => Rectangle.Perimeter;
        public AABRectangle Union(AABRectangle value) => new AABRectangle(Rectangle.Union(Rectangle, value.Rectangle));
    }

    internal class UnsafeAABBTreeTests
    {
        [Test]
        public void UnsafeAABBTreeTests_Rectangle_RemoveAt()
        {
            var tree = new UnsafeAABBTree<AABRectangle>(1, Allocator.Temp);

            // Construct tree:
            //            N
            //          /   \
            //         N     N
            //        / \   / \
            //       a   b  c  d

            var a = tree.Add(new AABRectangle(new Rectangle(new float2(5, 5), new float2(1, 1))));
            var b = tree.Add(new AABRectangle(new Rectangle(new float2(6, 6), new float2(1, 1))));

            var c = tree.Add(new AABRectangle(new Rectangle(new float2(-5, -5), new float2(1, 1))));
            var d = tree.Add(new AABRectangle(new Rectangle(new float2(-6, -6), new float2(1, 1))));

            Assert.AreEqual(tree.Right(tree.Right(tree.Root)), d);
            Assert.AreEqual(tree.Left(tree.Right(tree.Root)), c);
            Assert.AreEqual(tree.Right(tree.Left(tree.Root)), b);
            Assert.AreEqual(tree.Left(tree.Left(tree.Root)), a);

            // Remove d
            //            N
            //          /   \
            //         N     c
            //        / \
            //       a   b

            tree.RemoveAt(d);

            Assert.AreEqual(tree.Right(tree.Root), c);
            Assert.AreEqual(tree.Right(tree.Left(tree.Root)), b);
            Assert.AreEqual(tree.Left(tree.Left(tree.Root)), a);

            // Remove c
            //         N
            //        / \
            //       a   b

            tree.RemoveAt(c);

            Assert.AreEqual(tree.Right(tree.Root), b);
            Assert.AreEqual(tree.Left(tree.Root), a);

            // Remove a
            //         b

            tree.RemoveAt(a);

            Assert.AreEqual(tree.Root, b);

            // Remove b

            tree.RemoveAt(b);

            Assert.IsFalse(tree.Root.Valid);
            Assert.IsTrue(tree.IsEmpty);

            tree.Dispose();
        }
    }
}
