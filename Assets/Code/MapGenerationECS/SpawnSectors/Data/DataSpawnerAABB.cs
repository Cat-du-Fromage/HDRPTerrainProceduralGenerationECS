using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct DataSpawnerAABB : IComponentData
    {
        public AABB Value;
    }
}