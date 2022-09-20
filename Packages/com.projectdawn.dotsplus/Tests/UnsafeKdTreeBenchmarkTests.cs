using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Stopwatch = System.Diagnostics.Stopwatch;
using Random = Unity.Mathematics.Random;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeKdTreeBenchmarkTests
    {
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
        public unsafe void KdTreeBenchmarkTests_Float2_FindNearest()
        {
            var tree = new UnsafeKdTree<float2, TreeComparer>(1, Allocator.TempJob, new TreeComparer());

            var rnd = new Random(1);

            var list = new NativeList<float2>(Allocator.Temp);
            for (int i = 0; i < 10000; i++)
            {
                var point = rnd.NextFloat2(-20, 20);
                list.Add(point);
                tree.Add(point);
            }

            var stopWatch = new Stopwatch();

            int treeSearchCount = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 2000; i++)
            {
                var target = rnd.NextFloat2(-20, 20);
                tree.FindNearest(target, out var count);
                treeSearchCount += count;
            }
            stopWatch.Stop();
            Debug.Log($"TreeDepth:{tree.GetDepth()} {treeSearchCount}");
            float treeTime = stopWatch.ElapsedMilliseconds;

            int balancedTreeSearchCount = 0;
            rnd = new Random(1);
            tree.Build(list.AsArray());
            stopWatch.Restart();
            for (int i = 0; i < 2000; i++)
            {
                var target = rnd.NextFloat2(-20, 20);
                tree.FindNearest(target, out var count);
                balancedTreeSearchCount += count;
            }
            stopWatch.Stop();
            Debug.Log($"BalancedTreeDepth:{tree.GetDepth()} {balancedTreeSearchCount}");
            float treeBalancedTime = stopWatch.ElapsedMilliseconds;

            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 2000; i++)
            {
                var target = rnd.NextFloat2(-20, 20);
                FindNearestBruteForce(list, target);
            }
            stopWatch.Stop();
            float bruteForceTime = stopWatch.ElapsedMilliseconds;

            list.Dispose();
            tree.Dispose();

            Debug.Log($"KdTreeBenchmarkTests_Float2_FindNearest\n Tree:{treeTime}ms\n TreeBalanced:{treeBalancedTime}ms\n BruteForce:{bruteForceTime}ms");
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
