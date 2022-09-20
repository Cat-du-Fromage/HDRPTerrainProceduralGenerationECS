using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct DataSectorCardinal : IComponentData
    {
        public ESectors Value;
    }
}