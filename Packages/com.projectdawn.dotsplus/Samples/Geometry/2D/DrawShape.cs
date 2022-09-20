using System;
using System.Collections.Generic;
using ProjectDawn.Geometry2D;
using ProjectDawn.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Mathematics;
using Ray = ProjectDawn.Geometry2D.Ray;
using static ProjectDawn.Geometry2D.ShapeGizmos;

public enum DrawShapeType
{
    Point,
    Line,
    Ray,
    Circle,
    Rectangle,
    Polygon,
}

public class DrawShape : MonoBehaviour
{
    public DrawShapeType Type = DrawShapeType.Circle;
    public List<float2> PolygonPoints;
    public float Size = 1;
    public bool BoundingRectangle;
    public bool CircumscribedCircle;
    public bool InscribedCircle;

    public DrawShape Target;

    public float2 Point => new float2(transform.position.x, transform.position.y);
    public float2 Scale => new float2(transform.lossyScale.x, transform.lossyScale.y);
    public Line Line => new Line(Transform(-new float2(Size, 0)), Transform(new float2(Size, 0)));
    public Ray Ray => Line.ToRay();
    public Circle Circle => new Circle(Point, Size * Scale.x);
    public Rectangle Rectangle => new Rectangle(Point - Size * Scale, Size * 2 * Scale);
    public ConvexPolygon<TransformFloat2> Polygon
    {
        get
        {
            var polygon = new ConvexPolygon<TransformFloat2>(PolygonPoints.Count, Allocator.Temp, Tran);
            for (int i = 0; i < PolygonPoints.Count; ++i)
                polygon[i] = PolygonPoints[i];
            return polygon;
        }
    }

    float2 Transform(float2 point)
    {
        float3 pointWS = transform.TransformPoint(point.asvector3());
        return pointWS.xy;
    }

    public struct TransformFloat2 : ITransformFloat2
    {
        public Matrix4x4 m_Transform;
        public TransformFloat2(Matrix4x4 transform) { m_Transform = transform; }
        public float2 Transform(float2 point)
        {
            float3 pointWS = m_Transform.MultiplyPoint3x4(point.asvector3());
            return pointWS.xy;
        }
    }

    public TransformFloat2 Tran => new TransformFloat2(transform.localToWorldMatrix);

    void OnDrawGizmos()
    {
        if (BoundingRectangle)
            DrawBoundingRectangle();
        if (CircumscribedCircle)
            DrawCircumscribedCircle();
        if (InscribedCircle)
            DrawInscribedCircle();

        switch (Type)
        {
            case DrawShapeType.Circle:
                DrawWireCircle(Circle, Overlap(Circle, Target) ? Color.red : Color.green);
                break;
            case DrawShapeType.Rectangle:
                DrawWireRectangle(Rectangle, Overlap(Rectangle, Target) ? Color.red : Color.green);
                break;
            case DrawShapeType.Line:
                DrawLine(Line, Overlap(Line, Target) ? Color.red : Color.green);
                break;
            case DrawShapeType.Ray:
                
                if (Intersection(Ray, Target, out Line intersection))
                {
                    DrawLine(Line, Color.yellow);
                    DrawLine(intersection, Color.red);
                }
                else
                {
                    DrawLine(Line, Color.green);
                }
                break;
            case DrawShapeType.Point:
                DrawLine(new Line(Point, ClosestPoint(Target, Point)), Color.yellow);
                break;
            case DrawShapeType.Polygon:
                using (var polygon = Polygon)
                    DrawWirePolygon(polygon, Overlap(polygon, Target) ? Color.red : Color.green);
                break;
        }
    }

    static float2 ClosestPoint(DrawShape a, float2 point)
    {
        if (a == null)
            return point;

        if (a.Type == DrawShapeType.Circle)
            return a.Circle.ClosestPoint(point);
        if (a.Type == DrawShapeType.Rectangle)
            return a.Rectangle.ClosestPoint(point);
        if (a.Type == DrawShapeType.Line)
            return a.Line.ClosestPoint(point);

        return point;
    }

    static bool Overlap(Circle a, DrawShape b)
    {
        if (b == null)
            return false;

        switch (b.Type)
        {
            case DrawShapeType.Circle:
                return a.Overlap(b.Circle);
            case DrawShapeType.Rectangle:
                return a.Overlap(b.Rectangle);
            case DrawShapeType.Line:
                return a.Overlap(b.Line);
            case DrawShapeType.Point:
                return a.Overlap(b.Point);
            case DrawShapeType.Polygon:
                using (var polygon = b.Polygon)
                {
                    throw new NotImplementedException();
                }
        }
        return false;
    }

    static bool Overlap(Rectangle a, DrawShape b)
    {
        if (b == null)
            return false;

        switch (b.Type)
        {
            case DrawShapeType.Circle:
                return a.Overlap(b.Circle);
            case DrawShapeType.Rectangle:
                return a.Overlap(b.Rectangle);
            case DrawShapeType.Line:
                return a.Overlap(b.Line);
            case DrawShapeType.Point:
                return a.Overlap(b.Point);
            case DrawShapeType.Polygon:
                using (var polygon = b.Polygon)
                {
                    throw new NotImplementedException();
                }
        }
        return false;
    }

    static bool Overlap(Line a, DrawShape b)
    {
        if (b == null)
            return false;

        switch (b.Type)
        {
            case DrawShapeType.Circle:
                return a.Overlap(b.Circle);
            case DrawShapeType.Rectangle:
                return a.Overlap(b.Rectangle);
            case DrawShapeType.Line:
                return a.Overlap(b.Line);
            case DrawShapeType.Point:
                return false;
            case DrawShapeType.Polygon:
                using (var polygon = b.Polygon)
                {
                    throw new NotImplementedException();
                }
        }
        return false;
    }

    static bool Intersection(Ray a, DrawShape b, out Line intersection)
    {
        intersection = new Line();
        if (b == null)
            return false;

        switch (b.Type)
        {
            case DrawShapeType.Circle:
                return a.IntersectionLine(b.Circle, out intersection);
            case DrawShapeType.Rectangle:
                return a.IntersectionLine(b.Rectangle, out intersection);
            case DrawShapeType.Line:
                throw new NotImplementedException();
            case DrawShapeType.Point:
                throw new NotImplementedException();
            case DrawShapeType.Polygon:
                using (var polygon = b.Polygon)
                {
                    throw new NotImplementedException();
                }
        }
        return false;
    }

    static bool Overlap(ConvexPolygon<TransformFloat2> a, DrawShape b)
    {
        if (b == null)
            return false;

        switch (b.Type)
        {
            case DrawShapeType.Circle:
                throw new NotImplementedException();
            case DrawShapeType.Rectangle:
                throw new NotImplementedException();
            case DrawShapeType.Line:
                throw new NotImplementedException();
            case DrawShapeType.Point:
                return a.ContainsPoint(b.Point);
            case DrawShapeType.Polygon:
                using (var polygon = b.Polygon)
                {
                    return a.Distance(polygon) == 0;
                }
        }
        throw new NotImplementedException();
    }

    void DrawBoundingRectangle()
    {
        switch (Type)
        {
            case DrawShapeType.Circle:
                DrawWireRectangle(Circle.BoundingRectangle(), Color.white);
                break;
            case DrawShapeType.Line:
                DrawWireRectangle(Line.BoundingRectangle(), Color.white);
                break;
            case DrawShapeType.Polygon:
                using (var polygon = Polygon)
                    DrawWireRectangle(polygon.BoundingRectangle(), Color.white);
                break;
        }
    }

    void DrawCircumscribedCircle()
    {
        switch (Type)
        {
            case DrawShapeType.Rectangle:
                DrawWireCircle(Rectangle.CircumscribedCircle(), Color.white);
                break;
            case DrawShapeType.Line:
                DrawWireCircle(Line.CircumscribedCircle(), Color.white);
                break;
        }
    }

    void DrawInscribedCircle()
    {
        switch (Type)
        {
            case DrawShapeType.Rectangle:
                DrawWireCircle(Rectangle.InscribedCircle(), Color.white);
                break;
            case DrawShapeType.Polygon:
                using (var polygon = Polygon)
                    DrawWireCircle(polygon.InscribedCircle(), Color.white);
                break;
        }
    }
}
