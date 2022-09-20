using System;
using NUnit.Framework;
using Unity.Collections;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeStackTests
    {
        [Test]
        public unsafe void UnsafeStackTests_Int_PushPop()
        {
            var stack = new UnsafeStack<int>(1, Allocator.Temp);

            stack.Push(0);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(0, stack.Pop());

            stack.Dispose();
        }

        [Test]
        public unsafe void UnsafeStackTests_Int_TryPop_Empty()
        {
            var stack = new UnsafeStack<int>(1, Allocator.Temp);

            stack.Push(0);
            Assert.AreEqual(0, stack.Pop());
            Assert.IsFalse(stack.TryPop(out _));

            stack.Dispose();
        }

        [Test]
        public unsafe void UnsafeStackTests_Int_Pop_Empty()
        {
            var stack = new UnsafeStack<int>(1, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            stack.Push(0);
            stack.Pop();

            Assert.Throws<InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            stack.Dispose();
        }
    }
}
