using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace ProjectDawn.Collections.Tests
{
    internal class NativePriorityQueueTests
    {
        struct AscendingOrder : IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

        struct DescendingOrder : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }

        [Test]
        public unsafe void NativePriorityQueue_IsEmpty()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            Assert.IsTrue(queue.IsEmpty);

            queue.Enqueue(10);

            Assert.IsFalse(queue.IsEmpty);

            queue.Dequeue();

            Assert.IsTrue(queue.IsEmpty);

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_Length()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            Assert.AreEqual(queue.Length, 0);

            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Assert.AreEqual(queue.Length, 3);

            queue.Dequeue();

            Assert.AreEqual(queue.Length, 2);

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_AscendingOrder()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            queue.Enqueue(10);
            queue.Enqueue(15);
            queue.Enqueue(5);
            queue.Enqueue(1);
            queue.Enqueue(30);

            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(5, queue.Dequeue());
            Assert.AreEqual(10, queue.Dequeue());
            Assert.AreEqual(15, queue.Dequeue());
            Assert.AreEqual(30, queue.Dequeue());

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_DescendingOrder()
        {
            var queue = new NativePriorityQueue<int, DescendingOrder>(Allocator.Temp, new DescendingOrder());

            queue.Enqueue(10);
            queue.Enqueue(15);
            queue.Enqueue(5);
            queue.Enqueue(1);
            queue.Enqueue(30);

            Assert.AreEqual(30, queue.Dequeue());
            Assert.AreEqual(15, queue.Dequeue());
            Assert.AreEqual(10, queue.Dequeue());
            Assert.AreEqual(5, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_Peek()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            queue.Enqueue(10);
            queue.Enqueue(5);

            Assert.AreEqual(5, queue.Peek());
            Assert.AreEqual(queue.Length, 2);

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_Clear()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            queue.Enqueue(10);
            queue.Enqueue(15);
            queue.Enqueue(5);
            queue.Enqueue(1);
            queue.Enqueue(30);

            queue.Clear();

            queue.Enqueue(50);
            queue.Enqueue(40);

            Assert.AreEqual(40, queue.Dequeue());
            Assert.AreEqual(50, queue.Dequeue());

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_ToArray()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            queue.Enqueue(10);
            queue.Enqueue(15);
            queue.Enqueue(5);
            queue.Enqueue(1);
            queue.Enqueue(30);

            var array = queue.ToArray(Allocator.Temp);

            int index = 0;
            Assert.AreEqual(array[index++], 1);
            Assert.AreEqual(array[index++], 5);
            Assert.AreEqual(array[index++], 10);
            Assert.AreEqual(array[index++], 15);
            Assert.AreEqual(array[index++], 30);

            array.Dispose();

            queue.Dispose();
        }

        [Test]
        public unsafe void NativePriorityQueue_Enumerator()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.Temp, new AscendingOrder());

            for (int i = 0; i < 10; ++i)
                queue.Enqueue(i);

            int index = 0;
            foreach (var item in queue)
            {
                Assert.AreEqual(item, index++);
            }
            Assert.AreEqual(index, queue.Length);

            queue.Dispose();
        }

        struct PriorityQueueJob : IJob
        {
            [ReadOnly]
            public NativePriorityQueue<int, AscendingOrder> Queue;

            public void Execute()
            {
                Assert.AreEqual(Queue.Peek(), 0);
            }
        }

        [Test]
        public unsafe void NativePriorityQueue_Jobified_Read()
        {
            var queue = new NativePriorityQueue<int, AscendingOrder>(Allocator.TempJob, new AscendingOrder());

            queue.Enqueue(0);
            queue.Enqueue(1);
            queue.Enqueue(2);

            var job = new PriorityQueueJob { Queue = queue };

            var handle = job.Schedule();

            // Iterator is readonly so no race condition
            Assert.AreEqual(0, queue.Peek());

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                Assert.AreEqual(0, queue.Dequeue());
            });

            handle.Complete();

            queue.Dispose();
        }
    }
}
