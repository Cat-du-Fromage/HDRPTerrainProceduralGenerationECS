using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct DataTerrain : IComponentData
    {
        public int2 NumChunksAxis;
        public int2 NumQuadsAxis;
        public int2 NumVerticesAxis;
    }
}