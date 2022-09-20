using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Mathematics.math;

using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;
using static KWZTerrainECS.Utilities;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace KWZTerrainECS
{
    public class AuthoringKzwTerrain : MonoBehaviour, IConvertGameObjectToEntity
    {
        [field:SerializeField] public TerrainSettings TerrainSettings { get; private set; }
        [field:SerializeField] public SpawnSettings SpawnSettings { get; private set; }
        public GameObject[] chunks { get; private set; }
        
        private void Awake()
        {
            chunks = Build(TerrainSettings, TerrainSettings.ChunkSettings.Prefab);
            CreateSpawners();
        }
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.SetName(entity, "TerrainECS");
            DynamicBuffer<BufferChunk> chunksBuffer = dstManager.AddBuffer<BufferChunk>(entity);
            chunksBuffer.EnsureCapacity(chunks.Length);
            Array.ForEach(chunks, chunk => chunksBuffer.Add(conversionSystem.GetPrimaryEntity(chunk)));
            
            dstManager.AddComponentData(entity, (DataTerrain)TerrainSettings);
            dstManager.AddComponentData(entity, (DataChunk)TerrainSettings.ChunkSettings);
        }

        private void CreateSpawners()
        {
            for (int i = 0; i < SpawnSettings.NumSectors; i++)
            {
                if (!SpawnSettings[i]) continue;
                GameObject spawnerGo = Instantiate(SpawnSettings.Prefab);
                AuthoringSpawner newSpawner = spawnerGo.GetComponent<AuthoringSpawner>();
                newSpawner.CreateSpawnAt((ESectors)i, TerrainSettings);
            }
        }
        
        private GameObject[] Build(TerrainSettings terrain, GameObject chunkPrefab)
        {
            GameObject[] chunkArray = new GameObject[terrain.ChunksCount];

            using NativeArray<float3> positions = new (chunkArray.Length, TempJob, UninitializedMemory);
            JGetChunkPositions job = new ()
            {
                ChunkQuadsPerLine = terrain.ChunkQuadsPerLine,
                NumChunksAxis = terrain.NumChunkAxis,
                Positions = positions
            };
            job.ScheduleParallel(positions.Length,JobWorkerCount - 1, default).Complete();
            
            for (int i = 0; i < chunkArray.Length; i++)
            {
                GameObject chunk = Instantiate(chunkPrefab, positions[i], Quaternion.identity, transform);
                chunk.name = $"Chunk_{i}";
                chunkArray[i] = chunk;
            }
            return chunkArray;
        }
        
        [BurstCompile]
        private struct JGetChunkPositions : IJobFor
        {
            [ReadOnly] public int ChunkQuadsPerLine;
            [ReadOnly] public int2 NumChunksAxis;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float3> Positions;

            public void Execute(int index)
            {
                float halfSizeChunk = ChunkQuadsPerLine / 2f;
                int2 halfNumChunks = NumChunksAxis / 2; //we don't want 0.5!
                int2 coord = GetXY2(index, NumChunksAxis.x) - halfNumChunks;

                float2 positionOffset = mad(coord, ChunkQuadsPerLine, halfSizeChunk);
                float positionX = select(positionOffset.x, 0, halfNumChunks.x == 0);
                float positionY = select(positionOffset.y, 0, halfNumChunks.y == 0);
                
                Positions[index] = new float3(positionX, 0, positionY);
            }
        }
    }
    
    
}
