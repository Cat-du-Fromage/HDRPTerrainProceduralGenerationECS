namespace ProjectDawn.Collections
{
    public interface IOverlap<T> where T : unmanaged
    {
        /// <summary>
        /// Returns true if overlaps.
        /// </summary>
        bool Overlap(T value);
    }
}