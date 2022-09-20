using NUnit.Framework;
using Unity.Mathematics;

namespace ProjectDawn.Mathematics.Tests
{
    internal class FastMathTests
    {
        [Test]
        public unsafe void FastMathTests_RSqrt()
        {
            var rnd = new Random(1);
            for (int i = 0; i < 1000000; i++)
            {
                float value = rnd.NextFloat(1, float.MaxValue);
                Assert.AreEqual(math.rsqrt(value), fastmath.rsqrt(value), 0.0001f);
            }
        }

        [Test]
        public unsafe void FastMathTests_Cos()
        {
            var rnd = new Random(1);
            for (int i = 0; i < 1000000; i++)
            {
                float value = rnd.NextFloat();
                Assert.AreEqual(math.cos(value), fastmath.cos(value), 0.0001f);
            }
        }

        [Test]
        public unsafe void FastMathTests_Sin()
        {
            var rnd = new Random(1);
            for (int i = 0; i < 1000000; i++)
            {
                float value = rnd.NextFloat();
                Assert.AreEqual(math.sin(value), fastmath.sin(value), 0.0001f);
            }
        }
    }
}
