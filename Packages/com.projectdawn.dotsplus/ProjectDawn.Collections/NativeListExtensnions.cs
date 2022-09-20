using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Collections
{
    public unsafe static class NativeListExtensnios
    {
        /// <summary>
        /// Inserts new element at specific index.
        /// </summary>
        /// <param name="value">Element that will be inserted.</param>
        /// <param name="index">Index at wich element will be inserted.</param>
        /// <typeparam name="T">The type of the element.</typeparam>
        public static void Insert<T>(this NativeList<T> list, T value, int index) where T : unmanaged
        {
            CollectionChecks.CheckIndexInRange(index, list.Length + 1);

            var length = (list.Length - index) * sizeof(T);

            list.Length += 1;

            // Offset memory
            T* src = (T*)list.GetUnsafePtr() + index;
            if (length != 0)
            {
                T* dst = src + 1;
                UnsafeUtility.MemMove(dst, src, length);
            }

            // Add new element
            *src = value;
        }
    }
}