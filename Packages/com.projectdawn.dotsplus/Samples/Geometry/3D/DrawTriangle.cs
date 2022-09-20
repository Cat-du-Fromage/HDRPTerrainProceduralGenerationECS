using Unity.Mathematics;
using ProjectDawn.Geometry3D;
using UnityEngine;

public class DrawTriangle : MonoBehaviour
{
    public Transform PointA;
    public Transform PointB;
    public Transform PointC;
    public DrawRay IntersectionRay;
    public DrawTriangle IntersectionTriangle;
    public Triangle Triangle => new Triangle(PointA.position, PointB.position, PointC.position);

    void OnDrawGizmos()
    {
        if (PointA == null || PointB == null || PointC == null)
            return;

        var triangle = Triangle;

        if (IntersectionRay && IntersectionRay.Ray.Intersection(triangle, out float t))
        {
            DrawTriangleGizmos(triangle, new Color(1, 0, 0, 0.3f));
            DrawWireTriangle(triangle, Color.red);
            DrawPoint(IntersectionRay.Ray.GetPoint(t), triangle.Normal, 0.06f, Color.red);
        }
        else if (IntersectionTriangle && IntersectionTriangle.Triangle.Intersection(Triangle, out Line line))
        {
            DrawTriangleGizmos(triangle, new Color(1, 0, 0, 0.3f));
            DrawWireTriangle(triangle, Color.red);
            DrawDottedLine(line, 3, Color.red);
        }
        else
        {
            DrawTriangleGizmos(triangle, new Color(0, 1, 0, 0.3f));
            DrawWireTriangle(triangle, Color.green);
        }
    }

    void DrawTriangleGizmos(Triangle triangle, Color color)
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

    void DrawDottedLine(Line line, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawDottedLine(line.From, line.To, size);
#endif
    }

    void DrawPoint(float3 point, float3 normal, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawSolidDisc(point, normal, size);
#endif
    }
}
