using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using ProjectDawn.Geometry3D;
using UnityEngine;

public class DrawSurface : MonoBehaviour
{
    public Mesh Mesh;
    public DrawRay IntersectionRay;
    public DrawSurface IntersectionSurface;

    public struct Transformer : ITransformFloat3
    {
        public float4x4 Matrix;

        public float3 Transform(float3 point)
        {
            return transform(Matrix, point);
        }
    }

    public TriangularSurface<Transformer> Surface => Mesh.ToTriangularSurface<Transformer>(Allocator.Temp, new Transformer { Matrix = transform.localToWorldMatrix });

    void OnDrawGizmos()
    {
        if (Mesh == null)
            return;

        using (var surface = Surface)
        {
            DrawSurfaceGizmo(surface, new Color(0, 1, 0, 0.3f));
            DrawWireSurface(surface, Color.green);

            if (IntersectionRay && IntersectionRay.Ray.Intersection(surface, out SurfacePointIntersection intersection))
            {
                DrawPoint(IntersectionRay.Ray.GetPoint(intersection.Time), new float3(0, 1, 0), 0.06f, Color.red);
                DrawTriangleGizmos(surface.GetTriangle(intersection.TriangleIndex), new Color(1, 0, 0, 0.3f));
            }
            else if (IntersectionSurface)
            {
                using (var intersectionSurface = IntersectionSurface.Surface)
                {
                    var intersections = new NativeList<SurfaceLineIntersection>(Allocator.Temp);
                    surface.Intersection(intersectionSurface, intersections);
                    foreach (var intersection2 in intersections)
                    {
                        DrawDottedLine(intersection2.Line, 3, Color.red);
                        DrawTriangleGizmos(surface.GetTriangle(intersection2.TriangleIndexA), new Color(1, 0, 0, 0.3f));
                    }
                    intersections.Dispose();
                }
            }
        }
    }

    void DrawSurfaceGizmo(TriangularSurface<Transformer> surface, Color color)
    {
        for (int i = 0; i < surface.NumTriangles; i++)
        {
            DrawTriangleGizmos(surface.GetTriangle(i), color);
        }
    }

    void DrawWireSurface(TriangularSurface<Transformer> surface, Color color)
    {
        for (int i = 0; i < surface.NumTriangles; i++)
        {
            DrawWireTriangle(surface.GetTriangle(i), color);
        }
    }

    void DrawTriangleGizmos(Triangle triangle, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawAAConvexPolygon(triangle.VertexA, triangle.VertexB, triangle.VertexC);
#endif
    }

    void DrawWireTriangle(Triangle triangle, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(triangle.VertexA, triangle.VertexB);
        UnityEditor.Handles.DrawLine(triangle.VertexB, triangle.VertexC);
        UnityEditor.Handles.DrawLine(triangle.VertexC, triangle.VertexA);
#endif
    }

    void DrawDottedLine(Line line, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Disabled;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawDottedLine(line.From, line.To, size);
#endif
    }

    void DrawPoint(float3 point, float3 normal, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Disabled;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawSolidDisc(point, normal, size);
#endif
    }
}
