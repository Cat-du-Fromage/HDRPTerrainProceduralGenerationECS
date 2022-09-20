using NUnit.Framework;
using Unity.Collections;
using ProjectDawn.Collections.LowLevel.Unsafe;
using System;
using System.Collections.Generic;

namespace ProjectDawn.Collections.Tests
{
    internal class UnsafeLinkedListTests
    {
        [Test]
        public unsafe void UnsafeNativeLinkedList_Add()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            list.Add(0);
            list.Add(1);
            list.Add(2);

            var begin = list.Begin;
            Assert.AreEqual(list[begin], 0);
            Assert.AreEqual(list[list.Next(begin)], 1);
            Assert.AreEqual(list[list.Next(list.Next(begin))], 2);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_Insert()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            list.Add(0);
            list.Add(2);

            list.Insert(list.Begin, -1);
            list.Insert(list.End, 3);

            var begin = list.Begin;
            Assert.AreEqual(list[begin], -1);
            Assert.AreEqual(list[list.Next(begin)], 0);
            Assert.AreEqual(list[list.Next(list.Next(begin))], 2);
            Assert.AreEqual(list[list.Next(list.Next(list.Next(begin)))], 3);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_RemoveAt()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            list.Add(0);
            var handle = list.Add(1);
            list.Add(2);

            list.RemoveAt(handle);

            Assert.AreEqual(list[list.Begin], 0);
            Assert.AreEqual(list[list.Next(list.Begin)], 2);
            Assert.AreEqual(2, list.Length);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_Iterator()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            for (int i = 0; i < 10; ++i)
                list.Add(i);

            int index = 0;
            for (var itr = list.Begin; itr != list.End; itr = list.Next(itr))
            {
                Assert.AreEqual(list[itr], index++);
            }
            Assert.AreEqual(list.Length, index);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_Iterator_PersistAfterCapacityChange()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            var itr = list.Add(0);
            Assert.AreEqual(list.Capacity, 4);

            list.Add(1);
            list.Add(2);
            list.Add(3);
            Assert.AreNotEqual(list.Capacity, 4);

            Assert.AreEqual(list[itr], 0);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_Enumerator()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            for (int i = 0; i < 10; ++i)
                list.Add(i);

            int index = 0;
            foreach (var item in list)
            {
                Assert.AreEqual(item, index++);
            }
            Assert.AreEqual(list.Length, index);

            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_InValid_Iterator()
        {
            var list = new UnsafeLinkedList<int>(1, Allocator.Temp);

            list.Add(0);

            var itr = list.Begin;

            // Invalidate iterator
            list.RemoveAt(itr);
            
            // Using invalid iterator
            Assert.Throws<ArgumentException>(() =>
            {
                list.Insert(itr, 1);
            });

            // Using invalid iterator
            Assert.Throws<ArgumentException>(() =>
            {
                list[itr] = 1;
            });

            list.Dispose();
        }

        struct IntComparer : IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }


        [Test]
        public unsafe void UnsafeNativeLinkedList_Sort()
        {
            var linkedList = new UnsafeLinkedList<int>(1, Allocator.Temp);
            var list = new NativeList<int>(1, Allocator.Temp);

            var rnd = new Random(1);
            for (int i = 0; i < 1000; ++i)
            {
                int value = rnd.Next();
                linkedList.Add(value);
                list.Add(value);
            }

            list.Sort();

            var begin = linkedList.Begin;
            var end = linkedList.End;
            linkedList.Sort<IntComparer>(ref begin, ref end, default);

            var itr = linkedList.Begin;
            foreach (int value in list)
            {
                Assert.AreEqual(value, linkedList[itr]);
                itr = linkedList.Next(itr);
            }

            linkedList.Dispose();
            list.Dispose();
        }

        [Test]
        public unsafe void UnsafeNativeLinkedList_Sort_Range()
        {
            var linkedList = new UnsafeLinkedList<int>(1, Allocator.Temp);

            linkedList.Add(3);
            linkedList.Add(2);
            linkedList.Add(1);
            linkedList.Add(0);

            var begin = linkedList.Add(3);
            linkedList.Add(2);
            linkedList.Add(1);
            var last = linkedList.Add(0);

            linkedList.Add(3);
            linkedList.Add(2);
            linkedList.Add(1);
            linkedList.Add(0);

            var end = linkedList.Next(last);
            linkedList.Sort<IntComparer>(ref begin, ref end, default);

            int index = 0;

            Assert.AreEqual(3, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(2, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(1, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(0, GetElementAtIndex(linkedList, index++));

            Assert.AreEqual(0, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(1, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(2, GetElementAtIndex(linkedList, index++));
            Assert.AreEqual(3, GetElementAtIndex(linkedList, index++));

            Assert.AreEqual(0, linkedList[begin]);
            Assert.AreEqual(3, linkedList[end]);

            linkedList.Dispose();
        }

        static T GetElementAtIndex<T>(UnsafeLinkedList<T> list, int index) where T : unmanaged
        {
            var itr = list.Begin;
            for (int i = 0; i < index; ++i)
            {
                itr = list.Next(itr);
            }
            return list[itr];
        }
    }
}
