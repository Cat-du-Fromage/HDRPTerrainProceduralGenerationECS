using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Stopwatch = System.Diagnostics.Stopwatch;
using Random = Unity.Mathematics.Random;
using ProjectDawn.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeLinkedListBenchmarkTests
    {
        [Test]
        public unsafe void UnsafeLinkedListBenchmarkTests_Int_Add()
        {
            int count = 1000000;
            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Restart();
            var linkedList = new UnsafeLinkedList<int>(1, Allocator.Temp);
            for (int i = 0; i < count; ++i)
                linkedList.Add(i);
            stopWatch.Stop();

            float linkedListTime = stopWatch.ElapsedMilliseconds;

            stopWatch.Restart();
            var list = new UnsafeList<int>(1, Allocator.Temp);
            for (int i = 0; i < count; ++i)
                list.Add(i);
            stopWatch.Stop();
            float listTime = stopWatch.ElapsedMilliseconds;

            linkedList.Dispose();
            list.Dispose();

            Debug.Log($"UnsafeLinkedListBenchmarkTests_Int_Add\n UnsafeLinkedList:{linkedListTime}ms\n UnsafeList:{listTime}ms");
        }

        [Test]
        public unsafe void UnsafeLinkedListBenchmarkTests_Int_RandomRemove()
        {
            int count = 200000;
            Stopwatch stopWatch = new Stopwatch();
            Random rnd;

            rnd = new Random(1);
            stopWatch.Restart();
            var linkedList = new UnsafeLinkedList<int>(1, Allocator.Temp);
            for (int i = 0; i < count; ++i)
                linkedList.Add(rnd.NextInt(0, count));

            var end = linkedList.End;
            for (var itr = linkedList.Begin; itr != end; )
            {
                var next = linkedList.Next(itr);
                if (linkedList[itr] > count / 2)
                {
                    linkedList.RemoveAt(itr);
                }
                itr = next;
            }
            stopWatch.Stop();

            float linkedListTime = stopWatch.ElapsedMilliseconds;

            rnd = new Random(1);
            stopWatch.Restart();
            var list = new UnsafeList<int>(1, Allocator.Temp);
            for (int i = 0; i < count; ++i)
                list.Add(rnd.NextInt(0, count));
            for (int i = 0; i < list.Length; ++i)
            {
                if (list[i] > count / 2)
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
            stopWatch.Stop();

            float listTime = stopWatch.ElapsedMilliseconds;

            int index = 0;
            foreach (var item in linkedList)
            {
                Assert.AreEqual(item, list[index++]);
            }

            linkedList.Dispose();
            list.Dispose();

            Debug.Log($"UnsafeLinkedListBenchmarkTests_Int_RandomRemove\n UnsafeLinkedList:{linkedListTime}ms\n UnsafeList:{listTime}ms");
            Assert.IsTrue(linkedListTime <= listTime);
        }
    }
}
