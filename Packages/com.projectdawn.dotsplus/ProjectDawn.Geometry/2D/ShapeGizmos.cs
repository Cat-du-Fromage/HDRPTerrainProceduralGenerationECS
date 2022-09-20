using System.Diagnostics;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Helper class for drawing shapes using gizmos.
    /// </summary>
    public static partial class ShapeGizmos
    {
        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(float2 from, float2 to, Color color)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawLine(from.asvector3(), to.asvector3());
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawDottedLine(float2 from, float2 to, float screenSpaceSize, Color color)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawDottedLine(from.asvector3(), to.asvector3(), screenSpaceSize);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawDottedLine(Line line, float screenSpaceSize, Color color) => DrawDottedLine(line.From, line.To, screenSpaceSize, color);

        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Line line, Color color) => DrawLine(line.From, line.To, color);

        [Conditional("UNITY_EDITOR")]
        public static void DrawLineArrow(Line line, float size, Color color)
        {
            DrawLine(line, color);
            float lineAngle = angle(line.From - line.To);
            float arrowAngle = radians(135f);
            DrawLine(line.To, direction(lineAngle + arrowAngle) * size, color);
            DrawLine(line.To, direction(lineAngle - arrowAngle) * size, color);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawText(float2 point, string text, Color color)
        {
#if UNITY_EDITOR
            GUI.contentColor = color;
            UnityEditor.Handles.Label(point.asvector3(), text);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawWireCircle(float2 point, float size, Color color)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawWireDisc(point.asvector3(), new Vector3(0, 0, 1), size);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawSolidCircle(float2 point, float size, Color color)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawSolidDisc(point.asvector3(), new Vector3(0, 0, 1), size);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawWireCircle(Circle circle, Color color) => DrawWireCircle(circle.Center, circle.Radius, color);

        [Conditional("UNITY_EDITOR")]
        public static void DrawSolidCircle(Circle circle, Color color) => DrawSolidCircle(circle.Center, circle.Radius, color); 

        [Conditional("UNITY_EDITOR")]
        public static void DrawWireRectangle(Rectangle rectangle, Color color)
        {
            rectangle.GetPoints(out float2 a, out float2 b, out float2 c, out float2 d);
            DrawLine(a, b, color);
            DrawLine(b, c, color);
            DrawLine(c, d, color);
            DrawLine(d, a, color);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawSolidRectangle(Rectangle rectangle, Color color)
        {
            rectangle.GetPoints(out float2 a, out float2 b, out float2 c, out float2 d);
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAConvexPolygon(a.asvector3(), b.asvector3(), c.asvector3(), d.asvector3());
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawWirePolygon<T>(ConvexPolygon<T> polygon, Color color) where T : unmanaged, ITransformFloat2
        {
            bool isValid = polygon.IsValid();

            var points = polygon.Points;
            var transform = polygon.Transform;
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = transform.Transform(points[pointIndex]);
                float2 nextPoint = transform.Transform(points[(pointIndex + 1) % points.Length]);
                DrawLine(new Line(point, nextPoint), isValid ? color : Color.gray);
            }
        }

        public delegate float ChartFunction(float x);

        [Conditional("UNITY_EDITOR")]
        public static void DrawChart(ChartFunction chart, Rectangle bounds, int numSteps, Color color)
        {
            bounds.GetPoints(out float2 a, out float2 b, out float2 c, out float2 d);
            DrawDottedLine(new float2(bounds.Min.x, bounds.Center.y), new float2(bounds.Max.x, bounds.Center.y), bounds.Width / 5f, Color.gray);
            DrawDottedLine(new float2(bounds.Center.x, bounds.Min.y), new float2(bounds.Center.x, bounds.Max.y), bounds.Width / 5f, Color.gray);
            for (int i = 1; i < numSteps; ++i)
            {
                float2 previous;
                previous.x = bounds.Position.x + bounds.Width * ((float)(i - 1) / (numSteps - 1));
                previous.y = chart(previous.x);
                previous.y = clamp(previous.y, bounds.Min.y, bounds.Max.y);

                float2 current;
                current.x = bounds.Position.x + bounds.Width * ((float)i / (numSteps - 1));
                current.y = chart(current.x);
                current.y = clamp(current.y, bounds.Min.y, bounds.Max.y);

                DrawLine(previous, current, color);
            }
            DrawWireRectangle(bounds, Color.white);
        }
    }
}
