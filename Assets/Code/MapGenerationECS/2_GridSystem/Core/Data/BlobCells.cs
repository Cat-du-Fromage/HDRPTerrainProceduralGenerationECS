using System;
using Unity.Entities;

namespace KWZTerrainECS
{
    public struct BlobCells : IComponentData
    {
        public BlobAssetReference<GridCells> Blob;
    }
}