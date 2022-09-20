using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static KWZTerrainECS.Utilities;

using int2 = Unity.Mathematics.int2;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

namespace KWZTerrainECS
{
    [BurstCompile]
    public struct JPoissonDiscGeneration : IJob
    {
        [ReadOnly] public int2 MapQuadsAxis;
        
        [ReadOnly] public int NumSampleBeforeReject;
        [ReadOnly] public float Radius;
        [ReadOnly] public Random Prng;

        public NativeArray<int> DiscGrid;
        public NativeList<float2> ActivePoints;
        public NativeList<float2> SamplePoints;

        public void Execute()
        {
            float radius2X = 2 * Radius;
            int2 mapSizeOffset = MapQuadsAxis / 2;
            
            float2 firstPoint = float2.zero;
            ActivePoints.Add(firstPoint);
            
            while (!ActivePoints.IsEmpty)
            {
                int spawnIndex = Prng.NextInt(ActivePoints.Length);
                float2 spawnPosition = ActivePoints[spawnIndex];
                bool accepted = false;
                for (int k = 0; k < NumSampleBeforeReject; k++)
                {
                    float2 randDirection = Prng.NextFloat2Direction();
                    float2 sample = mad(randDirection, Prng.NextFloat(Radius, radius2X), spawnPosition);

                    int2 sampleXY = new int2(sample);
                    if (SampleAccepted(sample, sampleXY, mapSizeOffset)) //TEST for rejection
                    {
                        SamplePoints.Add(sample - mapSizeOffset);
                        ActivePoints.Add(sample);
                        DiscGrid[mad(sampleXY.y, MapQuadsAxis.x, sampleXY.x)] = SamplePoints.Length;
                        accepted = true;
                        break;
                    }
                }
                if (!accepted) ActivePoints.RemoveAt(spawnIndex);
            }
        }

        private bool SampleAccepted(float2 samplePosition, int2 sampleCoord, int2 mapSizeOffset)
        {
            bool4 isInsideBound = new bool4(samplePosition >= float2.zero,samplePosition < MapQuadsAxis);
            if(!all(isInsideBound)) return false;
            
            float squareRadius = Radius * Radius;

            int2 searchStartXY = max(int2.zero, sampleCoord - 2);
            int2 searchEndXY = min(sampleCoord + 2, MapQuadsAxis - 1);

            // <= or it will created strange cluster of points at the borders of the map
            for (int y = searchStartXY.y; y <= searchEndXY.y; y++)
            {
                for (int x = searchStartXY.x; x <= searchEndXY.x; x++)
                {
                    int indexSample = DiscGrid[mad(y, MapQuadsAxis.x, x)] - 1;
                    if (indexSample != -1)
                    {
                        float2 sampleOffset = samplePosition - mapSizeOffset;
                        if (distancesq(sampleOffset, SamplePoints[indexSample]) < squareRadius) 
                            return false;
                    }
                }
            }
            return true;
        }
    }
}

/*
        private bool SampleAccepted(float2 samplePosition, int sampleX, int sampleY)
        {
            float squareRadius = Radius * Radius;
            
            bool4 isInsideBound = new bool4(samplePosition >= float2.zero,samplePosition < MapQuadsAxis);
            if(!all(isInsideBound)) return false;
            
            int searchStartX = max(0, sampleX - 2);
            int searchEndX = min(sampleX + 2, MapQuadsAxis.x - 1);

            int searchStartY = max(0, sampleY - 2);
            int searchEndY = min(sampleY + 2, MapQuadsAxis.y - 1);
            
            
            // <= or it will created strange cluster of points at the borders of the map
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    int indexSample = DiscGrid[mad(y, MapQuadsAxis.x, x)] - 1;
                    if (indexSample != -1)
                    {
                        float2 sampleOffset = samplePosition - MapQuadsAxis / 2;
                        if (distancesq(sampleOffset, SamplePoints[indexSample]) < squareRadius) 
                            return false;
                    }
                }
            }
            return true;
        }
*/