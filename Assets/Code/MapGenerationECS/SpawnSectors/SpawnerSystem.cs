using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

using static KWZTerrainECS.Utilities;
using static Unity.Mathematics.math;

using Random = Unity.Mathematics.Random;

namespace KWZTerrainECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SpawnerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<DataTerrain>();
        }

        protected override void OnStartRunning()
        {
            Entity grid = GetSingletonEntity<DataTerrain>();

            int2 mapXY = GetComponent<DataTerrain>(grid).NumQuadsAxis;
            BlobCells blobCells = GetComponent<BlobCells>(grid);

            Entities
            .WithoutBurst()
            .ForEach((DecalProjector projector, 
            ref DynamicBuffer<BufferSpawnPositions> positions, 
            ref DynamicBuffer<BufferRandomSpawnPositions> randPositions, 
            in LocalToWorld ltw, in DataSectorCardinal cardinal) =>
            {
                float3 scale = projector.size;
                NativeArray<int> indices = GetCellsIndicesInside(mapXY, ltw, scale);

                positions.Capacity = randPositions.Capacity = indices.Length;
                Random prng = Random.CreateFromIndex(512);
                
                ref GridCells gridCells = ref blobCells.Blob.Value;
                for (int i = 0; i < indices.Length; i++)
                {
                    int cellIndex = indices[i];
                    float3 center = gridCells.Cells[cellIndex].Center;
                    positions.Add(center);
                    
                    float3 offset = GetRandomPoint(ref prng, center.xz, 0.25f);
                    float3 pos = gridCells.Get3DTranslatedPosition(offset.xz, mapXY);
                    randPositions.Add(pos);
                }
            }).Run();
        }

        protected override void OnUpdate()
        {
            return;
        }

        private float3 GetRandomPoint(ref Random prng, float2 spawnPositionXZ, float radius)
        {
            float2 randDirection = prng.NextFloat2Direction();
            float distanceRadius = prng.NextFloat(0, radius);
            float2 sample = mad( randDirection, distanceRadius, spawnPositionXZ);
            return new float3(sample.x,0,sample.y);
        }
        
        private NativeArray<int> GetCellsIndicesInside(int2 numQuadsAxis, in LocalToWorld ltw, in float3 scale)
        {
            int3 spawnScale = (int3)scale;
            int2 numCellsXY = spawnScale.xy;

            int numCells = cmul(numCellsXY);
            NativeArray<int> results = new (numCells, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            float3 localForward = normalize(ltw.Up);
            float3 localRight = normalize(ltw.Right);

            float3 cornerBotLeft = GetBoundCorners(ltw.Position, localForward, localRight, scale*0.5f)[0];
            for (int i = 0; i < numCells; i++)
            {
                int2 coord = GetXY2(i, numCellsXY.x);
                float3 widthOffset = localRight * coord.x + (0.5f * localRight);
                float3 heightOffset = localForward * coord.y + (0.5f * localForward);
                float2 center = (cornerBotLeft + widthOffset + heightOffset).xz;
                results[i] = GetIndexFromPositionOffset(center, numQuadsAxis);
            }

            return results;
        }

        private NativeArray<float3> GetBoundCorners(in float3 center,in float3 localForward,in float3 localRight,in float3 extents)
        {
            NativeArray<float3> result = new(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            float3 yOffset = new float3(cmin(extents.xy)) * localForward;
            float3 xOffset = new float3(cmax(extents.xy)) * localRight;

            result[0] = center - yOffset - xOffset; //Bot-Left
            result[1] = center - yOffset + xOffset; //Bot-Right
            result[2] = center + yOffset - xOffset; //Top-Left
            result[3] = center + yOffset + xOffset; //Top-Right
            return result;
        }
    }
    
}