using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using ProjectDawn.Mathematics;

namespace ProjectDawn.Geometry2D
{
    public static partial class ConvexPolygonUtility
    {
        /// <summary>
        /// Returns maximum inscribed circle in convex polygon.
        /// Polygon points must be convex and sorted counter clockwise.
        /// Based on https://stackoverflow.com/questions/27872964/confusion-on-delaunay-triangulation-and-largest-inscribed-circle
        /// </summary>
        static Circle MaxInscribedCircle<TTranform>(NativeSlice<float2> points, TTranform transform, float2 centroid)
            where TTranform : unmanaged, ITransformFloat2
        {
            // Idea of algorithm is to construct straight skeleton https://en.wikipedia.org/wiki/Straight_skeleton.
            // One of the points of the strigth skeleton will be center of maximum inscribed circle.
            // For more details check https://stackoverflow.com/questions/27872964/confusion-on-delaunay-triangulation-and-largest-inscribed-circle

            if (points.Length < 3)
                throw new System.ArgumentException("Can not get inscribed circle from less than 3 edges");

            int headIndex = 0;
            int numEdges = points.Length;

            var edges = new NativeArray<CollapseEdge>(numEdges, Allocator.Temp);

            // Construct a circular list of the polygon edges
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = transform.Transform(points[pointIndex]);
                float2 nextPoint = transform.Transform(points[(pointIndex + 1) % points.Length]);

                edges[pointIndex] = new CollapseEdge
                {
                    Line = new Line(point, nextPoint),
                    PreviousEdgeIndex = pointIndex == 0 ? numEdges - 1 : pointIndex - 1,
                    NextEdgeIndex = (pointIndex + 1) % points.Length,
                };
            }

            RecalculateNormals(edges, 0);

            // Until the queue is empty: Take out the next edge-collapse event: Remove the edge from the circular structure and update the collapse times of the neighboring edges of the removed edge.
            while (numEdges > 3)
            {
                // Find edge that will collapse firstly and the time of it
                CollapseEvent ev = FindCollapseEvent(edges, headIndex);

                // Shrink all edges by the collapse time 
                Shrink(edges, headIndex, ev.Time);

                // Recalculate normals that will be used for shrink
                RecalculateNormals(edges, headIndex);

                // Finally collapse the edge
                headIndex = Collapse(edges, ev);

                numEdges--;
            }

            // At this stage we are left with triangle and finding maximum inscribed circle of it is quite trivial
            CollapseEdge edge = edges[headIndex];
            CollapseEdge previousEdge = edges[edge.PreviousEdgeIndex];
            CollapseEdge nextEdge = edges[edge.NextEdgeIndex];
            float2 center = GetTriangleMaxInscribedCircleRadius(previousEdge.Line, edge.Line, nextEdge.Line);
            float radius = GetInscribedCircleRadius(points, transform, center);

            edges.Dispose();

            return new Circle(center, radius);
        }

        static float2 GetTriangleMaxInscribedCircleRadius(Line previousEdge, Line edge, Line nextEdge)
        {
            float2 normal = math2.perpendicularleft(edge.Direction);
            float2 previousNormal = math2.perpendicularleft(previousEdge.Direction);
            float2 nextNormal = math2.perpendicularleft(nextEdge.Direction);

            float2 smoothNormal0 = math.normalizesafe(normal + previousNormal);
            float2 smoothNormal1 = math.normalizesafe(normal + nextNormal);

            // wavefront propagation
            var n0 = new Ray(edge.From, smoothNormal0);
            var n1 = new Ray(edge.To, smoothNormal1);

            bool success = Ray.IntersectionPoint(n0, n1, out float2 collapsePoint);
            Assert.IsTrue(success);

            return collapsePoint;
        }

        static float GetInscribedCircleRadius<TTranform>(NativeSlice<float2> points, TTranform transform, float2 center)
            where TTranform : unmanaged, ITransformFloat2
        {
            var radius = float.MaxValue;
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = transform.Transform(points[pointIndex]);
                float2 nextPoint = transform.Transform(points[(pointIndex + 1) % points.Length]);

                Line edge = new Line(point, nextPoint);
                float distanceBetweenEdgeAndCentroid = edge.Distance(center);

                radius = math.min(radius, distanceBetweenEdgeAndCentroid);
            }
            return radius;
        }

        static int Collapse(NativeArray<CollapseEdge> edges, CollapseEvent ev)
        {
            CollapseEdge edge = edges[ev.EdgeIndex];
            CollapseEdge previousEdge = edges[edge.PreviousEdgeIndex];
            CollapseEdge nextEdge = edges[edge.NextEdgeIndex];

            // Connect previous and next edges
            previousEdge.NextEdgeIndex = edge.NextEdgeIndex;
            nextEdge.PreviousEdgeIndex = edge.PreviousEdgeIndex;

            // Collapse point
            previousEdge.Line.To = ev.CollapsePoint;
            nextEdge.Line.From = ev.CollapsePoint;

            edges[edge.PreviousEdgeIndex] = previousEdge;
            edges[edge.NextEdgeIndex] = nextEdge;

            return edge.NextEdgeIndex;
        }

        static void RecalculateNormals(NativeArray<CollapseEdge> edges, int edgeIndex)
        {
            int currentEdge = edgeIndex;
            while (true)
            {
                CollapseEdge edge = edges[currentEdge];

                edge.Normal = math2.perpendicularleft(edge.Line.Direction);

                edges[currentEdge] = edge;

                currentEdge = edges[currentEdge].NextEdgeIndex;

                if (currentEdge == edgeIndex)
                    break;
            }
        }

        static CollapseEvent FindCollapseEvent(NativeArray<CollapseEdge> edges, int headIndex)
        {
            CollapseEvent e = new CollapseEvent { EdgeIndex = -1, Time = float.MaxValue };
            int currentEdge = headIndex;
            while (true)
            {
                CollapseEdge edge = edges[currentEdge];
                CollapseEdge previousEdge = edges[edge.PreviousEdgeIndex];
                CollapseEdge nextEdge = edges[edge.NextEdgeIndex];

                float2 normal = edge.Normal;
                float2 previousNormal = previousEdge.Normal;
                float2 nextNormal = nextEdge.Normal;

                float2 smoothNormal0 = math.normalizesafe(normal + previousNormal);
                float2 smoothNormal1 = math.normalizesafe(normal + nextNormal);

                // wavefront propagation
                var n0 = new Ray(edge.Line.From, smoothNormal0);
                var n1 = new Ray(edge.Line.To, smoothNormal1);

                if (Ray.IntersectionPoint(n0, n1, out float2 collapsePoint))
                {
                    float collapseTime = edge.Line.Distance(collapsePoint);

                    if (collapseTime < e.Time)
                    {
                        e = new CollapseEvent
                        {
                            CollapsePoint = collapsePoint,
                            EdgeIndex = currentEdge,
                            Time = collapseTime,
                        };
                    }
                }

                currentEdge = edges[currentEdge].NextEdgeIndex;

                if (currentEdge == headIndex)
                    break;
            }
            Assert.AreNotEqual(e.EdgeIndex, -1);
            return e;
        }

        static void Shrink(NativeArray<CollapseEdge> edges, int edgeIndex, float time)
        {
            int currentEdge = edgeIndex;
            while (true)
            {
                CollapseEdge edge = edges[currentEdge];
                CollapseEdge previousEdge = edges[edge.PreviousEdgeIndex];
                CollapseEdge nextEdge = edges[edge.NextEdgeIndex];

                float2 normal = edge.Normal;
                float2 previousNormal = previousEdge.Normal;
                float2 nextNormal = nextEdge.Normal;

                float2 smoothNormal0 = math.normalizesafe(normal + previousNormal);
                float2 smoothNormal1 = math.normalizesafe(normal + nextNormal);

                float cos0 = math.dot(smoothNormal0, normal);
                float time0 = time / cos0;

                float cos1 = math.dot(smoothNormal1, normal);
                float time1 = time / cos1;

                edge.Line.From += smoothNormal0 * time0;
                edge.Line.To += smoothNormal1 * time1;

                edges[currentEdge] = edge;

                currentEdge = edges[currentEdge].NextEdgeIndex;

                if (currentEdge == edgeIndex)
                    break;
            }
        }

        struct CollapseEvent
        {
            public int EdgeIndex;
            public float2 CollapsePoint;
            public float Time;
        }

        struct CollapseEdge
        {
            public Line Line;
            public int PreviousEdgeIndex;
            public int NextEdgeIndex;
            public float2 Normal;

            public static CollapseEdge Null => new CollapseEdge
            {
                PreviousEdgeIndex = -1,
                NextEdgeIndex = -1
            };
        }
    }
}
