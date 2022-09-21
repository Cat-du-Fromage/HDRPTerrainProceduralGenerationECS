using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct ChunkNodeGrid : IBufferElementData
    {
        public ChunkNode Value;
        
        public static implicit operator ChunkNodeGrid(ChunkNode e)
        {
            return new ChunkNodeGrid {Value = e};
        }
    }
    
    public struct ChunkNode
    {
        //public bool4 AdjacentChunks;

        public FixedList4096Bytes<int> CellsIndex;
        //FixedList264 dont exist...
        /*
        public FixedList512Bytes<int> TopGateWay;
        public FixedList512Bytes<int> RightGateWay;
        public FixedList512Bytes<int> BottomGateWay;
        public FixedList512Bytes<int> LeftGateWay;
*/
        public ChunkNode(NativeSlice<int> indices, int chunkQuadPerLine)
        {
            CellsIndex = new FixedList4096Bytes<int>();
            /*
            TopGateWay    = new FixedList512Bytes<int>();
            RightGateWay  = new FixedList512Bytes<int>();
            BottomGateWay = new FixedList512Bytes<int>();
            LeftGateWay   = new FixedList512Bytes<int>();
*/
            for (int i = 0; i < indices.Length; i++)
            {
                CellsIndex.Add(indices[i]);
            }
            /*
            unsafe
            {
                CellsIndex.AddRange(indices.GetUnsafeReadOnlyPtr(), indices.Length);
            }
            */
            /*
            for (int i = 0; i < indices.Length; i++)
            {
                int2 coord = Utilities.GetXY2(i, chunkQuadPerLine);
                if (coord.y == 0)
                {
                    BottomGateWay.Add(CellsIndex[i]);
                }
                if (coord.y == chunkQuadPerLine - 1)
                {
                    TopGateWay.Add(CellsIndex[i]);
                }
                if (coord.x == 0)
                {
                    LeftGateWay.Add(CellsIndex[i]);
                }
                if (coord.x == chunkQuadPerLine - 1)
                {
                    RightGateWay.Add(CellsIndex[i]);
                }
            }
            */
        }
    }

    public struct GateWay
    {
        public FixedList32Bytes<short> test1;
    }
}