using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System;

namespace ProjectDawn.Collections.Tests
{
    internal class NativeStackTests
    {
        struct WriteJob : IJob
        {
            public NativeStack<int> Stack;

            public void Execute()
            {
                Stack.Push(1);
            }
        }

        [Test]
        public unsafe void NativeStackTests_Int_Jobified_Write()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            stack.Push(0);

            var job = new WriteJob { Stack = stack };

            var handle = job.Schedule();

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            handle.Complete();

            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(0, stack.Pop());

            stack.Dispose();
        }

        struct ParallelWriteJob : IJobParallelFor
        {
            public NativeStack<int>.ParallelWriter Stack;

            public void Execute(int index)
            {
                Stack.PushNoResize(1);
            }
        }

        [Test]
        public unsafe void NativeStackTests_Int_Parallel_Write()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            stack.Push(0);

            var job = new ParallelWriteJob { Stack = stack.AsParallelWriter() };

            var handle = job.Schedule(2, 2);

            // Iterator is write so here we get exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            handle.Complete();

            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(0, stack.Pop());

            stack.Dispose();
        }
    }
}
