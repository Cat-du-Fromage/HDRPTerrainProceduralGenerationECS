using Unity.Mathematics;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Interface of the voronoi output used by <see cref="VoronoiBuilder.Construct{T}(ref T)"/> to construct voronoi shape.
    /// </summary>
    public interface IVoronoiOutput
    {
        /// <summary>
        /// Callback after <see cref="VoronoiBuilder"/> processes the site.
        /// </summary>
        /// <param name="point">The point of site.</param>
        /// <param name="index">Index of site.</param>
        void ProcessSite(double2 point, int index);
        /// <summary>
        /// Callback after <see cref="VoronoiBuilder"/> processes the vertex.
        /// </summary>
        /// <param name="point">The point of vertex.</param>
        /// <returns>The index of the vertex.</returns>
        int ProcessVertex(double2 point);
        /// <summary>
        /// Callback after <see cref="VoronoiBuilder"/> processes the edge.
        /// </summary>
        /// <param name="a">Standard line a coefficient.</param>
        /// <param name="b">Standard line b coefficient.</param>
        /// <param name="c">Standard line c coefficient.</param>
        /// <param name="leftVertexIndex"></param>
        /// <param name="rightVertexIndex"></param>
        /// <param name="leftSiteIndex"></param>
        /// <param name="rightSiteIndex"></param>
        void ProcessEdge(double a, double b, double c, int leftVertexIndex, int rightVertexIndex, int leftSiteIndex, int rightSiteIndex);
        /// <summary>
        /// Callback after <see cref="VoronoiBuilder"/> finished building.
        /// </summary>
        void Build();
    }
}
