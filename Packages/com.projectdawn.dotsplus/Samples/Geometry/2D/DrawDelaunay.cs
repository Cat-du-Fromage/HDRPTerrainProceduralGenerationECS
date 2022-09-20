using UnityEngine;
using Unity.Collections;
using ProjectDawn.Collections;
using ProjectDawn.Mathematics;
using ProjectDawn.Geometry2D;
using ProjectDawn.Geometry3D;
using System.Collections.Generic;
using Unity.Mathematics;
using Line = ProjectDawn.Geometry2D.Line;
using static Unity.Mathematics.math;
using static ProjectDawn.Mathematics.math2;

public class DrawDelaunay : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Rectangle Bounds;

    void OnDrawGizmos()
    {
        using (var builder = new VoronoiBuilder(1, Allocator.Temp))
        {
            if (Transforms != null)
            {
                foreach (var transform in Transforms)
                {
                    if (transform == null)
                        continue;
                    builder.Add(transform.position.asfloat().xy);
                }
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    builder.Add(transform.position.asfloat().xy);
                }
            }

            var delaunay = new DelaunayTriangulation(builder.NumSites, Allocator.Temp);

            builder.Construct(ref delaunay);

            for (int i = 0; i < delaunay.Indices.Length; ++i)
            {
                var indices = delaunay.Indices[i];
                var triangle = new Triangle(delaunay.Points[indices.x].asfloat3(), delaunay.Points[indices.y].asfloat3(), delaunay.Points[indices.z].asfloat3());

                DrawTriangleGizmos(triangle, new Color(0, 1, 0, 0.3f));
                DrawWireTriangle(triangle, Color.green);
            }

            delaunay.Dispose();
        }
    }

    static void DrawTriangleGizmos(Triangle triangle, Color color)
    {
    #if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawAAConvexPolygon(triangle.VertexA, triangle.VertexB, triangle.VertexC);
    #endif
    }

    void DrawWireTriangle(Triangle triangle, Color color)
    {
    #if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(triangle.VertexA, triangle.VertexB);
        UnityEditor.Handles.DrawLine(triangle.VertexB, triangle.VertexC);
        UnityEditor.Handles.DrawLine(triangle.VertexC, triangle.VertexA);
    #endif
    }
}
