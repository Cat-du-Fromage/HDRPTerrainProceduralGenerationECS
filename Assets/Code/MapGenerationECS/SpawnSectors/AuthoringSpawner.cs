using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Rendering.HighDefinition;

using static Unity.Mathematics.math;
using static UnityEngine.Quaternion;
using static KWZTerrainECS.Utilities;

using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace KWZTerrainECS
{
    [DisallowMultipleComponent]
    public class AuthoringSpawner : MonoBehaviour, IConvertGameObjectToEntity
    {
        private ESectors spawnSector;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            float3 localScaleTransform = (transform.localScale / 2f);
            localScaleTransform.y = 10;
            
            AABB aabb = new() { Center = transform.position, Extents = localScaleTransform };
            dstManager.AddComponentData(entity, new DataSpawnerAABB() { Value = aabb });
            dstManager.AddComponentData(entity, new DataSectorCardinal() { Value = spawnSector });
            dstManager.AddBuffer<BufferSpawnPositions>(entity);
            dstManager.AddBuffer<BufferRandomSpawnPositions>(entity);
        }

        public void CreateSpawnAt(ESectors spawnESector, TerrainSettings setting)
        {
            spawnSector = spawnESector;
            const float centerMultiplier = 1.5f;
            int quadPerLine = setting.ChunkSettings.NumQuadPerLine;
            
            int rectHeightMul = floorlog2(quadPerLine);
            int rectWidthMul = quadPerLine - 2 * rectHeightMul;

            //we want Width to be the largest
            int widthScale  = max(1, max(setting.NumChunkWidth, setting.NumChunkHeight) / 2);
            //we want Height to be the smallest
            int heightScale = max(1, min(setting.NumChunkWidth, setting.NumChunkHeight) / 2);

            int width = rectWidthMul * widthScale * 2;
            int height = rectHeightMul * heightScale;
            int minWidthScale = min(setting.NumChunkWidth, setting.NumChunkHeight) * quadPerLine - 2 * max(1,rectHeightMul);
            
            Vector3 localScale = spawnESector != ESectors.Center
                ? new Vector3(min(minWidthScale,width), max(1,height), 5f)
                : new Vector3(max(1,height * centerMultiplier), max(1,height * centerMultiplier), 5f);
            
            //SET: POSITION AND ROTATION
            float2 halfRect = new float2(setting.NumChunkWidth, setting.NumChunkHeight) / 2f * quadPerLine;
            float halfScaleY = localScale.y / 2f;
            
            transform.SetPositionAndRotation(GetPosition(), GetRotation(transform.rotation));
            
            DecalProjector decal = GetComponent<DecalProjector>();
            decal.size = localScale;
            
            name = $"SpawnArea_{spawnESector.ToString()}";
            transform.localScale = localScale;
            
            //return spawn;
            
            // INNER METHODS
            //==========================================================================================================
            Vector3 GetPosition() => spawnESector switch
            {
                ESectors.Center => Vector3.zero,
                ESectors.Top    => new Vector3(0, 0, halfRect.y - halfScaleY),
                ESectors.Bottom => new Vector3(0, 0, -halfRect.y + halfScaleY),
                ESectors.Left   => new Vector3(-halfRect.x + halfScaleY, 0, 0),
                ESectors.Right  => new Vector3(halfRect.x - halfScaleY, 0, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(spawnESector), spawnESector, null)
            };

            Quaternion GetRotation(in Quaternion currentSpawnRotation) => spawnESector switch
            {
                ESectors.Center => currentSpawnRotation,
                ESectors.Top    => Euler(0, 180, 0) * currentSpawnRotation, // 180° rotation clock wise
                ESectors.Bottom => currentSpawnRotation,
                ESectors.Left   => Euler(0, 90, 0) * currentSpawnRotation, // 90° rotation clock wise
                ESectors.Right  => Euler(0, 270, 0) * currentSpawnRotation, // -90° or 270° rotation clock wise
                _ => throw new ArgumentOutOfRangeException(nameof(spawnESector), spawnESector, null)
            };
        }
    }
}