using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static KWZTerrainECS.Utilities;
using static Unity.Mathematics.math;
using static Unity.Collections.Allocator;
using static Unity.Entities.ComponentType;

namespace KWZTerrainECS
{
    public partial class RandomObstacleSystem : SystemBase
    {
        private EntityQuery query;
        protected override void OnCreate()
        {
            query = GetEntityQuery(typeof(TagTestObstacle));
            RequireForUpdate(query);
        }

        protected override void OnStartRunning()
        {
            Entity grid = GetSingletonEntity<DataTerrain>();
            int2 mapXY = GetComponent<DataTerrain>(grid).NumQuadsAxis;
            BlobCells blobCells = GetComponent<BlobCells>(grid);

            PopulateObstacles(grid, mapXY, blobCells);
            Test(grid, mapXY, blobCells);
        }

        private void PopulateObstacles(Entity grid, int2 mapXY, BlobCells blobCells)
        {
            NativeArray<float2> spawn2D = GetSpawnSamples();
            NativeArray<Entity> obstacles = CreateObstacles(spawn2D.Length);
            NativeArray<DataSpawnerAABB> bounds = GetSpawnerBounds();

            ref GridCells gridCells = ref blobCells.Blob.Value;
            for (int i = 0; i < spawn2D.Length; i++)
            {
                float3 pos = gridCells.Get3DTranslatedPosition(spawn2D[i], mapXY);
                Entity obstacle = obstacles[i];
                bool checkBound = IsInsideSpawner(pos);
                
                if (checkBound)
                    EntityManager.DestroyEntity(obstacle);
                else
                    SetComponent(obstacle, new Translation(){Value = pos});
            }
            
            //INNER METHODS
            //==========================================================================================================
            NativeArray<DataSpawnerAABB> GetSpawnerBounds()
            {
                EntityQuery spawnerBounds = GetEntityQuery(typeof(DataSpawnerAABB));
                bounds = spawnerBounds.ToComponentDataArray<DataSpawnerAABB>(Temp);
                return bounds;
            }
            
            NativeArray<float2> GetSpawnSamples()
            {
                DynamicBuffer<BufferSamplesDisc> spawnBuffer = GetBuffer<BufferSamplesDisc>(grid);
                NativeArray<float2> spawn2D = spawnBuffer.ToNativeArray(Temp).Reinterpret<float2>();
                return spawn2D;
            }
            
            NativeArray<Entity> CreateObstacles(int num)
            {
                Entity prefab = GetEntityQuery(typeof(GenerateAuth_DummyObstacle), typeof(Prefab)).GetSingletonEntity();
                obstacles = new (num, Temp);
                EntityManager.Instantiate(prefab, obstacles);
                EntityManager.AddComponent<TagStaticObstacle>(obstacles);
                return obstacles;
            }
            
            bool IsInsideSpawner(float3 pos)
            {
                for (int j = 0; j < bounds.Length; j++)
                {
                    if (bounds[j].Value.Contains(pos)) return true;
                }
                return false;
            }
        }

        private void Test(Entity grid,int2 mapXY, BlobCells blobCells)
        {
            ref GridCells gridCells = ref blobCells.Blob.Value;
            int numCells = gridCells.Cells.Length;
            int2 halfMapSize = mapXY / 2;
            
            DynamicBuffer<BufferStaticObstacle> staticObstacles;
            BufferFromEntity<BufferStaticObstacle> checkHasBuffer = GetBufferFromEntity<BufferStaticObstacle>();
            if (!checkHasBuffer.TryGetBuffer(grid, out staticObstacles))
            {
                staticObstacles = EntityManager.AddBuffer<BufferStaticObstacle>(grid);
            }
            staticObstacles.EnsureCapacity(numCells);
            staticObstacles.AddRange(new NativeArray<BufferStaticObstacle>(numCells, Temp));

            EntityQuery obstacleQuery = GetEntityQuery(ReadOnly<TagStaticObstacle>(), ReadOnly<Translation>());
            NativeArray<float3> obstaclePositions = obstacleQuery.ToComponentDataArray<Translation>(Temp).Reinterpret<float3>();
            
            for (int i = 0; i < obstaclePositions.Length; i++)
            {
                float2 offsetPosition = obstaclePositions[i].xz + halfMapSize;
                int2 coord = (int2)floor(offsetPosition);
                int index = coord.y * mapXY.x + coord.x;
                
                staticObstacles[index] = true;
            }
            
            Entity prefab = GetEntityQuery(typeof(TestObstacleCube), typeof(Prefab)).GetSingletonEntity();
            for (int i = 0; i < obstaclePositions.Length; i++)
            {
                float2 offsetPosition = obstaclePositions[i].xz + halfMapSize;
                int2 coord = (int2)floor(offsetPosition);
                int index = coord.y * mapXY.x + coord.x;
                
                Entity obstacle = EntityManager.Instantiate(prefab);
                SetComponent(obstacle, new Translation(){Value = gridCells.Cells[index].Center});
            }
        }

        protected override void OnUpdate()
        {
            
        }
    }
}