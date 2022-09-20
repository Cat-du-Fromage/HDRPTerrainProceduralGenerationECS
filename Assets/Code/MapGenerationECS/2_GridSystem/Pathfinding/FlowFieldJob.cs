using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Collections.Allocator;
using static Unity.Mathematics.math;
using static KWZTerrainECS.Utilities;
using float2 = Unity.Mathematics.float2;

namespace KWZTerrainECS
{
    [BurstCompile]
    public struct JCostField : IJobFor
    {
        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<bool> Obstacles;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<byte> CostField;

        public void Execute(int index)
        {
            CostField[index] = (byte)select(1, byte.MaxValue, Obstacles[index]);
        }
    }
    

    [BurstCompile]
    public struct JIntegrationField : IJob
    {
        [ReadOnly] public int DestinationCellIndex;
        [ReadOnly] public int NumCellX;

        public NativeArray<byte> CostField;
        public NativeArray<int> BestCostField;

        public void Execute()
        {
            NativeQueue<int> cellsToCheck = new (Temp);
            NativeList<int> currentNeighbors = new (4, Temp);

            //Set Destination cell cost at 0
            CostField[DestinationCellIndex] = 0;
            BestCostField[DestinationCellIndex] = 0;

            cellsToCheck.Enqueue(DestinationCellIndex);

            while (cellsToCheck.Count > 0)
            {
                int currentCellIndex = cellsToCheck.Dequeue();
                GetNeighborCells(currentCellIndex, currentNeighbors);

                for (int i = 0; i < currentNeighbors.Length; i++)
                {
                    int neighborIndex = currentNeighbors[i];
                    if (CostField[neighborIndex] >= byte.MaxValue) continue;
                    if (CostField[neighborIndex] + BestCostField[currentCellIndex] < BestCostField[neighborIndex])
                    {
                        BestCostField[neighborIndex] = CostField[neighborIndex] + BestCostField[currentCellIndex];
                        cellsToCheck.Enqueue(neighborIndex);
                    }
                }
                currentNeighbors.Clear();
            }
        }

        private readonly void GetNeighborCells(int index, NativeList<int> curNeighbors)
        {
            int2 coord = GetXY2(index,NumCellX);
            for (int i = 0; i < 4; i++)
            {
                int neighborId = index.AdjCellFromIndex((1 << i), coord, NumCellX);
                if (neighborId == -1) continue;
                curNeighbors.AddNoResize(neighborId);
            }
        }
    }
    

    [BurstCompile]
    public struct JBestDirection : IJobFor
    {
        [ReadOnly] public int NumCellX;
        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<int> BestCostField;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float2> CellBestDirection;

        public void Execute(int index)
        {
            int currentBestCost = BestCostField[index];

            if (currentBestCost >= ushort.MaxValue)
            {
                CellBestDirection[index] = float2.zero;
                return;
            }

            int2 currentCellCoord = GetXY2(index, NumCellX);
            NativeList<int> neighbors = GetNeighborCells(index, currentCellCoord);
            for (int i = 0; i < neighbors.Length; i++)
            {
                int currentNeighbor = neighbors[i];
                if (BestCostField[currentNeighbor] < currentBestCost)
                {
                    currentBestCost = BestCostField[currentNeighbor];
                    int2 neighborCoord = GetXY2(currentNeighbor, NumCellX);
                    int2 bestDirection = neighborCoord - currentCellCoord;
                    CellBestDirection[index] = bestDirection;
                }
            }
        }

        private readonly NativeList<int> GetNeighborCells(int index, in int2 coord)
        {
            NativeList<int> neighbors = new (4, Temp);
            for (int i = 0; i < 4; i++)
            {
                int neighborId = index.AdjCellFromIndex((1 << i), coord, NumCellX);
                if (neighborId == -1) continue;
                neighbors.AddNoResize(neighborId);
            }
            return neighbors;
        }
    }
}
