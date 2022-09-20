using UnityEngine;
using Unity.Mathematics;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;

public class DrawBazierCurve : MonoBehaviour
{
    public int NumSteps = 100;
    public float DottedLineSize = 0.1f;

    void OnDrawGizmos()
    {
        var transforms = transform.GetComponentsInChildren<Transform>();

        if (transforms.Length == 3)
        {
            float2 a = transforms[1].position.asfloat().xy;
            float2 b = transforms[2].position.asfloat().xy;
            for (int i = 1; i < NumSteps; ++i)
            {
                float t0 = (float)(i - 1) / (NumSteps - 1);
                float t1 = (float)(i - 0) / (NumSteps - 1);
                float2 p0 = bazier(a, b, t0);
                float2 p1 = bazier(a, b, t1);
                DrawLine(p0, p1, Color.green);
            }
        }

        if (transforms.Length == 4)
        {
            float2 a = transforms[1].position.asfloat().xy;
            float2 b = transforms[2].position.asfloat().xy;
            float2 c = transforms[3].position.asfloat().xy;
            for (int i = 1; i < NumSteps; ++i)
            {
                float t0 = (float)(i-1) / (NumSteps - 1);
                float t1 = (float)(i-0) / (NumSteps - 1);
                float2 p0 = bazier(a, b, c, t0);
                float2 p1 = bazier(a, b, c, t1);
                DrawLine(p0, p1, Color.green);
            }
            DrawDottedLine(a, b, DottedLineSize, Color.gray);
            DrawDottedLine(b, c, DottedLineSize, Color.gray);
        }

        if (transforms.Length == 5)
        {
            float2 a = transforms[1].position.asfloat().xy;
            float2 b = transforms[2].position.asfloat().xy;
            float2 c = transforms[3].position.asfloat().xy;
            float2 d = transforms[4].position.asfloat().xy;
            for (int i = 1; i < NumSteps; ++i)
            {
                float t0 = (float)(i - 1) / (NumSteps - 1);
                float t1 = (float)(i - 0) / (NumSteps - 1);
                float2 p0 = bazier(a, b, c, d, t0);
                float2 p1 = bazier(a, b, c, d, t1);
                DrawLine(p0, p1, Color.green);
            }
            DrawDottedLine(a, b, DottedLineSize, Color.gray);
            DrawDottedLine(b, c, DottedLineSize, Color.gray);
            DrawDottedLine(c, d, DottedLineSize, Color.gray);
        }
    }

    static void DrawLine(float2 a, float2 b, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(a.asvector3(), b.asvector3());
#endif
    }

    static void DrawDottedLine(float2 a, float2 b, float size, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawDottedLine(a.asvector3(), b.asvector3(), size);
#endif
    }
}
