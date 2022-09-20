using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using Unity.Entities.Hybrid;
using Unity.Jobs;
using static Unity.Mathematics.AABBExtensions;

using Random = Unity.Mathematics.Random;

namespace KWZTerrainECS
{
    [RequireComponent(typeof(AuthoringKzwTerrain))]
    public class RandomObstaclePlacement : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
#if UNITY_EDITOR
        public GameObject ObstacleDummyPrefab;
        public GameObject ObstacleTestCubePrefab;
#endif
        
        private TerrainSettings setting;
        private float2[] samples;
        
        private void Awake()
        {
            setting = GetComponent<AuthoringKzwTerrain>().TerrainSettings;
            
            using NativeArray<int> gridCells = new (setting.MapQuadCount, Allocator.TempJob);
            using NativeList<float2> activePoints = new (Allocator.TempJob);
            using NativeList<float2> samplePoints = new (Allocator.TempJob);

            JPoissonDiscGeneration job = new JPoissonDiscGeneration
            {
                MapQuadsAxis = setting.NumQuadsAxis,
                NumSampleBeforeReject = 10,
                Radius = 4,
                Prng = Random.CreateFromIndex(setting.NoiseSettings.Seed),
                DiscGrid = gridCells,
                ActivePoints = activePoints,
                SamplePoints = samplePoints
            };
            JobHandle jh = job.Schedule();
            jh.Complete();
            samples = samplePoints.ToArray();
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<TagTestObstacle>(entity);
            DynamicBuffer<BufferSamplesDisc> buffer = dstManager.AddBuffer<BufferSamplesDisc>(entity);
            buffer.EnsureCapacity(samples.Length);

            for (int i = 0; i < samples.Length; i++)
            {
                buffer.Add(samples[i]);
            }
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
#if UNITY_EDITOR
            referencedPrefabs.Add(ObstacleDummyPrefab);
            referencedPrefabs.Add(ObstacleTestCubePrefab);
#endif
        }
    }
}
