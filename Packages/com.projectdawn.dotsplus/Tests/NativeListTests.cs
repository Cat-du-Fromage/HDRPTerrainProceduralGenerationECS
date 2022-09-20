using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System;

namespace ProjectDawn.Collections.Tests
{
    internal class NativeListTests
    {
        [Test]
        public void NativeListTests_Int_Insert()
        {
            var list = new NativeList<int>(Allocator.Temp);

            list.Insert(0, 0);
            list.Insert(1, 1);
            list.Insert(3, 2);
            list.Insert(2, 2);
            list.Insert(-1, 0);

            Assert.AreEqual(-1, list[0]);
            Assert.AreEqual(0, list[1]);
            Assert.AreEqual(1, list[2]);
            Assert.AreEqual(2, list[3]);
            Assert.AreEqual(3, list[4]);

            list.Dispose();
        }

        [Test]
        public void NativeListTests_Int_Insert_IndexOutOfRange()
        {
            var list = new NativeList<int>(Allocator.Temp);

            list.Insert(0, 0);
            list.Insert(1, 1);
            list.Insert(3, 2);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                list.Insert(4, 4);
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                list.Insert(-1, -1);
            });

            list.Dispose();
        }

        struct ReadJob : IJob
        {
            [ReadOnly]
            public NativeList<int> List;

            public void Execute()
            {
                Assert.AreEqual(0, List[0]);
            }
        }

        [Test]
        public unsafe void NativeListTests_Int_Insert_Jobified_Read()
        {
            var list = new NativeList<int>(Allocator.TempJob);

            list.Add(0);
            list.Add(1);
            list.Add(2);

            var job = new ReadJob { List = list };

            var handle = job.Schedule();

            // Iterator is readonly so no race condition
            Assert.AreEqual(0, list[0]);

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                list[0] = 1;
            });

            handle.Complete();

            list.Dispose();
        }
    }
}
