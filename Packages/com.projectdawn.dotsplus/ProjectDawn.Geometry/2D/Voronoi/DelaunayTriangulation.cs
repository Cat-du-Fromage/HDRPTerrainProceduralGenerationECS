using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections;
using ProjectDawn.Collections;
using static Unity.Mathematics.math;
using static ProjectDawn.Mathematics.math2;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Delaunay trinagulation composed of triangles and points.
    /// Use <see cref="VoronoiBuilder.Construct{T}(ref T)"/> to construct this delaunay triangulation.
    /// </summary>
    public unsafe struct DelaunayTriangulation : IVoronoiOutput, IDisposable
    {
        /// <summary>
        /// List of points.
        /// </summary>
        public NativeList<float2> Points;
        /// <summary>
        /// List of triangles indices.
        /// </summary>
        public NativeList<int3> Indices;

        NativeList<int2> m_Edges;
        NativeParallelHashSet<int2> m_EdgeCheck;

        public DelaunayTriangulation(int numSites, AllocatorManager.AllocatorHandle allocator)
        {
            Points = new NativeList<float2>(numSites, allocator);
            Points.ResizeUninitialized(numSites);
            Indices = new NativeList<int3>(allocator);

            m_Edges = new NativeList<int2>(numSites << 1, allocator);
            m_EdgeCheck = new NativeParallelHashSet<int2>(numSites << 1, allocator);
        }

        /// <inheritdoc />
        public void ProcessSite(double2 point, int siteIndex)
        {
            CollectionChecks.CheckIndexInRange(siteIndex, Points.Length);
            Points[siteIndex] = new float2((float)point.x, (float)point.y);
        }

        /// <inheritdoc />
        public int ProcessVertex(double2 point) => -1;

        /// <inheritdoc />
        public void ProcessEdge(double a, double b, double c, int leftVertexIndex, int rightVertexIndex, int leftSiteIndex, int rightSiteIndex)
        {
            int2 indices = sort(new int2(leftSiteIndex, rightSiteIndex));
            m_Edges.Add(indices);
            m_EdgeCheck.Add(indices);
        }

        /// <inheritdoc />
        public void Build()
        {
            for (int edgeIndexA = 0; edgeIndexA < m_Edges.Length; ++edgeIndexA)
            {
                int2 indicesA = m_Edges[edgeIndexA];
                for (int edgeIndexB = edgeIndexA + 1; edgeIndexB < m_Edges.Length; ++edgeIndexB)
                {
                    int2 indicesB = m_Edges[edgeIndexB];

                    // Check if both edges originate from same vertex
                    if (indicesA.x != indicesB.x)
                        continue;

                    // Check if the edges end points are connected
                    int2 indicesC = sort(new int2(indicesA.y, indicesB.y));
                    if (!m_EdgeCheck.Contains(indicesC))
                        continue;

                    // Here we gather triangle indices
                    int3 indices = new int3(indicesA.x, indicesC.x, indicesC.y);

                    float2 a = Points[indices.x];
                    float2 b = Points[indices.y];
                    float2 c = Points[indices.z];

                    // Check if ther is any point inside triangle, if yes it means this triangle contains more triangle and we should discard it
                    bool check = false;
                    for (int pointIndex = 0; pointIndex < Points.Length; ++pointIndex)
                    {
                        float3 bary = barycentric(a, b, c, Points[pointIndex]);
                        if (all(EPSILON < bary & bary < 1 - EPSILON))
                        {
                            check = true;
                            break;
                        }
                    }
                    if (check)
                        continue;

                    // Set triangle to clockwise
                    if (iscclockwise(a, b, c))
                        swap(ref indices.x, ref indices.z);

                    Indices.Add(indices);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Points.Dispose();
            Indices.Dispose();
            m_Edges.Dispose();
            m_EdgeCheck.Dispose();
        }
    }
}
