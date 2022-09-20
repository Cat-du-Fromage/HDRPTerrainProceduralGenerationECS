using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Collections.Allocator;
using static Unity.Mathematics.math;
using static KWZTerrainECS.Utilities;

namespace KWZTerrainECS
{
    public readonly struct Node
    {
        public readonly int CameFromNodeIndex;
        
        public readonly int GCost; //Distance from Start Node
        public readonly int HCost; // distance from End Node
        public readonly int FCost;
        public readonly int2 Coord;

        public Node(int cameFromNodeIndex, int gCost, int hCost, in int2 coord)
        {
            CameFromNodeIndex = cameFromNodeIndex;
            GCost = gCost;
            HCost = hCost;
            FCost = GCost + HCost;
            Coord = coord;
        }

        public Node(in int2 coord)
        {
            CameFromNodeIndex = -1;
            GCost = int.MaxValue;
            HCost = default;
            FCost = GCost + HCost;
            Coord = coord;
        }
    }
    
    [BurstCompile]
    public struct JaStar : IJob
    {
        [ReadOnly] public int NumCellX;
        [ReadOnly] public int StartNodeIndex;
        [ReadOnly] public int EndNodeIndex;
        
        [ReadOnly] public NativeArray<bool> ObstaclesGrid;
        [WriteOnly] public NativeList<int> PathList; // if PathNode.Length == 0 means No Path!
        
        public NativeArray<Node> Nodes;
        
        public void Execute()
        {
            NativeParallelHashSet<int> openSet = new (16, Temp);
            NativeParallelHashSet<int> closeSet = new (16, Temp);
            
            Nodes[StartNodeIndex] = StartNode(Nodes[StartNodeIndex], Nodes[EndNodeIndex]);
            openSet.Add(StartNodeIndex);

            NativeList<int> neighbors = new (8,Temp);

            while (!openSet.IsEmpty)
            {
                int currentNode = GetLowestFCostNodeIndex(openSet);
                
                //Check if we already arrived
                if (currentNode == EndNodeIndex)
                {
                    CalculatePath();
                    return;
                }

                //Add "already check" Node AND remove from "To check"
                openSet.Remove(currentNode);
                closeSet.Add(currentNode);

                //Add Neighbors to OpenSet
                GetNeighborCells(currentNode, neighbors, closeSet);
                if (neighbors.Length > 0)
                {
                    for (int i = 0; i < neighbors.Length; i++)
                    {
                        openSet.Add(neighbors[i]);
                    }
                }
                neighbors.Clear();
            }
            
        }

        private void CalculatePath()
        {
            PathList.Add(EndNodeIndex);
            int currentNode = EndNodeIndex;
            while(currentNode != StartNodeIndex)
            {
                currentNode = Nodes[currentNode].CameFromNodeIndex;
                PathList.Add(currentNode);
            }
        }
        
        private void GetNeighborCells(int index, NativeList<int> curNeighbors, NativeParallelHashSet<int> closeSet)
        {
            int2 coord = GetXY2(index,NumCellX);
            for (int i = 0; i < 8; i++)
            {
                int neighborId = index.AdjCellFromIndex(1 << i, coord, NumCellX);
                if (neighborId == -1 || ObstaclesGrid[neighborId] == true || closeSet.Contains(neighborId)) continue;

                int tentativeCost = Nodes[index].GCost + CalculateDistanceCost(Nodes[index],Nodes[neighborId]);
                if (tentativeCost < Nodes[neighborId].GCost)
                {
                    curNeighbors.Add(neighborId);
                    int gCost = CalculateDistanceCost(Nodes[neighborId], Nodes[StartNodeIndex]);
                    int hCost = CalculateDistanceCost(Nodes[neighborId], Nodes[EndNodeIndex]);
                    Nodes[neighborId] = new Node(index, gCost, hCost, Nodes[neighborId].Coord);
                }
            }
        }

        private int GetLowestFCostNodeIndex(NativeParallelHashSet<int> openSet)
        {
            int lowest = -1;
            foreach (int index in openSet)
            {
                //lowest = lowest == -1 ? index : lowest;
                lowest = select(lowest, index, lowest == -1);
                lowest = select(lowest, index, Nodes[index].FCost < Nodes[lowest].FCost);
            }
            return lowest;
        }

        private Node StartNode(in Node start, in Node end)
        {
            int hCost = CalculateDistanceCost(start, end);
            return new Node(-1, 0, hCost, start.Coord);
        }

        private int CalculateDistanceCost(in Node a, in Node b)
        {
            const int DiagonalCostMove = 14;
            const int StraightCostMove = 10;
            
            int2 xyDistance = abs(a.Coord - b.Coord);
            int remaining = abs(xyDistance.x - xyDistance.y);
            return DiagonalCostMove * cmin(xyDistance) + remaining * StraightCostMove;
        }
    }

    public struct JaStar2 : IJob
    {
        [ReadOnly] public int NumChunkX;
        
        [ReadOnly] public int StartChunkIndex;
        [ReadOnly] public int EndChunkIndex;

        [ReadOnly] public NativeArray<bool> ObstaclesGrid;
        
        [WriteOnly] public NativeList<int> PathList;

        public NativeArray<Node> Nodes;

        public void Execute()
        {
            NativeParallelHashSet<int> openSet = new (16, Temp);
            NativeParallelHashSet<int> closeSet = new (16, Temp);
            
            Nodes[StartChunkIndex] = StartNode(Nodes[StartChunkIndex], Nodes[EndChunkIndex]);
            openSet.Add(StartChunkIndex);
            
            NativeList<int> neighborsChunk = new (4,Temp);

            byte security = 0;
            while (!openSet.IsEmpty || security < byte.MaxValue - 1)
            {
                int currentNode = GetLowestFCostNodeIndex(openSet);
                openSet.Remove(currentNode);
                closeSet.Add(currentNode);
                
                GetNeighborChunks(currentNode, neighborsChunk, closeSet);
                
                security++;
            }
        }
        
        private void GetNeighborChunks(int index, NativeList<int> curNeighbors, NativeParallelHashSet<int> closeSet)
        {
            int2 coord = GetXY2(index,NumChunkX);
            for (int i = 0; i < 4; i++)
            {
                int neighborId = index.AdjCellFromIndex(1 << i, coord, NumChunkX);
                
                //Condition:
                // 1: ChunkNode[CURRENT-CHUNK].Side[i] ? ok : continue;
                
                if (neighborId == -1 /*|| ObstaclesGrid[neighborId] == true*/ || closeSet.Contains(neighborId)) continue;

                int tentativeCost = Nodes[index].GCost + CalculateDistanceCost(Nodes[index],Nodes[neighborId]);
                if (tentativeCost < Nodes[neighborId].GCost)
                {
                    curNeighbors.Add(neighborId);
                    int gCost = CalculateDistanceCost(Nodes[neighborId], Nodes[StartChunkIndex]);
                    int hCost = CalculateDistanceCost(Nodes[neighborId], Nodes[EndChunkIndex]);
                    Nodes[neighborId] = new Node(index, gCost, hCost, Nodes[neighborId].Coord);
                }
            }
        }
        
        private int GetLowestFCostNodeIndex(NativeParallelHashSet<int> openSet)
        {
            int lowest = -1;
            foreach (int index in openSet)
            {
                lowest = select(lowest, index, lowest == -1);
                lowest = select(lowest, index, Nodes[index].FCost < Nodes[lowest].FCost);
            }
            return lowest;
        }
        
        private Node StartNode(in Node start, in Node end)
        {
            int hCost = CalculateDistanceCost(start, end);
            return new Node(-1, 0, hCost, start.Coord);
        }
        
        private int CalculateDistanceCost(in Node a, in Node b)
        {
            int2 xyDistance = abs(a.Coord - b.Coord);
            return abs(xyDistance.x - xyDistance.y);
        }
    }
}
