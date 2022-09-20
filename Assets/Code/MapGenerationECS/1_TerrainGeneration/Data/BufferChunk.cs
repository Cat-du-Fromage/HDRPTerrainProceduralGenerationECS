using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct BufferChunk : IBufferElementData
    {
        public Entity Value;
        
        public static implicit operator BufferChunk(Entity e)
        {
            return new BufferChunk {Value = e};
        }
    }
}