using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct BufferStaticObstacle : IBufferElementData
    {
        public bool Value;
        
        public static implicit operator BufferStaticObstacle(bool e)
        {
            return new BufferStaticObstacle {Value = e};
        }
    }
}