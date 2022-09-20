using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct DataChunk : IComponentData
    {
        public int NumQuadPerLine;
        public int QuadsCount;
        public int NumVerticesPerLine;
        public int VerticesCount;
        public int TrianglesCount;
        public int TriangleIndicesCount;
    }
}