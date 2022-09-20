using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System;

namespace ProjectDawn.Collections.Tests
{
    internal class NativeKdTreeTests
    {
        struct TreeComparer : IKdTreeComparer<int>
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

        struct ReadJob : IJob
        {
            [ReadOnly]
            public NativeKdTree<int, TreeComparer> Tree;

            public void Execute()
            {
                Assert.AreEqual(Tree.Root.Value, 0);
            }
        }

        [Test]
        public unsafe void NativeKdTreeTests_Jobified_Read()
        {
            var tree = new NativeKdTree<int, TreeComparer>(Allocator.TempJob);

            tree.Add(0);
            tree.Add(1);
            tree.Add(2);

            var job = new ReadJob { Tree = tree };

            var handle = job.Schedule();

            // Iterator is readonly so no race condition
            Assert.AreEqual(0, tree.Root.Value);

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                tree.Add(3);
            });

            handle.Complete();

            tree.Dispose();
        }
    }
}
