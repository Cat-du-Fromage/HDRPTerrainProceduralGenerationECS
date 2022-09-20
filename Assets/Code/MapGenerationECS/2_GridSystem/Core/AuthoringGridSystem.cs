using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

using static UnityEngine.Mesh;
using static KWZTerrainECS.Utilities;
using static Unity.Mathematics.math;
using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;

using int2 = Unity.Mathematics.int2;

namespace KWZTerrainECS
{
    [RequireComponent(typeof(AuthoringKzwTerrain))]
    [DisallowMultipleComponent]
    public partial class AuthoringGridSystem : MonoBehaviour, IConvertGameObjectToEntity
    {
        public EntityManager entityManager { get; private set; }
        
        private bool isActive;
        private AuthoringKzwTerrain terrain;
        private TerrainSettings TerrainSettings;

        private void Awake()
        {
            isActive = TryGetComponent(out AuthoringKzwTerrain comp);
            if (!isActive) return;
            
            terrain = comp;
            TerrainSettings = terrain.TerrainSettings;
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            entityManager = dstManager;
            if (!isActive) return;
            BlobAssetReference<GridCells> blob = CreateGridCells(TerrainSettings);
            dstManager.AddComponentData(entity, new BlobCells(){ Blob = blob });
        }

        private BlobAssetReference<GridCells> CreateGridCells(TerrainSettings terrainSetting)
        {
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref GridCells gridCells = ref builder.ConstructRoot<GridCells>();
            /*
            BlobBuilderArray<Cell> arrayBuilder = builder.Allocate(ref gridCells.Cells, terrainSetting.MapQuadCount);
            
            using MeshDataArray meshDataArray = AcquireReadOnlyMeshData(terrain.chunks.GetMeshesComponent());
            using NativeArray<float3> verticesNtv = GetOrderedVertices(terrain.chunks, terrainSetting, meshDataArray);

            NativeArray<float3> cellVertices = new (4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int cellIndex = 0; cellIndex < arrayBuilder.Length; cellIndex++)
            {
                (int x, int y) = GetXY(cellIndex, terrainSetting.NumQuadX);
                for (int vertexIndex = 0; vertexIndex < 4; vertexIndex++)
                {
                    (int xV, int yV) = GetXY(vertexIndex, 2);
                    int index = mad(y + yV, terrainSetting.NumVerticesX, x + xV);
                    cellVertices[vertexIndex] = verticesNtv[index];
                }
                arrayBuilder[cellIndex] = new Cell(terrainSetting ,x, y, cellVertices);
            }
            */
            BlobBuilderArray<Cell> arrayBuilder = ConstructGridArray(ref gridCells);
            
            BlobAssetReference<GridCells> result = builder.CreateBlobAssetReference<GridCells>(Allocator.Persistent);
            builder.Dispose();
            return result;

            BlobBuilderArray<Cell> ConstructGridArray(ref GridCells gridCells)
            {
                BlobBuilderArray<Cell> arrayBuilder = builder.Allocate(ref gridCells.Cells, terrainSetting.MapQuadCount);
            
                using MeshDataArray meshDataArray = AcquireReadOnlyMeshData(terrain.chunks.GetMeshesComponent());
                using NativeArray<float3> verticesNtv = GetOrderedVertices(terrain.chunks, terrainSetting, meshDataArray);

                NativeArray<float3> cellVertices = new (4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int cellIndex = 0; cellIndex < arrayBuilder.Length; cellIndex++)
                {
                    (int x, int y) = GetXY(cellIndex, terrainSetting.NumQuadX);
                    for (int vertexIndex = 0; vertexIndex < 4; vertexIndex++)
                    {
                        (int xV, int yV) = GetXY(vertexIndex, 2);
                        int index = mad(y + yV, terrainSetting.NumVerticesX, x + xV);
                        cellVertices[vertexIndex] = verticesNtv[index];
                    }
                    arrayBuilder[cellIndex] = new Cell(terrainSetting ,x, y, cellVertices);
                }
                return arrayBuilder;
            }
        }

        private NativeArray<float3> GetOrderedVertices(GameObject[] chunks, TerrainSettings terrainSetting, MeshDataArray meshDataArray)
        {
            NativeArray<float3> verticesNtv = new(terrainSetting.MapVerticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> chunkPosition = GetChunkPosition(chunks.Length);
            
            NativeArray<JobHandle> jobHandles = new(chunks.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                int2 chunkCoord = GetXY2(chunkIndex, terrainSetting.NumChunkWidth);
                
                jobHandles[chunkIndex] = new JReorderMeshVertices()
                {
                    ChunkIndex = chunkIndex,
                    ChunkCoord = chunkCoord,
                    TerrainNumVertexPerLine = terrainSetting.NumVerticesX,
                    ChunkNumVertexPerLine = terrainSetting.ChunkVerticesPerLine,
                    ChunkPositions = chunkPosition,
                    MeshVertices = meshDataArray[chunkIndex].GetVertexData<float3>(stream: 0),
                    OrderedVertices = verticesNtv
                }.ScheduleParallel(terrainSetting.ChunkVerticesCount,JobWorkerCount - 1,default);
            }
            JobHandle.CompleteAll(jobHandles);
            chunkPosition.Dispose();
            return verticesNtv;

         // INNER METHODS : GET CHUNK POSITIONS
         //==========================================================================================================
            NativeArray<float3> GetChunkPosition(int length)
            {
                NativeArray<float3> positions = new(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < chunks.Length; i++)
                    positions[i] = chunks[i].transform.position;
                return positions;
            }
        }
        
        [BurstCompile]
        private struct JReorderMeshVertices : IJobFor
        {
            [ReadOnly] public int ChunkIndex;
            [ReadOnly] public int2 ChunkCoord;
        
            [ReadOnly] public int TerrainNumVertexPerLine;
            [ReadOnly] public int ChunkNumVertexPerLine;
        
            [ReadOnly, NativeDisableParallelForRestriction] 
            public NativeArray<float3> ChunkPositions;
            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> MeshVertices;
        
            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> OrderedVertices;
        
            public void Execute(int index)
            {
                int2 cellCoord = GetXY(index);
            
                bool2 skipDuplicate = new (ChunkCoord.x > 0 && cellCoord.x == 0, ChunkCoord.y > 0 && cellCoord.y == 0);
                if (any(skipDuplicate)) return;

                int chunkNumQuadPerLine = ChunkNumVertexPerLine - 1;
                int2 offset = ChunkCoord * chunkNumQuadPerLine;
            
                int2 fullTerrainCoord = cellCoord + offset;
            
                int fullMapIndex = fullTerrainCoord.y * TerrainNumVertexPerLine + fullTerrainCoord.x;
                OrderedVertices[fullMapIndex] = ChunkPositions[ChunkIndex] + MeshVertices[index];
            }

            private int2 GetXY(int index)
            {
                int y = index / ChunkNumVertexPerLine;
                int x = index - (y * ChunkNumVertexPerLine);
                return new int2(x, y);
            }
        }
    }
}