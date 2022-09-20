using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using System;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.Tests
{
    struct TreeIntComparer : IKdTreeComparer<int>
    {
        public int Compare(int x, int y, int depth) => x.CompareTo(y);

        public float DistanceSq(int x, int y)
        {
            throw new NotImplementedException();
        }

        public float DistanceToSplitSq(int x, int y, int depth)
        {
            throw new NotImplementedException();
        }
    }

    internal class UnsafeKdTreeTests
    {
        [Test]
        public unsafe void KdTreeTests_Int_Add()
        {
            var tree = new UnsafeKdTree<int, TreeIntComparer>(1, Allocator.TempJob, new TreeIntComparer());

            tree.Add(5);
            tree.Add(10);
            tree.Add(2);
            tree.Add(3);

            Assert.AreEqual(tree[tree.Root], 5);
            Assert.AreEqual(tree[tree.Left(tree.Root)], 2);
            Assert.AreEqual(tree[tree.Right(tree.Root)], 10);
            Assert.AreEqual(tree[tree.Right(tree.Left(tree.Root))], 3);

            tree.Dispose();
        }

        [Test]
        public unsafe void KdTreeTests_Int_GetDepth()
        {
            var tree = new UnsafeKdTree<int, TreeIntComparer>(1, Allocator.TempJob, new TreeIntComparer());

            tree.Add(5);
            tree.Add(10);
            tree.Add(2);
            tree.Add(3);
            tree.Add(6);
            tree.Add(8);

            Assert.AreEqual(tree.GetDepth(), 3);

            tree.Dispose();
        }

        [Test]
        public unsafe void KdTreeTests_Int_Build()
        {
            var tree = new UnsafeKdTree<int, TreeIntComparer>(1, Allocator.TempJob, new TreeIntComparer());

            var list = new NativeList<int>(Allocator.Temp);
            list.Add(5);
            list.Add(10);
            list.Add(2);
            list.Add(3);
            tree.Build(list.AsArray());
            list.Dispose();

            Assert.AreEqual(tree[tree.Root], 5);
            Assert.AreEqual(tree[tree.Left(tree.Root)], 3);
            Assert.AreEqual(tree[tree.Right(tree.Root)], 10);
            Assert.AreEqual(tree[tree.Left(tree.Left(tree.Root))], 2);

            tree.Dispose();
        }

        [Test]
        public unsafe void KdTreeTests_Int_RemoveAt()
        {
            var tree = new UnsafeKdTree<int, TreeIntComparer>(1, Allocator.TempJob, new TreeIntComparer());

            tree.Add(5);
            tree.Add(10);
            var iterator = tree.Add(2);
            tree.Add(3);

            tree.RemoveAt(iterator);

            Assert.AreEqual(tree[tree.Root], 5);
            Assert.AreEqual(tree[tree.Left(tree.Root)], 3);
            Assert.AreEqual(tree[tree.Right(tree.Root)], 10);

            tree.Dispose();
        }

        [Test]
        public unsafe void KdTreeTests_Int_RemoveAt_Head()
        {
            var tree = new UnsafeKdTree<int, TreeIntComparer>(1, Allocator.TempJob, new TreeIntComparer());

            var iterator = tree.Add(5);
            tree.Add(10);
            tree.Add(2);
            tree.Add(3);

            tree.RemoveAt(iterator);

            Assert.AreEqual(tree[tree.Root], 2);
            Assert.AreEqual(tree[tree.Right(tree.Root)], 3);
            Assert.AreEqual(tree[tree.Right(tree.Right(tree.Root))], 10);

            tree.Dispose();
        }

        struct TreeComparer : IKdTreeComparer<float2>
        {
            public int Compare(float2 x, float2 y, int depth)
            {
                int axis = depth % 2;
                return x[axis].CompareTo(y[axis]);
            }

            public float DistanceSq(float2 x, float2 y)
            {
                return math.distancesq(x, y);
            }

            public float DistanceToSplitSq(float2 x, float2 y, int depth)
            {
                int axis = depth % 2;
                return (x[axis] - y[axis]) * (x[axis] - y[axis]);
            }
        }

        [Test]
        public unsafe void KdTreeTests_Float2_FindNearest()
        {
            var tree = new UnsafeKdTree<float2, TreeComparer>(1, Allocator.TempJob, new TreeComparer());

            var rnd = new Random(1);

            var list = new NativeList<float2>(Allocator.Temp);
            for (int i = 0; i < 5000; i++)
            {
                var point = rnd.NextFloat2(-20, 20);
                list.Add(point);
                tree.Add(point);
            }

            for (int i = 0; i < 100; i++)
            {
                var target = rnd.NextFloat2(-20, 20);

                var bruteForceResult = FindNearestBruteForce(list, target);
                var treeResult = tree.FindNearest(target, out var count);
                Assert.AreEqual(bruteForceResult, tree[treeResult]);
            }

            list.Dispose();
            tree.Dispose();
        }

        [Test]
        public unsafe void KdTreeTests_Float2_FindRadius()
        {
            float maxDistance = 1;

            var tree = new UnsafeKdTree<float2, TreeComparer>(1, Allocator.TempJob, new TreeComparer());

            var rnd = new Random(1);

            var list = new NativeList<float2>(Allocator.Temp);
            for (int i = 0; i < 5000; i++)
            {
                var point = rnd.NextFloat2(-20, 20);
                list.Add(point);
                tree.Add(point);
            }

            var points = new NativeList<float2>(Allocator.Temp);

            for (int i = 0; i < 100; i++)
            {
                var target = rnd.NextFloat2(-20, 20);

                // Brute force the num nearest
                int numNearest = 0;
                foreach (var point in list)
                {
                    var distance = math.distance(target, point);
                    if (distance <= maxDistance)
                    {
                        numNearest++;
                    }
                }

                // Find using tree
                tree.FindRadius(target, maxDistance, ref points, out int count, 200);

                // Check if brute force and tree found same amount points
                Assert.AreEqual(numNearest, points.Length);

                // Check if all points withing max distance
                foreach (var point in points)
                {
                    var distance = math.distance(target, point);
                    Assert.IsTrue(distance <= maxDistance);
                }
            }

            points.Dispose();

            list.Dispose();
            tree.Dispose();
        }

        static float2 FindNearestBruteForce(NativeList<float2> list, float2 target)
        {
            float minDistance = float.MaxValue;
            float2 minPoint = float2.zero;
            foreach (var point in list)
            {
                var distance = math.distancesq(target, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minPoint = point;
                }
            }
            return minPoint;
        }
    }
}
