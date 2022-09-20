using NUnit.Framework;
using Unity.Mathematics;
using Unity.Collections;
using System.Diagnostics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Geometry2D.Tests
{
    internal class VoronoiBenchmarkTests
    {
        struct DummyVoronoiDiagram : IVoronoiOutput
        {
            public void ProcessSite(double2 point, int siteIndex)
            {
            }

            public void ProcessEdge(double a, double b, double c, int leftVertexIndex, int rightVertexIndex, int leftSiteIndex, int rightSiteIndex)
            {
            }

            public int ProcessVertex(double2 point)
            {
                return -1;
            }

            public void Build()
            {
            }

        }

        [Test]
        public void VoronoiBenchmarkTests_Full()
        {
            var voronoi = new VoronoiBuilder(1, Allocator.Temp);

            var sw = new Stopwatch();
            sw.Start();
            var rnd = new Random(1);
            for (int i = 0; i < 1000; ++i)
            {
                voronoi.Add(rnd.NextDouble2(-40, 40));
            }
            var diagram = new DummyVoronoiDiagram();
            voronoi.Construct(ref diagram);
            sw.Stop();

            UnityEngine.Debug.Log($"VoronoiBenchmarkTests_Full {sw.ElapsedMilliseconds}ms");

            voronoi.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        unsafe struct Job : IJob
        {
            public VoronoiBuilder Builder;
            public DummyVoronoiDiagram Diagram;

            public void Execute()
            {
                var rnd = new Random(1);
                for (int i = 0; i < 10000; ++i)
                {
                    Builder.Add(rnd.NextDouble2(-40, 40));
                }
                Builder.Construct(ref Diagram);
            }
        }


        [Test]
        public unsafe void VoronoiBenchmarkTests_Full_Jobified()
        {
            var voronoi = new VoronoiBuilder(1000, Allocator.TempJob);

            var job = new Job
            {
                Builder = voronoi,
                Diagram = new DummyVoronoiDiagram(),
            };

            var sw = new Stopwatch();
            sw.Start();
            job.Schedule().Complete();
            sw.Stop();

            UnityEngine.Debug.Log($"VoronoiBenchmarkTests_Full_Jobified {sw.ElapsedMilliseconds}ms");

            voronoi.Dispose();
        }
    }
}
