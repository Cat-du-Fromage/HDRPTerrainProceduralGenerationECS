
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    public static class NativeLinkedListSort
    {
        /// <summary>
        /// A comparer that uses IComparable.CompareTo(). For primitive types, this is an ascending sort.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public struct DefaultComparer<T> : IComparer<T> where T : IComparable<T>
        {
            /// <summary>
            /// Compares two values.
            /// </summary>
            /// <param name="x">First value to compare.</param>
            /// <param name="y">Second value to compare.</param>
            /// <returns>A signed integer that denotes the relative values of `x` and `y`:
            /// 0 if they're equal, negative if `x &lt; y`, and positive if `x &gt; y`.</returns>
            public int Compare(T x, T y) => x.CompareTo(y);
        }

        /// <summary>
        /// Based on https://www.geeksforgeeks.org/merge-sort-for-linked-list/.
        /// </summary>
        public static NativeLinkedList<T>.Iterator Middle<T>(this NativeLinkedList<T> list, NativeLinkedList<T>.Iterator first, NativeLinkedList<T>.Iterator last) where T : unmanaged
        {
            var fastptr = first.Next;
            var slowptr = first;

            // Move fastptr by two and slow ptr by one
            // Finally slowptr will point to middle node
            while (fastptr != last)
            {
                fastptr = fastptr.Next;
                if (fastptr != last)
                {
                    slowptr = slowptr.Next;
                    fastptr = fastptr.Next;
                }
            }
            return slowptr;
        }

        /// <summary>
        /// Returns number of elements between iterators.
        /// </summary>
        public static int Count<T>(this NativeLinkedList<T> list, NativeLinkedList<T>.Iterator first, NativeLinkedList<T>.Iterator last) where T : unmanaged
        {
            int count = 0;
            for (var itr = first; itr != last; itr.MoveNext())
                count++;
            return count;
        }

        /// <summary>
        /// Sorts this list using a custom comparison.
        /// </summary>
        public static void Sort<T>(this NativeLinkedList<T> list) where T : unmanaged, IComparable<T>
        {
            var begin = list.Begin;
            var end = list.End;
            list.Sort(ref begin, ref end, new DefaultComparer<T>());
        }
    }
}