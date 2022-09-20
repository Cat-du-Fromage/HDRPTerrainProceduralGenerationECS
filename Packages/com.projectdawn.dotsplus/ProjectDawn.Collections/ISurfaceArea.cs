namespace ProjectDawn.Collections
{
    public interface ISurfaceArea<T> where T : unmanaged
    {
        /// <summary>
        /// Returns surface area of the shape.
        /// </summary>
        float SurfaceArea();
    }
}