namespace ProjectDawn.Collections
{
    public interface IUnion<T> where T : unmanaged
    {
        /// <summary>
        /// Returns combined volume.
        /// </summary>
        T Union(T value);
    }
}