using UnityEngine;
using Unity.Mathematics;
using ProjectDawn.Geometry2D;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;

public abstract class DrawCurve : MonoBehaviour
{
    protected void DrawGizmos(Rectangle bounds, int numSteps = 100, float dottedLineSize = 0.1f)
    {
        DrawDottedLine(new float2(bounds.Min.x, bounds.Center.y), new float2(bounds.Max.x, bounds.Center.y), dottedLineSize, Color.gray);
        DrawDottedLine(new float2(bounds.Center.x, bounds.Min.y), new float2(bounds.Center.x, bounds.Max.y), dottedLineSize, Color.gray);
        for (int i = 1; i < numSteps; ++i)
        {
            float2 previous;
            previous.x = bounds.Position.x + bounds.Width * ((float)(i - 1) / (numSteps - 1));
            previous.y = SolveForY(previous.x);
            previous.y = clamp(previous.y, bounds.Min.y, bounds.Max.y);

            float2 current;
            current.x = bounds.Position.x + bounds.Width * ((float)i / (numSteps - 1));
            current.y = SolveForY(current.x);
            current.y = clamp(current.y, bounds.Min.y, bounds.Max.y);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(previous.asvector3() + transform.position, current.asvector3() + transform.position);
        }
        DrawRect(bounds, Color.white);
    }

    protected abstract float SolveForY(float x);

    void DrawRect(Rectangle rect, Color color)
    {
        Gizmos.color = color;
        var a = new Vector3(rect.Min.x, rect.Min.y) + transform.position;
        var b = new Vector3(rect.Max.x, rect.Min.y) + transform.position;
        var c = new Vector3(rect.Max.x, rect.Max.y) + transform.position;
        var d = new Vector3(rect.Min.x, rect.Max.y) + transform.position;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    void DrawDottedLine(float2 a, float2 b, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawDottedLine(a.asvector3() + transform.position, b.asvector3() + transform.position, size);
#endif
    }
}
