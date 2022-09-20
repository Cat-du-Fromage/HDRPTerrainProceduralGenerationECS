using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Entities.ComponentType;

namespace KWZTerrainECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial class SpawnerDummyUnitSystem : SystemBase
    {
        private EntityQuery unitPrefabQuery;
        private Keyboard keyboard;
        protected override void OnCreate()
        {
            unitPrefabQuery = GetEntityQuery(typeof(GenerateAuth_DummyUnit), typeof(Prefab));
            RequireForUpdate(unitPrefabQuery);
        }

        protected override void OnStartRunning()
        {
            keyboard = Keyboard.current;
        }

        protected override void OnUpdate()
        {
            TestSpawnUnitDummies();
        }
        
        //==============================================================================================================
        //MUST SPLIT INTO A DIFFERENT SCRIPT
        private void TestSpawnUnitDummies()
        {
            bool isActive = keyboard.pKey.wasReleasedThisFrame;
            if (!isActive) return;
            //if (!Input.GetKeyUp(KeyCode.P)) return;
            EntityQuery unitsQuery = GetEntityQuery(Exclude<Prefab>(), ReadOnly<GenerateAuth_DummyUnit>());
            if (!unitsQuery.IsEmpty) return;

            Entity unit = unitPrefabQuery.GetSingletonEntity();
            EntityCommandBuffer ecb = new (Allocator.Temp);
            Entities
            .WithoutBurst()
            .ForEach((in DataSectorCardinal cardinal, in DynamicBuffer<BufferRandomSpawnPositions> positions) =>
            {
                NativeArray<Entity> units = new (positions.Length, Allocator.Temp);
                ecb.Instantiate(unit, units);
                for (int i = 0; i < units.Length; i++)
                {
                    ecb.SetComponent(units[i], new Translation(){Value = positions[i].Value}); 
                    ecb.SetName(units[i], new FixedString64Bytes($"Unit_{cardinal.Value.ToString()}_{i}"));
                }
            }).Run();
            ecb.Playback(EntityManager);
        }
        //==============================================================================================================
    }
}