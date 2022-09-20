using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ProjectDawn.Collections;
using System.Diagnostics;

namespace ProjectDawn.Geometry3D
{
    public interface ITransformFloat3
    {
        /// <summary>
        /// Returns transformed point.
        /// </summary>
        float3 Transform(float3 point);
    }

    [DebuggerDisplay("NumTriangles = {NumTriangles}, IsCreated = {IsCreated}")]
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangularSurface<T>
        : IDisposable
        where T : unmanaged, ITransformFloat3
    {
        /// <summary>
        /// Surface vertices.
        /// </summary>
        public NativeList<float3> Vertices;

        /// <summary>
        /// Surfaces triangles.
        /// </summary>
        public NativeList<int3> Indices;

        /// <summary>
        /// Surface transformer.
        /// </summary>
        public T Transform;

        /// <summary>
        /// Returns if the surface is allocated.
        /// </summary>
        public bool IsCreated => Vertices.IsCreated && Indices.IsCreated;

        /// <summary>
        /// Returns the number of triangles.
        /// </summary>
        public int NumTriangles => Indices.Length;

        /// <summary>
        /// Returns world space triangle at index.
        /// </summary>
        /// <param name="triangleIndex">Index of triangle</param>
        /// <returns>Returns world space triangle at index.</returns>
        public Triangle GetTriangle(int triangleIndex)
        {
            CollectionChecks.CheckIndexInRange(triangleIndex, NumTriangles);

            int3 triangleIndices = Indices[triangleIndex];
            float3 a = Transform.Transform(Vertices[triangleIndices.x]);
            float3 b = Transform.Transform(Vertices[triangleIndices.y]);
            float3 c = Transform.Transform(Vertices[triangleIndices.z]);
            return new Triangle(a, b, c);
        }

        public TriangularSurface(Allocator allocator, T transform = default)
        {
            Vertices = new NativeList<float3>(allocator);
            Indices = new NativeList<int3>(allocator);
            Transform = transform;
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Indices.Dispose();
        }

        /// <summary>
        /// Returns surfaces intersections.
        /// </summary>
        /// <param name="surface">Surface.</param>
        /// <param name="intersections">Intersection data.</param>
        public void Intersection(TriangularSurface<T> surface, NativeList<SurfaceLineIntersection> intersections)
        {
            ShapeUtility.IntersectionTriangularSurfaceAndTriangularSurface(this, surface, intersections);
        }
    }

    public static class MeshExtensions
    {
        public unsafe static TriangularSurface<T> ToTriangularSurface<T>(this Mesh mesh, Allocator allocator, T transform = default) where T : unmanaged, ITransformFloat3
        {
            var surface = new TriangularSurface<T>(allocator, transform);

            // TODO: Use native arrays
            var vertices = mesh.vertices;
            surface.Vertices.Resize(vertices.Length, NativeArrayOptions.UninitializedMemory);
            surface.Vertices.AsArray().Reinterpret<Vector3>().CopyFrom(vertices);
            var triangles = mesh.triangles;
            surface.Indices.Resize(triangles.Length / 3, NativeArrayOptions.UninitializedMemory);
            surface.Indices.AsArray().Reinterpret<int>(sizeof(int3)).CopyFrom(triangles);

            return surface;
        }

        public unsafe static Mesh ToMesh<T>(this TriangularSurface<T> surface) where T : unmanaged, ITransformFloat3
        {
            var mesh = new Mesh();

            mesh.SetVertices(surface.Vertices.AsArray());
            mesh.SetIndices(surface.Indices.AsArray().Reinterpret<int>(sizeof(int3)), MeshTopology.Triangles, 0);

            return mesh;
        }
    }
}
