using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System;

namespace ProjectDawn.Collections.Tests
{
    internal class NativeLinkedListTests
    {
        struct ReadJob : IJob
        {
            [ReadOnly]
            public NativeLinkedList<int> List;

            public void Execute()
            {
                Assert.AreEqual(List.BeginRO.Value, 0);
            }
        }

        [Test]
        public unsafe void NativeLinkedListTests_Jobified_Read()
        {
            var list = new NativeLinkedList<int>(Allocator.TempJob);

            list.Add(0);
            list.Add(1);
            list.Add(2);

            var job = new ReadJob { List = list };

            var handle = job.Schedule();

            // Iterator is readonly so no race condition
            Assert.AreEqual(0, list.BeginRO.Value);

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                var itr = list.Begin;
                itr.Value = 1;
            });

            handle.Complete();

            list.Dispose();
        }

        [Test]
        public void NativeLinkedListTests_Middle_Even()
        {
            var list = new NativeLinkedList<int>(Allocator.Temp);

            list.Add(0);
            var middle = list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.AreEqual(middle.Value, list.Middle(list.Begin, list.End).Value);

            list.Dispose();
        }

        [Test]
        public void NativeLinkedListTests_Middle_Odd()
        {
            var list = new NativeLinkedList<int>(Allocator.Temp);

            list.Add(0);
            list.Add(1);
            var middle = list.Add(2);
            list.Add(3);
            list.Add(4);

            Assert.AreEqual(middle.Value, list.Middle(list.Begin, list.End).Value);

            list.Dispose();
        }

        [Test]
        public void NativeLinkedListTests_Middle_Count()
        {
            var list = new NativeLinkedList<int>(Allocator.Temp);

            list.Add(0);
            list.Add(1);
            var itr = list.Add(2);
            list.Add(3);
            list.Add(4);

            Assert.AreEqual(3, list.Count(itr, list.End));

            list.Dispose();
        }
    }
}
