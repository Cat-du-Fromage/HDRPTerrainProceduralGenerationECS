using Unity.Entities;

namespace KWZTerrainECS
{
    public struct GridCells
    {
        public BlobArray<Cell> Cells;
    }
}