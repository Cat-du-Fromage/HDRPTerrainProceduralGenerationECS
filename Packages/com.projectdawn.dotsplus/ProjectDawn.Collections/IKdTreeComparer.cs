namespace ProjectDawn.Collections
{
     /// <summary>
    /// Comparer used for sorting elements in k-d tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IKdTreeComparer<in T>
    {
        /// <summary>
        /// Compares tree elements at specific node height. Almost same as <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <param name="height">The node height.</param>
        /// <returns></returns>
        int Compare(T x, T y, int height);

        /// <summary>
        /// Returns distance between tree elements.
        /// </summary>
        float DistanceSq(T x, T y);

        /// <summary>
        /// Returns distance between tree elements at specific node height.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <param name="height">The node height.</param>
        float DistanceToSplitSq(T x, T y, int height);
    }
}