using Unity.Mathematics;
using Ray = ProjectDawn.Geometry3D.Ray;
using static Unity.Mathematics.math;
using UnityEngine;

public class DrawRay : MonoBehaviour
{
    public Transform Origin;

    public Ray Ray => new Ray(Origin.position, mul(transform.rotation, new float3(1, 0, 0)));

    void OnDrawGizmos()
    {
        if (Origin == null)
            return;

        Ray ray = Ray;

        DrawRayGizmos(ray, Color.green);
    }

    void DrawRayGizmos(Ray ray, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(ray.Origin, ray.Origin + ray.Direction * 1000);
#endif
    }
}
