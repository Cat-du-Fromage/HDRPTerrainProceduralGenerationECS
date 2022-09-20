using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct BufferSpawnPositions : IBufferElementData
    {
        public float3 Value;
        
        public static implicit operator BufferSpawnPositions(float3 e)
        {
            return new BufferSpawnPositions {Value = e};
        }
    }
}