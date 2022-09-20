using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct BufferRandomSpawnPositions : IBufferElementData
    {
        public float3 Value;
        
        public static implicit operator BufferRandomSpawnPositions(float3 e)
        {
            return new BufferRandomSpawnPositions {Value = e};
        }
    }
}