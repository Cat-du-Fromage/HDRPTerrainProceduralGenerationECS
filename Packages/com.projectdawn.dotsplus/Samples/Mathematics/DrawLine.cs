using UnityEngine;
using Unity.Mathematics;
using ProjectDawn.Geometry2D;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;

public class DrawLine : DrawCurve
{
    public float A = 1;
    public float B;
    public float C;
    public Rectangle Bounds = new Rectangle(-3, 6);
    public int NumSteps = 100;
    public float DottedLineSize = 10;

    void OnDrawGizmos()
    {
        DrawGizmos(Bounds, NumSteps, DottedLineSize);
    }

    protected override float SolveForY(float x)
    {
        return line(A, B, C, x);
    }
}
