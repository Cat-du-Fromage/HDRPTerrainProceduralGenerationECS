using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static KWZTerrainECS.Utilities;

namespace KWZTerrainECS
{
    public struct Cell
    {
        public int2 Coord;
        public float3 Center;
        public FixedList64Bytes<float3> Vertices;

        public Cell(TerrainSettings settings, int x, int y, NativeArray<float3> cellVertex)
        {
            //Debug.Log($"size: {UnsafeUtility.SizeOf(typeof(ChunkGrid))}; NodeChunk: {UnsafeUtility.SizeOf(typeof(NodeChunk))}");
            Coord = new int2(x, y);
            Vertices = new FixedList64Bytes<float3>();
            //for (int i = 0; i < cellVertex.Length; i++) Vertices.Add(cellVertex[i]);
            unsafe {Vertices.AddRange(cellVertex.GetUnsafeReadOnlyPtr(),cellVertex.Length);}
            int cellIndex = y * settings.NumQuadX + x;
            float2 coord2D =GetXY2(cellIndex, settings.NumQuadX) - (float2)settings.NumQuadsAxis / 2f + float2(0.5f);
            float height = (cellVertex[1] + normalize(cellVertex[2] - cellVertex[1]) * (SQRT2/2)).y;
            Center = new float3(coord2D.x, height, coord2D.y);
        }
        
        public Cell(TerrainSettings settings, int2 coord, NativeArray<float3> cellVertex)
        {
            Coord = coord;
            Vertices = new FixedList64Bytes<float3>();
            
            //for (int i = 0; i < cellVertex.Length; i++) Vertices.Add(cellVertex[i]);
            unsafe { Vertices.AddRange(cellVertex.GetUnsafeReadOnlyPtr(), cellVertex.Length);}

            int cellIndex = coord.y * settings.NumQuadX + coord.x;
            float2 coord2D =GetXY2(cellIndex, settings.NumQuadX) - (float2)settings.NumQuadsAxis / 2f + float2(0.5f);
            float height = (cellVertex[1] + normalize(cellVertex[2] - cellVertex[1]) * (SQRT2/2)).y;
            Center = new float3(coord2D.x, height, coord2D.y);
        }

        public float HighestPoint => ceil(cmax(float4(Vertices[0].y, Vertices[1].y, Vertices[2].y, Vertices[3].y)));

        public float3 NormalTriangleLeft => normalizesafe(cross(Vertices[2] - Vertices[0], Vertices[1] - Vertices[0]));
        public float3 NormalTriangleRight => normalizesafe(cross(Vertices[1] - Vertices[3], Vertices[2] - Vertices[3]));
        
        public NativeSlice<float3> LeftTriangle => Vertices.ToNativeArray(Allocator.Temp).Slice(0, 3);
        public NativeSlice<float3> RightTriangle => Vertices.ToNativeArray(Allocator.Temp).Slice(1, 3);
        
        public readonly override string ToString()
        {
            return $"Coord: {Coord}; Center: {Center}";
        }
    }
    
    public static class TriangleProofOfConcept
    {
        public static float3 Get3DTranslatedPosition(this ref GridCells cells, float2 position2D, int2 mapSizeXY)
        {
            int cellIndex = GetIndexFromPositionOffset(position2D, mapSizeXY);
            Cell cell = cells.Cells[cellIndex];

            bool isLeftTri = IsPointInTriangle(cell.LeftTriangle, position2D);
            //bool isRightTri = IsPointInTriangle(cell.RightTriangle, position2D);

            //Ray origin
            float3 rayOrigin = new float3(position2D.x, cell.HighestPoint, position2D.y);
            //NORMAL
            float3 triangleNormal = isLeftTri ? cell.NormalTriangleLeft : cell.NormalTriangleRight;
            //Point A : start
            float3 a = isLeftTri ? cell.LeftTriangle[0] : cell.RightTriangle[0];
            float t = dot(a - rayOrigin, triangleNormal) / dot(down(), triangleNormal);
            return mad(t,down(), rayOrigin);
        }
        
        public static bool IsPointInTriangle(NativeSlice<float3> triangle, float2 position2D)
        {
            float2 triA = triangle[0].xz;
            float2 triB = triangle[1].xz;
            float2 triC = triangle[2].xz;
            
            bool isAEqualC = approximately(triC.y, triA.y);
            float2 a = select(triA, triB, isAEqualC);
            float2 b = select(triB, triA, isAEqualC);
            
            float s1 = triC.y - a.y;
            float s2 = triC.x - a.x;

            float s3 = b.y - a.y;
            float s4 = position2D.y - a.y;

            float w1 = (a.x * s1 + s4 * s2 - position2D.x * s1) / (s3 * s2 - (b.x - a.x) * s1);
            float w2 = (s4 - w1 * s3) / s1;
            return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
        }
    }
}