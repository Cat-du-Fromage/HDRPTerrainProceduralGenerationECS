using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using Stopwatch = System.Diagnostics.Stopwatch;
using Random = Unity.Mathematics.Random;

namespace ProjectDawn.Mathematics.Tests
{
    internal class FastMathBenchmaskTests
    {
        [Test]
        public unsafe void FastMathBenchmaskTests_RSqrt()
        {
            var rnd = new Random(1);
            var stopWatch = new Stopwatch();
            float sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += math.rsqrt(rnd.NextFloat(0, float.MaxValue) + 0.001f);
            }
            stopWatch.Stop();
            float invSqrtTime = stopWatch.ElapsedMilliseconds;
            float invSqrtSum = sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += fastmath.rsqrt(rnd.NextFloat(0, float.MaxValue) + 0.001f);
            }
            stopWatch.Stop();
            float fastInvSqrtTime = stopWatch.ElapsedMilliseconds;
            float fastInvSqrtSum = sum;

            Debug.Log($"InvSqrt {invSqrtTime} FastInvSqrt {fastInvSqrtTime}");
            Debug.Log($"InvSqrt {invSqrtSum} FastInvSqrt {fastInvSqrtSum}");

            Assert.Greater(invSqrtTime, fastInvSqrtTime);
        }

        [Test]
        public unsafe void FastMathBenchmaskTests_Cos()
        {
            var rnd = new Random(1);
            var stopWatch = new Stopwatch();
            float sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += math.cos(rnd.NextFloat());
            }
            stopWatch.Stop();
            float defaultTime = stopWatch.ElapsedMilliseconds;
            float defaultSum = sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += fastmath.cos(rnd.NextFloat());
            }
            stopWatch.Stop();
            float fastTime = stopWatch.ElapsedMilliseconds;
            float fastSum = sum;

            Debug.Log($"Cos {defaultTime} FastCos {fastTime}");
            Debug.Log($"Cos {defaultSum} FastCos {fastSum}");

            Assert.Greater(defaultTime, fastTime);
        }

        [Test]
        public unsafe void FastMathBenchmaskTests_Sin()
        {
            var rnd = new Random(1);
            var stopWatch = new Stopwatch();
            float sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += math.sin(rnd.NextFloat());
            }
            stopWatch.Stop();
            float defaultTime = stopWatch.ElapsedMilliseconds;
            float defaultSum = sum;

            sum = 0;
            rnd = new Random(1);
            stopWatch.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                sum += fastmath.sin(rnd.NextFloat());
            }
            stopWatch.Stop();
            float fastTime = stopWatch.ElapsedMilliseconds;
            float fastSum = sum;

            Debug.Log($"Sin {defaultTime} FastSin {fastTime}");
            Debug.Log($"Sin {defaultSum} FastSin {fastSum}");

            Assert.Greater(defaultTime, fastTime);
        }
    }
}
