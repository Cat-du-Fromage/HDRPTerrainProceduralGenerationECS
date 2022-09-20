using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;
using static ProjectDawn.Mathematics.math2;
using ProjectDawn.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// The cell (a.k.a region) of voronoi diagram.
    /// </summary>
    public struct VoronoiCell
    {
        /// <summary>
        /// The site point that was used for generating this cell.
        /// </summary>
        public float2 Site;
        /// <summary>
        /// Iterator to first voronoi edge of this cell.
        /// </summary>
        public NativeLinkedList<VoronoiEdge>.Iterator Begin;
        /// <summary>
        /// Iterator to end voronoi edge of this cell. This iterator references dummy edge that represent the end.
        /// </summary>
        public NativeLinkedList<VoronoiEdge>.Iterator End;

        /// <summary>
        /// Returns true if any edge is intersecting the borders.
        /// </summary>
        internal bool HasBorderEdges;
    }

    /// <summary>
    /// The edge of voronoi diagram.
    /// </summary>
    public struct VoronoiEdge
    {
        /// <summary>
        /// Index of voronoi vertex from.
        /// </summary>
        public int FromVertexIndex;
        /// <summary>
        /// Index of voronoi vertex to.
        /// </summary>
        public int ToVertexIndex;
    }

    /// <summary>
    /// The type of voronoi vertex.
    /// </summary>
    public enum VoronoiVertexType
    {
        /// <summary>
        /// True voronoi vertex.
        /// </summary>
        Default,
        /// <summary>
        /// Bounds corner.
        /// </summary>
        Corner,
        /// <summary>
        /// Bounds and voronoi edge intersection vertex.
        /// </summary>
        BorderRight,
        /// <summary>
        /// Bounds and voronoi edge intersection vertex.
        /// </summary>
        BorderLeft,
        /// <summary>
        /// Bounds and voronoi edge intersection vertex.
        /// </summary>
        BorderUp,
        /// <summary>
        /// Bounds and voronoi edge intersection vertex.
        /// </summary>
        BorderBottom,
    }

    /// <summary>
    /// Vertex of voronoi diagram.
    /// </summary>
    public struct VoronoiVertex
    {
        public float2 Point;
        public VoronoiVertexType Type;
    }

    /// <summary>
    /// Voronoi diagram composed of cells, edges and vertices.
    /// Voronoi diagram is a partition of a plane into regions close to each of a given set of objects. In the simplest case, these objects are just finitely many points in the plane (called seeds, sites, or generators).
    /// For each seed there is a corresponding region, called a Voronoi cell, consisting of all points of the plane closer to that seed than to any other.
    /// Use <see cref="VoronoiBuilder.Construct{T}(ref T)"/> to construct this voronoi diagram.
    /// </summary>
    public unsafe struct VoronoiDiagram : IVoronoiOutput, IDisposable
    {
        /// <summary>
        /// List of cells.
        /// </summary>
        public NativeList<VoronoiCell> Cells;
        /// <summary>
        /// List of edges.
        /// </summary>
        public NativeLinkedList<VoronoiEdge> Edges;
        /// <summary>
        /// List of vertices.
        /// </summary>
        public NativeList<VoronoiVertex> Vertices;

        NativeList<int> EdgeIndices;
        Rectangle Bounds;
        int4 CornerVertexIndices;

        public VoronoiDiagram(Rectangle bounds, Allocator allocator)
        {
            Cells = new NativeList<VoronoiCell>(allocator);
            Vertices = new NativeList<VoronoiVertex>(allocator);
            Edges = new NativeLinkedList<VoronoiEdge>(allocator);
            EdgeIndices = new NativeList<int>(allocator);
            Bounds = bounds;
            CornerVertexIndices = 0;
        }

        /// <inheritdoc />
        public void ProcessSite(double2 point, int index)
        {
            if (Cells.Length <= index)
                Cells.Length = index + 1;

            var itr = Edges.Add(default);
            Cells[index] = new VoronoiCell
            {
                Site = (float2)point,
                Begin = itr,
                End = itr,
            };
        }

        /// <inheritdoc />
        public int ProcessVertex(double2 point)
        {
            return CreateVertex((float2)point);
        }

        /// <inheritdoc />
        public void ProcessEdge(double a, double b, double c, int leftVertexIndex, int rightVertexIndex, int leftSiteIndex, int rightSiteIndex)
        {
            if (ClipEdgeWithBounds(a, b, c,
                leftVertexIndex,
                rightVertexIndex,
                Bounds,
                out double2 leftVertex, out double2 rightVertex,
                out VoronoiVertexType leftVertexType, out VoronoiVertexType rightVertexType))
            {
                ref var leftCell = ref Cells.ElementAt(leftSiteIndex);
                ref var rightCell = ref Cells.ElementAt(rightSiteIndex);

                // Handle the case if edge is clipped by border
                if (leftVertexIndex == -1 || leftVertexType != VoronoiVertexType.Default)
                {
                    leftVertexIndex = CreateVertex((float2)leftVertex, leftVertexType);
                    leftCell.HasBorderEdges = true;
                    rightCell.HasBorderEdges = true;
                }

                // Handle the case if edge is clipped by border
                if (rightVertexIndex == -1 || rightVertexType != VoronoiVertexType.Default)
                {
                    rightVertexIndex = CreateVertex((float2)rightVertex, rightVertexType);
                    leftCell.HasBorderEdges = true;
                    rightCell.HasBorderEdges = true;
                }

                leftCell.Begin = CreateEdgeAt(leftCell.Begin, rightVertexIndex, leftVertexIndex);
                rightCell.Begin = CreateEdgeAt(rightCell.Begin, leftVertexIndex, rightVertexIndex);
            }
        }

        /// <inheritdoc />
        public void Build()
        {
            CreateCornerVertices();

            // If only one cell simply return the bounds
            if (Cells.Length == 1)
            {
                ref VoronoiCell cell = ref Cells.ElementAt(0);
                cell.Begin = CreateEdgeAt(cell.Begin, CornerVertexIndices.x, CornerVertexIndices.y);
                cell.Begin = CreateEdgeAt(cell.Begin, CornerVertexIndices.y, CornerVertexIndices.z);
                cell.Begin = CreateEdgeAt(cell.Begin, CornerVertexIndices.z, CornerVertexIndices.w);
                cell.Begin = CreateEdgeAt(cell.Begin, CornerVertexIndices.w, CornerVertexIndices.x);
                return;
            }

            for (int cellIndex = 0; cellIndex < Cells.Length; ++cellIndex)
            {
                ref VoronoiCell cell = ref Cells.ElementAt(cellIndex);

                // Sort edges clockwise order
                Edges.Sort(ref cell.Begin, ref cell.End,
                    new EdgeComparer {Vertices = (VoronoiVertex*)Vertices.GetUnsafePtr(), Center = cell.Site });

                //continue;

                // Add border edges
                if (cell.HasBorderEdges)
                {
                    var previous = cell.Begin;
                    for (var next = cell.Begin.Next; next != cell.End; next.MoveNext())
                    {
                        ConnectEdges(next, previous, next);

                        previous = next;
                    }

                    ConnectEdges(cell.End, cell.End.Previous, cell.Begin);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Cells.Dispose();
            Vertices.Dispose();
            Edges.Dispose();
            EdgeIndices.Dispose();
        }

        void CreateCornerVertices()
        {
            Bounds.GetPoints(out float2 a, out float2 b, out float2 c, out float2 d);
            CornerVertexIndices.x = CreateVertex(c, VoronoiVertexType.Corner);
            CornerVertexIndices.y = CreateVertex(d, VoronoiVertexType.Corner);
            CornerVertexIndices.z = CreateVertex(a, VoronoiVertexType.Corner);
            CornerVertexIndices.w = CreateVertex(b, VoronoiVertexType.Corner);
        }

        void ConnectEdges(NativeLinkedList<VoronoiEdge>.Iterator position, NativeLinkedList<VoronoiEdge>.Iterator from, NativeLinkedList<VoronoiEdge>.Iterator to)
        {
            int fromVertexIndex = from.Value.ToVertexIndex;
            int toVertexIndex = to.Value.FromVertexIndex;
            if (fromVertexIndex == toVertexIndex)
                return;

            var fromVertexType = Vertices[fromVertexIndex].Type;
            var toVertexType = Vertices[toVertexIndex].Type;

            // Only edges that intersect borders
            Assert.AreNotEqual(VoronoiVertexType.Default, fromVertexType);
            Assert.AreNotEqual(VoronoiVertexType.Default, toVertexType);

            // Iterate each corner clockwise order and create edge out of it
            while (fromVertexType != toVertexType)
            {
                switch (fromVertexType)
                {
                    case VoronoiVertexType.BorderUp:
                        CreateEdgeAt(position, fromVertexIndex, CornerVertexIndices.x);
                        fromVertexType = VoronoiVertexType.BorderRight;
                        fromVertexIndex = CornerVertexIndices.x;
                        break;
                    case VoronoiVertexType.BorderRight:
                        CreateEdgeAt(position, fromVertexIndex, CornerVertexIndices.y);
                        fromVertexType = VoronoiVertexType.BorderBottom;
                        fromVertexIndex = CornerVertexIndices.y;
                        break;
                    case VoronoiVertexType.BorderBottom:
                        CreateEdgeAt(position, fromVertexIndex, CornerVertexIndices.z);
                        fromVertexType = VoronoiVertexType.BorderLeft;
                        fromVertexIndex = CornerVertexIndices.z;
                        break;
                    case VoronoiVertexType.BorderLeft:
                        CreateEdgeAt(position, fromVertexIndex, CornerVertexIndices.w);
                        fromVertexType = VoronoiVertexType.BorderUp;
                        fromVertexIndex = CornerVertexIndices.w;
                        break;
                }
            }

            // If both vertices intersect same border then simply connect them
            if (fromVertexType == toVertexType)
            {
                Edges.Insert(position, new VoronoiEdge
                {
                    FromVertexIndex = fromVertexIndex,
                    ToVertexIndex = toVertexIndex,
                });
            }
        }

        int CreateVertex(float2 point, VoronoiVertexType type = VoronoiVertexType.Default)
        {
            int index = Vertices.Length;
            Vertices.Add(new VoronoiVertex
            {
                Point = point,
                Type = type,
            });
            return index;
        }

        NativeLinkedList<VoronoiEdge>.Iterator CreateEdgeAt(NativeLinkedList<VoronoiEdge>.Iterator position, int fromVertexIndex, int toVertexIndex)
        {
            return Edges.Insert(position, new VoronoiEdge
            {
                FromVertexIndex = fromVertexIndex,
                ToVertexIndex = toVertexIndex,
            });
        }

        struct EdgeComparer : IComparer<VoronoiEdge>
        {
            public VoronoiVertex* Vertices;
            public float2 Center;

            public int Compare(VoronoiEdge a, VoronoiEdge b)
            {
                // TODO: Check maybe there is more efficient way to order as atan2 and normalizesafe quite expensive operations

                float2 midPointA = (Vertices[a.ToVertexIndex].Point + Vertices[a.FromVertexIndex].Point) * 0.5f;
                float2 midPointB = (Vertices[b.ToVertexIndex].Point + Vertices[b.FromVertexIndex].Point) * 0.5f;

                float angleA = angle(normalizesafe(midPointA - Center));
                float angleB = angle(normalizesafe(midPointB - Center));

                return angleB.CompareTo(angleA);
            }
        }

        bool ClipEdgeWithBounds(
            double a, double b, double c, int leftVertexIndex, int rightVertexIndex, Rectangle rectangle, 
            out double2 leftVertex, out double2 rightVertex, 
            out VoronoiVertexType leftVertexType, out VoronoiVertexType rightVertexType)
        {
            double minX = rectangle.Min.x;
            double minY = rectangle.Min.y;
            double maxX = rectangle.Max.x;
            double maxY = rectangle.Max.y;

            if (leftVertexIndex != -1)
            {
                leftVertex = Vertices[leftVertexIndex].Point;
                leftVertexType = VoronoiVertexType.Default;
            }
            else if (a == 1.0 && b >= 0.0)
            {
                leftVertex = new double2(minX, maxY);
                leftVertexType = VoronoiVertexType.BorderUp;
            }
            else
            {
                leftVertex = new double2(minX, minY);
                leftVertexType = VoronoiVertexType.BorderLeft;
            }

            if (rightVertexIndex != -1)
            {
                rightVertex = Vertices[rightVertexIndex].Point;
                rightVertexType = VoronoiVertexType.Default;
            }
            else if (a == 1.0 && b >= 0.0)
            {
                rightVertex = new double2(maxX, minY);
                rightVertexType = VoronoiVertexType.BorderBottom;
            }
            else
            {
                rightVertex = new double2(maxX, maxY);
                rightVertexType = VoronoiVertexType.BorderRight;
            }

            /*if (leftVertexIndex == 0 && rightVertexIndex == -1)
            {
                ShapeGizmos.DrawChart((x) => (float)(c - a * x), new Rectangle(Bounds.Position - Bounds.Size, Bounds.Size * 3), 100, UnityEngine.Color.red);
                ShapeGizmos.DrawSolidCircle((float2)leftVertex, 0.1f, UnityEngine.Color.blue);
                ShapeGizmos.DrawSolidCircle((float2)rightVertex, 0.1f, UnityEngine.Color.red);
            }*/

            double x1 = leftVertex.x;
            double y1 = leftVertex.y;
            double x2 = rightVertex.x;
            double y2 = rightVertex.y;
            if (a == 1.0)
            {
                if (y1 <= minY)
                {
                    y1 = minY;
                    leftVertexType = VoronoiVertexType.BorderBottom;
                }
                else if (y1 >= maxY)
                {
                    y1 = maxY;
                    leftVertexType = VoronoiVertexType.BorderUp;
                }

                x1 = c - b * y1;

                if (y2 <= minY)
                {
                    y2 = minY;
                    rightVertexType = VoronoiVertexType.BorderBottom;
                }
                else if (y2 >= maxY)
                {
                    y2 = maxY;
                    rightVertexType = VoronoiVertexType.BorderUp;
                }

                x2 = c - b * y2;

                // TODO
                if (((x1 >= maxX) & (x2 >= maxX)) | ((x1 <= minX) & (x2 <= minX)))
                    return false;
                if (((y1 >= maxY) & (y2 >= maxY)) | ((y1 <= minY) & (y2 <= minY)))
                    return false;

                if (x1 >= maxX)
                {
                    x1 = maxX;
                    y1 = (c - x1) / b;
                    leftVertexType = VoronoiVertexType.BorderRight;
                }
                else if (x1 <= minX)
                {
                    x1 = minX;
                    y1 = (c - x1) / b;
                    leftVertexType = VoronoiVertexType.BorderLeft;
                }

                if (x2 >= maxX)
                {
                    x2 = maxX;
                    y2 = (c - x2) / b;
                    rightVertexType = VoronoiVertexType.BorderRight;
                }
                else if (x2 <= minX)
                {
                    x2 = minX;
                    y2 = (c - x2) / b;
                    rightVertexType = VoronoiVertexType.BorderLeft;
                }
            }
            else
            {
                if (x1 <= minX)
                {
                    x1 = minX;
                    leftVertexType = VoronoiVertexType.BorderLeft;
                }
                else if (x1 >= maxX)
                {
                    x1 = maxX;
                    leftVertexType = VoronoiVertexType.BorderRight;
                }

                y1 = c - a * x1;

                if (x2 <= minX)
                {
                    x2 = minX;
                    rightVertexType = VoronoiVertexType.BorderLeft;
                }
                else if (x2 >= maxX)
                {
                    x2 = maxX;
                    rightVertexType = VoronoiVertexType.BorderRight;
                }

                y2 = c - a * x2;

                // TODO
                if (((x1 >= maxX) & (x2 >= maxX)) | ((x1 <= minX) & (x2 <= minX)))
                    return false;
                if (((y1 >= maxY) & (y2 >= maxY)) | ((y1 <= minY) & (y2 <= minY)))
                    return false;

                if (y1 >= maxY)
                {
                    y1 = maxY;
                    x1 = (c - y1) / a;
                    leftVertexType = VoronoiVertexType.BorderUp;
                }
                else if (y1 <= minY)
                {
                    y1 = minY;
                    x1 = (c - y1) / a;
                    leftVertexType = VoronoiVertexType.BorderBottom;
                }

                if (y2 >= maxY)
                {
                    y2 = maxY;
                    x2 = (c - y2) / a;
                    rightVertexType = VoronoiVertexType.BorderUp;
                }
                else if (y2 <= minY)
                {
                    y2 = minY;
                    x2 = (c - y2) / a;
                    rightVertexType = VoronoiVertexType.BorderBottom;
                }
            }

            leftVertex = new double2(x1, y1);
            rightVertex = new double2(x2, y2);

            return true;
        }
    }
}
