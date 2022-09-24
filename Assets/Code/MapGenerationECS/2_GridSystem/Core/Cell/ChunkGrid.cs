using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using static KWZTerrainECS.Sides;

using static Unity.Mathematics.math;

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
    
    //==========================================================

    public enum Sides : int
    {
        Top    = 0,
        Right  = 1,
        Bottom = 2,
        Left   = 3
    }
    
    public struct ChunkNodes : IComponentData
    {
        public BlobAssetReference<ChunkNodesPool> Blob;
    }
    
    public struct ChunkNodesPool
    {
        public BlobArray<ChunkNode> Blob;
    }
    
    public struct ChunkNode
    {
        //public bool4 AdjacentChunks;
        
        public BlobArray<GateWay> TopGatePairIndices;
        public BlobArray<GateWay> RightGatePairIndices;
        public BlobArray<GateWay> BottomGatePairIndices;
        public BlobArray<GateWay> LeftGatePairIndices;
        
        public static ref ChunkNode Build(int index, int numChunkPerLine, int2 numChunksXY, int chunkSize)
        {
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref ChunkNode chunkNode = ref builder.ConstructRoot<ChunkNode>();
            
            int2 chunkCoord = Utilities.GetXY2(index, numChunkPerLine);
            
            //int numSide = 4;
            //numSide -= select(0, 1, chunkCoord.y == 0 || chunkCoord.y == numChunksXY.y - 1);
            //numSide -= select(0, 1, chunkCoord.x == 0 || chunkCoord.x == numChunksXY.x - 1);

            int2 terrainSize = numChunksXY * chunkSize;
            for (int i = 0; i < 4; i++)
            {
                //BlobBuilderArray<GateWay> arrayBuilder = builder.Allocate(ref chunkNode.GatePairIndices, numSide * chunkSize);
                Sides side = (Sides)i;

                if (side == Bottom && chunkCoord.y == 0)                 continue;
                if (side == Top    && chunkCoord.y == numChunksXY.y - 1) continue;
                if (side == Left   && chunkCoord.x == 0)                 continue;
                if (side == Right  && chunkCoord.x == numChunksXY.x - 1) continue;

                NativeArray<GateWay> gateWays = GetGateWaysIndices(side);

                switch (side)
                {
                    case Top:
                        builder.Allocate(ref chunkNode.TopGatePairIndices, gateWays.Length);
                        builder.Construct(ref chunkNode.TopGatePairIndices, gateWays.ToArray());
                        break;
                    case Right:
                        builder.Allocate(ref chunkNode.RightGatePairIndices, gateWays.Length);
                        builder.Construct(ref chunkNode.TopGatePairIndices, gateWays.ToArray());
                        break;
                    case Bottom:
                        builder.Allocate(ref chunkNode.BottomGatePairIndices, gateWays.Length);
                        builder.Construct(ref chunkNode.TopGatePairIndices, gateWays.ToArray());
                        break;
                    case Left:
                        builder.Allocate(ref chunkNode.LeftGatePairIndices, gateWays.Length);
                        builder.Construct(ref chunkNode.TopGatePairIndices, gateWays.ToArray());
                        break;
                }
            }
            builder.Dispose();
            
            return ref chunkNode;

            //==================================================
            NativeArray<GateWay> GetGateWaysIndices(Sides side)
            {
                NativeArray<GateWay> gates = new (chunkSize, Allocator.Temp);
                int2 offsetChunk = chunkSize * chunkCoord;

                bool2 isYOffset = new(side == Top, side == Bottom);
                bool2 isXOffset = new(side == Left, side == Right);

                for (int i = 0; i < chunkSize; i++)
                {
                    int2 gateCoord = Utilities.GetXY2(i, chunkSize) + offsetChunk;
                    int offsetY = select( select(1,-1,isYOffset.y), 0,!any(isYOffset) );
                    int offsetX = select( select(1,-1,isXOffset.x), 0,!any(isXOffset) );
                    
                    int gateIndex = gateCoord.y * terrainSize.x + gateCoord.x;
                    int adjGateIndex = (gateCoord.y + offsetY) * terrainSize.x + (gateCoord.x + offsetX);
                    gates[i] = new GateWay(gateIndex, adjGateIndex);
                }

                return gates;
            }
        }
    }

    //==========================================================
    
    public readonly struct GateWay
    {
        public readonly int Index;
        public readonly int IndexAdjacent;

        public GateWay(int index, int indexAdj)
        {
            Index = index;
            IndexAdjacent = indexAdj;
        }
    }
}