using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace ProjectDawn.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An managed, resizable stack, without any thread safety check features.
    /// Limited version of <see cref="NativeList"/> that allows only adding to list back or removing.
    /// </summary>
    /// <typeparam name="T">Source type of elements</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeStack<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Ptr;

        public int length;
        public int capacity;
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        /// Whether the stack is empty.
        /// </summary>
        /// <value>True if the stack is empty or the stack has not been constructed.</value>
        public bool IsEmpty => length == 0;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length => length;

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        public int Capacity
        {
            get => capacity;
            set { capacity = value; }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Constructs stack as view into memory.
        /// </summary>
        public unsafe UnsafeStack(T* ptr, int length)
        {
            Ptr = ptr;
            this.length = length;
            capacity = 0;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Constructs a new stack using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The stack initially has a capacity of one. To avoid reallocating memory for the stack, specify
        /// sufficient capacity up front.</remarks>
        public unsafe UnsafeStack(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;
            this.ListData() = new UnsafeList<T>(initialCapacity, allocator, options);
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public static UnsafeStack<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeStack<T>* data = AllocatorManager.Allocate<UnsafeStack<T>>(allocator);

            data->Ptr = null;
            data->length = 0;
            data->capacity = 0;
            data->Allocator = AllocatorManager.None;
            data->ListData() = new UnsafeList<T>(initialCapacity, allocator, options);

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeStack<T>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
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
            return this.ListData().Dispose(inputDeps);
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Stack Capacity remains unchanged.</remarks>
        public void Clear()
        {
            this.ListData().Clear();
        }

        /// <summary>
        /// Set the number of items that can fit in the stack.
        /// </summary>
        /// <param name="capacity">The number of items that the stack can hold before it resizes its internal storage.</param>
        public void SetCapacity(int capacity)
        {
            this.ListData().SetCapacity(capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        public void TrimExcess()
        {
            this.ListData().TrimExcess();
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
            this.ListData().AddNoResize(value);
        }

        /// <summary>
        /// Pushs the elements to this container.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the container.</param>
        /// <remarks>
        /// If the stack has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void PushRangeNoResize(void* ptr, int length)
        {
            this.ListData().AddRangeNoResize(ptr, length);
        }

        /// <summary>
        /// Pushs an element to the stack.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the stack.</param>
        public void Push(T value)
        {
            this.ListData().Add(value);
        }

        /// <summary>
        /// Removes the element from the end of the stack.
        /// </summary>
        public T Pop()
        {
            CheckStackEmpty();
            length--;
            return Ptr[length];
        }

        /// <summary>
        /// Removes the element from the end of the stack.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="value">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryPop(out T value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            length--;
            value = Ptr[length];
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckStackEmpty()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Trying to pop from an empty stack");
        }
    }

    internal unsafe static class UnsafeStackExtensions
    {
        public static ref UnsafeList<T> ListData<T>(ref this UnsafeStack<T> from) where T : unmanaged => ref UnsafeUtility.As<UnsafeStack<T>, UnsafeList<T>>(ref from);
    }
}