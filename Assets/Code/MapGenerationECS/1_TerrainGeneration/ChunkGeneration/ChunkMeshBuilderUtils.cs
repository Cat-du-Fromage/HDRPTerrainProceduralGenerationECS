using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using static UnityEngine.Mesh;

using float3 = Unity.Mathematics.float3;
using half4 = Unity.Mathematics.half4;
using Random = Unity.Mathematics.Random;

namespace KWZTerrainECS
{
    public static class ChunkMeshBuilderUtils
    {
        public static JobHandle SetNoiseJob(TerrainSettings terrain, int2 chunkCoord, NativeArray<float> noiseMap, JobHandle jobHandle = default)
        {
            NativeArray<float2> octaveOffsets = new(terrain.NoiseSettings.Octaves, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Random prng = Random.CreateFromIndex(terrain.NoiseSettings.Seed);
            
            float halfExtend = terrain.ChunkQuadsPerLine / 2f;
            float2 offsetChunk = (float2)chunkCoord * terrain.ChunkQuadsPerLine + halfExtend;

            for (int i = 0; i < terrain.NoiseSettings.Octaves; i++)
            {
                float offsetX = prng.NextFloat(-100000f, 100000f) + terrain.NoiseSettings.Offset.x;
                float offsetY = prng.NextFloat(-100000f, 100000f) - terrain.NoiseSettings.Offset.y;
                octaveOffsets[i] = float2(offsetX, offsetY) + offsetChunk;
            }

            JNoiseMap job = new()
            {
                MapSizeX = terrain.ChunkVerticesPerLine,
                Settings = terrain.NoiseSettings,
                NoiseMap = noiseMap,
                Octaves = octaveOffsets
            };
            JobHandle jh = job.ScheduleParallel(terrain.ChunkVerticesCount, JobWorkerCount - 1, jobHandle);
            return jh;
        }

        public static JobHandle SetMeshJob(ChunkSettings chunkSetting, MeshData meshData, NativeArray<float> noiseMap, JobHandle jobHandle = default)
        {
            JMeshDatas job = new()
            {
                MapSizeX = chunkSetting.NumVertexPerLine,
                NoiseMap = noiseMap,
                Vertices = meshData.GetVertexData<float3>(stream: 0),
                Uvs = meshData.GetVertexData<half2>(stream: 3),
                Triangles = meshData.GetIndexData<ushort>()
            };
            JobHandle jh = job.ScheduleParallel(chunkSetting.VerticesCount, JobWorkerCount - 1, jobHandle);
            return jh;
        }

        public static JobHandle SetNormalsJob(ChunkSettings chunkSetting, MeshData meshData, JobHandle jobHandle = default)
        {
            JNormals job = new()
            {
                MapSizeX = chunkSetting.NumVertexPerLine,
                Vertices = meshData.GetVertexData<float3>(stream: 0),
                Normals = meshData.GetVertexData<float3>(stream: 1),
            };
            JobHandle jh = job.ScheduleParallel(chunkSetting.VerticesCount, JobWorkerCount - 1, jobHandle);
            return jh;
        }

        public static JobHandle SetTangentsJob(ChunkSettings chunkSettings, MeshData meshData, JobHandle jobHandle = default)
        {
            NativeArray<float3> tangent1 = new(chunkSettings.VerticesCount,Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> tangent2 = new(chunkSettings.VerticesCount,Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            JTangentsPerTriangle job2 = new()
            {
                Triangles = meshData.GetIndexData<ushort>(),
                Uvs = meshData.GetVertexData<half2>(stream: 3),
                Vertices = meshData.GetVertexData<float3>(stream: 0),
                Tangent1 = tangent1,
                Tangent2 = tangent2
            };
            JobHandle jh2 = job2.ScheduleParallel(chunkSettings.TrianglesCount, JobWorkerCount - 1, jobHandle);

            JTangentsPerVertex job3 = new()
            {
                Normals = meshData.GetVertexData<float3>(stream: 1),
                Tangent1 = tangent1,
                Tangent2 = tangent2,
                Tangents = meshData.GetVertexData<half4>(stream: 2)
            };
            JobHandle jh3 = job3.ScheduleParallel(chunkSettings.VerticesCount, JobWorkerCount - 1, jh2);
            return jh3;
        }

        //TANGENT : MUST BE DONE AFTER NORMALS!
        //==================================================================================================================
        [BurstCompile(CompileSynchronously = true)]
        public struct JTangentsPerVertex : IJobFor
        {
            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> Normals;

            [ReadOnly, NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> Tangent1;

            [ReadOnly, NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> Tangent2;

            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<half4> Tangents;

            public void Execute(int index)
            {
                float3 normal = Normals[index];
                float3 tangent = Tangent1[index];
                // Gram-Schmidt orthogonalize
                //Vector3.OrthoNormalize( n, t );
                //Vector3 vecA_orth_to_vecB = normalize(vecA - dot (vecA, vecB) * vecB);
                float3 n = normalizesafe(normal - dot(normal, tangent) * tangent);
                float3 t = normalizesafe(tangent - dot(tangent, normal) * normal);

                half3 tanXYZ = new half3(t.xyz);
                half tanW = half(select(1f, -1f, dot(cross(n, t), Tangent2[index]) < 0));
                Tangents[index] = new half4(tanXYZ, tanW);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        public struct JTangentsPerTriangle : IJobFor
        {
            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<ushort> Triangles;

            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<half2> Uvs;

            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> Vertices;

            [NativeDisableParallelForRestriction] public NativeArray<float3> Tangent1;
            [NativeDisableParallelForRestriction] public NativeArray<float3> Tangent2;

            public void Execute(int index)
            {
                int triIndex = index * 3;
                int i1 = Triangles[triIndex + 0];
                int i2 = Triangles[triIndex + 1];
                int i3 = Triangles[triIndex + 2];

                float3 v1 = Vertices[i1];
                float3 v2 = Vertices[i2];
                float3 v3 = Vertices[i3];

                float2 w1 = Uvs[i1];
                float2 w2 = Uvs[i2];
                float2 w3 = Uvs[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1f / (s1 * t2 - s2 * t1);

                float3 sdir = new float3((t2 * x1 - t1 * x2), (t2 * y1 - t1 * y2), (t2 * z1 - t1 * z2)) * r;
                float3 tdir = new float3((s1 * x2 - s2 * x1), (s1 * y2 - s2 * y1), (s1 * z2 - s2 * z1)) * r;

                Tangent1[i1] += sdir;
                Tangent1[i2] += sdir;
                Tangent1[i3] += sdir;

                Tangent2[i1] += tdir;
                Tangent2[i2] += tdir;
                Tangent2[i3] += tdir;
            }
        }

        //NORMALS
        //==================================================================================================================
        [BurstCompile(CompileSynchronously = true)]
        public struct JNormals : IJobFor
        {
            [ReadOnly] public int MapSizeX;

            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> Vertices;

            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> Normals;

            public void Execute(int index)
            {
                int y = index / MapSizeX;
                int x = index - (y * MapSizeX);
                float3 normal = GetNormal(x, y, MapSizeX, true);
                Normals[index] = normal;
            }

            private float3 GetNormal(int x, int y, int width, bool isRightTriangleBottom) //quad split from bottom-right to top-left
            {
                int lastIndexWidth = width - 1;
                float3 normal = float3.zero;
                int numTriangleVertex = 1;

                if (IsCellOnCorner(x, y, lastIndexWidth))
                {
                    numTriangleVertex = select(2, 1, x == y);

                    if (x == 0 && y == 0) // bottom left
                    {
                        normal = GetNormalFromTopRight(x, y, width);
                    }
                    else if (x == lastIndexWidth && y == 0) // bottom right
                    {
                        normal = GetNormalFromTopLeft(x, y, width);
                    }
                    else if (x == 0 && y == lastIndexWidth) // top left
                    {
                        normal = GetNormalFromBottomRight(x, y, width);
                    }
                    else // if (x == lastIndexWidth && y == lastIndexWidth) // top right
                    {
                        normal = GetNormalFromBottomLeft(x, y, width);
                    }
                }
                else if (IsCellOnEdge(y, lastIndexWidth))
                {
                    numTriangleVertex = 3;
                    if (y == 0)
                    {
                        normal = GetNormalFromTopRight(x, y, width) + GetNormalFromTopLeft(x, y, width);
                    }
                    else // if (y == lastIndexWidth)
                    {
                        normal = GetNormalFromBottomRight(x, y, width) + GetNormalFromBottomLeft(x, y, width);
                    }

                }
                else if (IsCellOnEdge(x, lastIndexWidth))
                {
                    numTriangleVertex = 3;
                    if (x == 0)
                    {
                        normal = GetNormalFromTopRight(x, y, width) + GetNormalFromBottomRight(x, y, width);
                    }
                    else // if (x == lastIndexWidth)
                    {
                        normal = GetNormalFromTopLeft(x, y, width) + GetNormalFromBottomLeft(x, y, width);
                    }
                }
                else
                {
                    numTriangleVertex = 6;
                    normal = GetNormalFromTopRight(x, y, width) + GetNormalFromBottomRight(x, y, width) +
                             GetNormalFromTopLeft(x, y, width) + GetNormalFromBottomLeft(x, y, width);
                }

                return normalize(normal / numTriangleVertex);
            }

            private float3 GetNormalFromTopRight(int x, int y, int width)
            {
                float3 A = Vertices[mad(y, width, x)];
                float3 B = Vertices[mad(y, width, x + 1)];
                float3 C = Vertices[mad(y + 1, width, x)];

                float3 sideAB = B - A;
                float3 sideAC = C - A;
                return cross(sideAC, sideAB);
            }

            //2 triangles share A and B
            private float3 GetNormalFromTopLeft(int x, int y, int width)
            {
                float3 A = Vertices[mad(y, width, x)];
                float3 B = Vertices[mad(y + 1, width, x - 1)];

                float3 C1 = Vertices[mad(y + 1, width, x)];
                float3 C2 = Vertices[mad(y, width, x - 1)];

                float3 sideAB = B - A;
                float3 sideAC1 = C1 - A;
                float3 sideAC2 = C2 - A;
                return cross(sideAB, sideAC1) + cross(sideAC2, sideAB);
            }

            //2 triangles share A and B
            private float3 GetNormalFromBottomRight(int x, int y, int width)
            {
                float3 A = Vertices[mad(y, width, x)];
                float3 B = Vertices[mad(y - 1, width, x + 1)];

                float3 C1 = Vertices[mad(y - 1, width, x)];
                float3 C2 = Vertices[mad(y, width, x + 1)];

                float3 sideAB = B - A;
                float3 sideAC1 = C1 - A;
                float3 sideAC2 = C2 - A;
                return cross(sideAB, sideAC1) + cross(sideAC2, sideAC2);
            }


            private float3 GetNormalFromBottomLeft(int x, int y, int width)
            {
                float3 A = Vertices[mad(y, width, x)];
                float3 B = Vertices[mad(y, width, x - 1)];
                float3 C = Vertices[mad(y - 1, width, x)];

                float3 sideAB = B - A;
                float3 sideAC = C - A;
                return cross(sideAC, sideAB);
            }

            private bool IsCellOnEdge(int coord, int lastIndexWidth) => coord == 0 || coord == lastIndexWidth;

            private bool IsCellOnCorner(int x, int y, int lastIndexWidth) => (x == 0 && y == 0) ||
                                                                             (x == 0 && y == lastIndexWidth) ||
                                                                             (x == lastIndexWidth && y == 0) ||
                                                                             (x == lastIndexWidth &&
                                                                              y == lastIndexWidth);
        }

        [BurstCompile(CompileSynchronously = true)]
        public struct JMeshDatas : IJobFor
        {
            [ReadOnly] public int MapSizeX;

            [ReadOnly, NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float> NoiseMap;

            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<ushort> Triangles;

            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<half2> Uvs;

            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> Vertices;

            public void Execute(int index)
            {
                int y = index / MapSizeX;
                int x = index - (y * MapSizeX);

                int mapPoints = MapSizeX - 1;
                float halfSize = mapPoints * 0.5f;
                Vertices[index] = float3(x - halfSize, NoiseMap[index], y - halfSize);
                Uvs[index] = half2(float2(x / (float)MapSizeX, y / (float)MapSizeX));

                if (y < mapPoints && x < mapPoints)
                {
                    int baseTriIndex = (index - y) * 6;
                    //(0,0)-(1,0)-(0,1)-(1,1) 
                    int4 trianglesVertex = int4(index, index + 1, index + MapSizeX, index + MapSizeX + 1);
                    Triangles[baseTriIndex + 0] = (ushort)trianglesVertex.x; //(0,0)
                    Triangles[baseTriIndex + 1] = (ushort)trianglesVertex.z; //(1,0)
                    Triangles[baseTriIndex + 2] = (ushort)trianglesVertex.y; //(0,1)
                    Triangles[baseTriIndex + 3] = (ushort)trianglesVertex.y; //(0,1)
                    Triangles[baseTriIndex + 4] = (ushort)trianglesVertex.z; //(1,0)
                    Triangles[baseTriIndex + 5] = (ushort)trianglesVertex.w; //(1,1)
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        public struct JNoiseMap : IJobFor
        {
            [ReadOnly] public int MapSizeX;
            [ReadOnly] public NoiseSettingsData Settings;

            [ReadOnly, NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float2> Octaves;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<float> NoiseMap;

            public void Execute(int index)
            {
                float halfMapSize = MapSizeX * 0.5f;

                int y = index / MapSizeX;
                int x = index - y * MapSizeX;

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0;

                for (int i = 0; i < Settings.Octaves; i++)
                {

                    float sampleX = (x - halfMapSize + Octaves[i].x);
                    float sampleY = (y - halfMapSize + Octaves[i].y);
                    float2 sampleXY = float2(sampleX, sampleY) / Settings.Scale * frequency;

                    float pNoiseValue = snoise(sampleXY);
                    noiseHeight = mad(pNoiseValue, amplitude, noiseHeight);
                    amplitude = mul(amplitude, Settings.Persistence);
                    frequency = mul(frequency, Settings.Lacunarity);
                }

                NoiseMap[index] = mul(abs(lerp(0, 1f, noiseHeight)), Settings.HeightMultiplier);
            }
        }
    }
}
