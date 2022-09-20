using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct BufferSamplesDisc : IBufferElementData
    {
        public float2 Value;
        
        public static implicit operator BufferSamplesDisc(float2 e)
        {
            return new BufferSamplesDisc {Value = e};
        }
    }
}