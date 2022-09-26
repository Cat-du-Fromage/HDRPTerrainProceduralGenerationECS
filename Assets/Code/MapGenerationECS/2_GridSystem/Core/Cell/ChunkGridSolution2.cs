using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using static KWZTerrainECS.Sides;

using static KWZTerrainECS.Utilities;
using static Unity.Mathematics.math;
using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;

namespace KWZTerrainECS
{
    //[BurstCompile]
    public struct JConstructGrid : IJobFor
    {
        [ReadOnly] public int ChunkQuadPerLine;
        [ReadOnly] public int2 NumChunksXY;
        
        [WriteOnly, NativeDisableParallelForRestriction] 
        public NativeArray<GateWay> GateWays;

        public void Execute(int chunkIndex)
        {
            int2 terrainSize = NumChunksXY * ChunkQuadPerLine;
            
            int2 chunkCoord = GetXY2(chunkIndex, NumChunksXY.x);
            int2 offsetChunk = ChunkQuadPerLine * chunkCoord;

            for (int i = 0; i < 4; i++)
            {
                Sides side = (Sides)i;

                bool2 isXOffset = new(side == Left, side == Right);
                bool2 isYOffset = new(side == Top, side == Bottom);

                for (int j = 0; j < ChunkQuadPerLine; j++)
                {
                    int2 gateCoord = GetXY2(j, ChunkQuadPerLine) + offsetChunk;

					//gateCoord = select(gateCoord,gateCoord.yx,any(isXOffset));
					//gateCoord.x = select(gateCoord.x + ChunkQuadPerLine - 1, gateCoord.x, side == Left);
					//gateCoord.y = select(gateCoord.y + ChunkQuadPerLine - 1, gateCoord.y, side == Bottom);
					
                    gateCoord.x = select(gateCoord.x,select(offsetChunk.x - 1,offsetChunk.x,side == Left),any(isXOffset));
                    gateCoord.y = select(gateCoord.y,select(offsetChunk.y - 1,offsetChunk.y,side == Bottom),any(isYOffset));
                    
                    int2 offsetXY = new int2
                    (
                        select( select(1,-1,isXOffset.x), 0,!any(isXOffset) ),
                        select( select(1,-1,isYOffset.y), 0,!any(isYOffset) )
                    );
                    
                    //int offsetX = select( select(1,-1,isXOffset.x), 0,!any(isXOffset) );
                    //int offsetY = select( select(1,-1,isYOffset.y), 0,!any(isYOffset) );
                    
                    int gateIndex = gateCoord.y * terrainSize.x + gateCoord.x;
                    int2 coordOffset = gateCoord + offsetXY;
                    
                    int adjGateIndex = (coordOffset.y) * terrainSize.x + (coordOffset.x);

                    bool2 isOutLimit = new bool2
                    (
                        coordOffset.x < 0 || coordOffset.x > terrainSize.x - 1,
                        coordOffset.y < 0 || coordOffset.y > terrainSize.y - 1
                    );

                    int indexOffset = (chunkIndex * ChunkQuadPerLine * 4) + i * ChunkQuadPerLine + j;
                    GateWays[indexOffset] = any(isOutLimit) ? new GateWay() : new GateWay(gateIndex, adjGateIndex);
                }
            }
        }

        private void Test()
        {
            
        }
        
    }
    
    public static class ChunkGridSolution2
    {
        public static void BuildGrid(this DynamicBuffer<ChunkNodeGrid> buffer, int chunkSize, int2 numChunksXY)
        {
            int numChunks = cmul(numChunksXY);
            int bufferCapacity = (chunkSize * 4) * numChunks;
            buffer.EnsureCapacity(bufferCapacity);

            NativeArray<GateWay> gates = new (bufferCapacity, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            JConstructGrid job = new JConstructGrid
            {
                ChunkQuadPerLine = chunkSize,
                NumChunksXY = numChunksXY,
                GateWays = gates
            };
            job.ScheduleParallel(numChunks, JobWorkerCount - 1, default).Complete();
            buffer.CopyFrom(gates.Reinterpret<ChunkNodeGrid>());
            //buffer.Reinterpret<ChunkNodeGrid>();
            gates.Dispose();
            /*
            for (int i = 0; i < numChunks; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    gates.GetGateWays(i, (Sides)j, chunkSize, numChunksXY);
                }
                buffer.AddRange(gates.Reinterpret<ChunkNodeGrid>());
            }
            */
        }

        //WRONG use for build
        public static NativeArray<GateWay> GetGateWays(this NativeArray<GateWay> gates, int chunkIndex, Sides side, int chunkSize, int2 numChunksXY)
        {
            //NativeArray<GateWay> gates = new (chunkSize, Allocator.Temp);
            int2 terrainSize = numChunksXY * chunkSize;
            
            int2 chunkCoord = GetXY2(chunkIndex, numChunksXY.x);
            int2 offsetChunk = chunkSize * chunkCoord;
            
            bool2 isYOffset = new(side == Top, side == Bottom);
            bool2 isXOffset = new(side == Left, side == Right);
            for (int i = 0; i < chunkSize; i++)
            {
                int2 gateCoord = GetXY2(i, chunkSize) + offsetChunk;
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
