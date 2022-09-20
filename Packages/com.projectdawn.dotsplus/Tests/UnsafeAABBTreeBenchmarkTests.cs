using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Stopwatch = System.Diagnostics.Stopwatch;
using Random = Unity.Mathematics.Random;
using ProjectDawn.Collections.LowLevel.Unsafe;
using ProjectDawn.Geometry2D;
using Unity.Jobs;
using Unity.Burst;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeAABBTreeBenchmarkTests
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

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct AABRectangleJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Count;

            public void Execute()
            {
                var tree = new UnsafeAABBTree<AABRectangle>(1, Allocator.Temp);

                var volumes = new NativeArray<AABRectangle>(5000, Allocator.Temp);
                var rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var rectangle = new Rectangle(rnd.NextFloat2(-400, 400), rnd.NextFloat2(1, 2));
                    volumes[i] = new AABRectangle(rectangle);
                }
                
                tree.Build(volumes);
                volumes.Dispose();

                var nodesToVitis = new UnsafeStack<UnsafeAABBTree<AABRectangle>.Handle>(1, Allocator.Temp);
                rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var line = new Line(rnd.NextFloat2(-400, 400), rnd.NextFloat2(-400, 400));

                    nodesToVitis.Push(tree.Root);
                    while (nodesToVitis.TryPop(out var node))
                    {
                        if (!node.Valid)
                            continue;

                        if (!tree[node].Rectangle.Overlap(line))
                            continue;

                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));

                        if (tree.IsLeaf(node))
                        {
                            (*Count)++;
                        }
                    }
                }
                nodesToVitis.Dispose();

                tree.Dispose();
                
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct RectangleBruteForceJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Count;

            public void Execute()
            {
                var volumes = new NativeArray<AABRectangle>(5000, Allocator.Temp);
                var rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var rectangle = new Rectangle(rnd.NextFloat2(-400, 400), rnd.NextFloat2(1, 2));
                    volumes[i] = new AABRectangle(rectangle);
                }

                rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var line = new Line(rnd.NextFloat2(-400, 400), rnd.NextFloat2(-400, 400));

                    for (int j = 0; j < 5000; ++j)
                    {
                        var rectangle = volumes[j].Rectangle;
                        if (!rectangle.Overlap(line))
                            continue;

                        (*Count)++;
                    }
                }
                volumes.Dispose();
            }
        }

        [Test]
        public unsafe void UnsafeAABBTreeBenchmarkTests_Rectangle_Overlap()
        {
            var tree = new UnsafeAABBTree<AABRectangle>(1, Allocator.TempJob);

            var rectangles = new NativeList<Rectangle>(Allocator.Temp);

            var rnd = new Random(1);
            for (int i = 0; i < 1000; ++i)
            {
                var rectangle = new Rectangle(rnd.NextFloat2(-40, 40), rnd.NextFloat2(1, 2));
                rectangles.Add(rectangle);
                tree.Add(new AABRectangle(rectangle));
            }

            var sw = new Stopwatch();
            rnd = new Random(1);
            sw.Restart();
            int treeCount = 0;
            var nodesToVitis = new UnsafeStack<UnsafeAABBTree<AABRectangle>.Handle>(1, Allocator.Temp);
            for (int i = 0; i < 10000; ++i)
            {
                var line = new Line(rnd.NextFloat2(-40, 40), rnd.NextFloat2(-40, 40));

                nodesToVitis.Push(tree.Root);
                while (nodesToVitis.TryPop(out var node))
                {
                    if (!node.Valid)
                        continue;

                    if (!tree[node].Rectangle.Overlap(line))
                        continue;

                    nodesToVitis.Push(tree.Left(node));
                    nodesToVitis.Push(tree.Right(node));

                    if (tree.IsLeaf(node))
                    {
                        treeCount++;
                    }
                }
            }
            nodesToVitis.Dispose();
            sw.Stop();
            float treeTime = sw.ElapsedMilliseconds;

            rnd = new Random(1);
            sw.Restart();
            int bruteForceCount = 0;
            for (int i = 0; i < 10000; ++i)
            {
                var line = new Line(rnd.NextFloat2(-40, 40), rnd.NextFloat2(-40, 40));
                foreach (var rectangle in rectangles)
                {
                    if (rectangle.Overlap(line))
                    {
                        bruteForceCount++;
                    }
                }
            }
            nodesToVitis.Dispose();
            sw.Stop();
            float bruteForceTime = sw.ElapsedMilliseconds;

            Assert.AreEqual(bruteForceCount, treeCount);

            Debug.Log($"UnsafeAABBTreeBenchmarkTests_Rectangle_Overlap {treeCount} Tree:{treeTime}ms BruteForce:{bruteForceTime}ms");

            tree.Dispose();
            rectangles.Dispose();
        }

        struct AABCircle : ISurfaceArea<AABCircle>, IUnion<AABCircle>
        {
            public Circle Shape;

            public AABCircle(Circle circle)
            {
                Shape = circle;
            }
            public float SurfaceArea() => Shape.Perimeter;
            public AABCircle Union(AABCircle value) => new AABCircle(Circle.Union(Shape, value.Shape));
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct AABCircleJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Count;

            public void Execute()
            {
                var tree = new UnsafeAABBTree<AABCircle>(1, Allocator.Temp);

                var volumes = new NativeArray<AABCircle>(5000, Allocator.Temp);
                var rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var rectangle = new Rectangle(rnd.NextFloat2(-400, 400), rnd.NextFloat2(1, 2));
                    var circle = rectangle.CircumscribedCircle();
                    volumes[i] = new AABCircle(circle);
                }

                tree.Build(volumes);
                volumes.Dispose();

                var nodesToVitis = new UnsafeStack<UnsafeAABBTree<AABCircle>.Handle>(1, Allocator.Temp);
                rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var line = new Line(rnd.NextFloat2(-400, 400), rnd.NextFloat2(-400, 400));

                    nodesToVitis.Push(tree.Root);
                    while (nodesToVitis.TryPop(out var node))
                    {
                        if (!node.Valid)
                            continue;

                        if (!tree[node].Shape.Overlap(line))
                            continue;

                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));

                        if (tree.IsLeaf(node))
                        {
                            (*Count)++;
                        }
                    }
                }
                nodesToVitis.Dispose();

                tree.Dispose();

            }
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct CircleBruteForceJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public int* Count;

            public void Execute()
            {
                var volumes = new NativeArray<AABCircle>(5000, Allocator.Temp);
                var rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var rectangle = new Rectangle(rnd.NextFloat2(-400, 400), rnd.NextFloat2(1, 2));
                    var circle = rectangle.CircumscribedCircle();
                    volumes[i] = new AABCircle(circle);
                }

                rnd = new Random(1);
                for (int i = 0; i < 5000; ++i)
                {
                    var line = new Line(rnd.NextFloat2(-400, 400), rnd.NextFloat2(-400, 400));

                    for (int j = 0; j < 5000; ++j)
                    {
                        var rectangle = volumes[j].Shape;
                        if (!rectangle.Overlap(line))
                            continue;

                        (*Count)++;
                    }
                }
                volumes.Dispose();
            }
        }

        [Test]
        public unsafe void UnsafeAABBTreeBenchmarkTests_Rectangle_Overlap_Jobified()
        {
            var sw = new Stopwatch();
            sw.Restart();
            int jobifiedTreeCount = 0;
            new AABRectangleJob
            {
                Count = &jobifiedTreeCount
            }.Schedule().Complete();
            sw.Stop();
            float treeTime = sw.ElapsedMilliseconds;

            sw.Restart();
            int bruteForceCount = 0;
            new RectangleBruteForceJob
            {
                Count = &bruteForceCount
            }.Schedule().Complete();
            sw.Stop();
            float bruteForceTime = sw.ElapsedMilliseconds;

            Debug.Log($"UnsafeAABBTreeBenchmarkTests_Rectangle_Overlap_Jobified Tree({jobifiedTreeCount}):{treeTime}ms BruteForce({bruteForceCount}):{bruteForceTime}ms");
        }

        [Test]
        public unsafe void UnsafeAABBTreeBenchmarkTests_Circle_Overlap_Jobified()
        {
            var sw = new Stopwatch();
            sw.Restart();
            int jobifiedTreeCount = 0;
            new AABCircleJob
            {
                Count = &jobifiedTreeCount
            }.Schedule().Complete();
            sw.Stop();
            float treeTime = sw.ElapsedMilliseconds;

            sw.Restart();
            int bruteForceCount = 0;
            new CircleBruteForceJob
            {
                Count = &bruteForceCount
            }.Schedule().Complete();
            sw.Stop();
            float bruteForceTime = sw.ElapsedMilliseconds;

            Debug.Log($"UnsafeAABBTreeBenchmarkTests_Circle_Overlap_Jobified Tree({jobifiedTreeCount}):{treeTime}ms BruteForce({bruteForceCount}):{bruteForceTime}ms");
        }
    }
}
