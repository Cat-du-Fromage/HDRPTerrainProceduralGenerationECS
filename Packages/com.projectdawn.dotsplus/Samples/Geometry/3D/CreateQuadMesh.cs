using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using ProjectDawn.Geometry3D;
using UnityEngine;

[ExecuteAlways]
public class CreateQuadMesh : MonoBehaviour
{
    public struct Transformer : ITransformFloat3
    {
        public float3 Transform(float3 point) => point;
    }

    void Update()
    {
        var filter = GetComponent<MeshFilter>();
        if (filter.sharedMesh != null)
            return;

        using (var surface = new TriangularSurface<Transformer>(Allocator.Temp))
        {
            surface.Vertices.Add(new float3(-1, -1, 0));
            surface.Vertices.Add(new float3(-1, 1, 0));
            surface.Vertices.Add(new float3(1, 1, 0));
            surface.Vertices.Add(new float3(1, -1, 0));
            surface.Indices.Add(new int3(0, 1, 2));
            surface.Indices.Add(new int3(0, 2, 3));
            filter.sharedMesh = surface.ToMesh();
        }
    }
}
