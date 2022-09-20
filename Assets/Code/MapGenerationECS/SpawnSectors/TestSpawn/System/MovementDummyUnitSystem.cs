using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Physics.Math;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace KWZTerrainECS
{
    public partial class MovementDummyUnitSystem : SystemBase
    {
        private BeginInitializationEntityCommandBufferSystem beginInitEcb;
        
        private Entity grid;
        private AABB centerBounds;
        private int2 mapSizeXY;
        private BlobCells blobCells;
        
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<DataTerrain>();
            beginInitEcb = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            grid = GetSingletonEntity<DataTerrain>();
            
            EntityQuery query = GetEntityQuery(typeof(DataSpawnerAABB), typeof(DataSectorCardinal));
            NativeArray<Entity> spawners = query.ToEntityArray(Allocator.Temp);
            foreach (Entity spawner in spawners)
            {
                if (GetComponent<DataSectorCardinal>(spawner).Value == ESectors.Center)
                {
                    centerBounds = GetComponent<DataSpawnerAABB>(spawner).Value;
                }
            }
            mapSizeXY = GetComponent<DataTerrain>(grid).NumQuadsAxis;
            blobCells = GetComponent<BlobCells>(grid);
        }

        protected override void OnUpdate()
        {
            int2 mapXY = mapSizeXY;
            BlobCells blobCell = blobCells;
            float deltaTime = Time.DeltaTime;
            
            Entities
            .WithBurst()
            .WithAll<GenerateAuth_DummyUnit>()
            .ForEach((ref Translation translation, ref Rotation rotation) =>
            {
                ref GridCells gridCells = ref blobCell.Blob.Value;
                float2 direction = normalizesafe(float2.zero - translation.Value.xz);
                float2 newPosition = translation.Value.xz + direction * 1f * deltaTime;

                //float3 pos = gridCells.Get3DTranslatedPosition(newPosition, mapSizeXY);
                translation.Value = gridCells.Get3DTranslatedPosition(newPosition, mapXY);
                
                quaternion lookRotation =LookRotationLockY(translation.Value, float3.zero);
                rotation.Value = slerp(rotation.Value, lookRotation, deltaTime);
            }).ScheduleParallel();
            
            AABB centerBound = centerBounds;
            EntityCommandBuffer.ParallelWriter ecb = beginInitEcb.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithBurst()
            .WithAll<GenerateAuth_DummyUnit>()
            .ForEach((Entity unit, int entityInQueryIndex, in Translation translation) =>
            {
                if (!centerBound.Contains(translation.Value)) return;
                ecb.DestroyEntity(entityInQueryIndex, unit);
            }).ScheduleParallel();
            beginInitEcb.AddJobHandleForProducer(Dependency);
        }

        private static quaternion LookRotationLockY(float3 position, float3 target)
        {
            float3 direction3D = target - position;
            direction3D.y = 0;
            return quaternion.LookRotation(direction3D, up());
        }
        
        private quaternion RotateFSelf(in quaternion localRotation, float3 xyz)
        {
            quaternion eulerRot = quaternion.EulerZXY(xyz);
            return mul(localRotation, eulerRot);
        }
    }
}