using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using ProjectDawn.Collections.LowLevel.Unsafe;
using System.Threading;

namespace ProjectDawn.Collections
{
    /// <summary>
    /// An managed, resizable stack.
    /// Limited version of <see cref="NativeList"/> that only operates with the last element at the time.
    /// </summary>
    /// <typeparam name="T">Source type of elements</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeStack<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeStack<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Whether the stack is empty.
        /// </summary>
        /// <value>True if the stack is empty or the stack has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Data->IsEmpty;
            }
        }

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Data->Length;
            }
        }

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Data->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                CollectionChecks.CheckCapacityInRange(value, m_Data->Length);
                m_Data->Capacity = value;
            }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_Data != null;

        /// <summary>
        /// Initializes and returns an instance of NativeLinkedList.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public NativeStack(Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this(1, allocator, options) { }

        /// <summary>
        /// Constructs a new stack using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The stack initially has a capacity of one. To avoid reallocating memory for the stack, specify
        /// sufficient capacity up front.</remarks>
        public NativeStack(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckCapacity(initialCapacity);
            CollectionChecks.CheckIsUnmanaged<T>();
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = AtomicSafetyHandle.Create();
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 2, allocator);
#endif
#endif

            m_Data = UnsafeStack<T>.Create(initialCapacity, allocator, options);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            AtomicSafetyHandle.Release(m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            UnsafeStack<T>.Destroy(m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_Data->Dispose(inputDeps);
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Stack Capacity remains unchanged.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Clear();
        }

        /// <summary>
        /// Pushs an element to the container.
        /// </summary>
        /// <param name="value">The value to be added at the end of the container.</param>
        /// <remarks>
        /// If the stack has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void PushNoResize(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_Data->PushNoResize(value);
        }

        /// <summary>
        /// Pushs an element to the stack.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the stack.</param>
        public void Push(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Data->Push(value);
        }

        /// <summary>
        /// Removes the element from the end of the stack.
        /// </summary>
        public T Pop()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->Pop();
        }

        /// <summary>
        /// Removes the element from the end of the stack.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="value">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryPop(out T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return m_Data->TryPop(out value);
        }

        public ParallelWriter AsParallelWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelWriter(m_Data->Ptr, m_Data, ref m_Safety);
#else
            return new ParallelWriter(m_Data->Ptr, m_Data);
#endif
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            readonly void* Ptr;

            [NativeDisableUnsafePtrRestriction]
            UnsafeStack<T>* Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;

            public unsafe ParallelWriter(void* ptr, UnsafeStack<T>* data, ref AtomicSafetyHandle safety)
            {
                Ptr = ptr;
                Data = data;
                m_Safety = safety;
            }
#else
            public unsafe ParallelWriter(void* ptr, UnsafeStack<T>* data)
            {
                Ptr = ptr;
                Data = data;
            }
#endif

            /// <summary>
            /// Push an element to the stack.
            /// </summary>
            /// <param name="value">The value to be added at the end of the stack.</param>
            /// <remarks>
            /// If the stack has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void PushNoResize(T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Increment(ref Data->length) - 1;
                CheckSufficientCapacity(Data->Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(Ptr, idx, value);
            }

            private void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Add(ref Data->length, length) - length;
                CheckSufficientCapacity(Data->Capacity, idx + length);

                void* dst = (byte*)Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            }

            /// <summary>
            /// </summary>
            /// <param name="ptr"></param>
            /// <param name="length"></param>
            public void PushRangeNoResize(void* ptr, int length)
            {
                CollectionChecks.CheckPositive(length);
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, length);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
        }
    }
}