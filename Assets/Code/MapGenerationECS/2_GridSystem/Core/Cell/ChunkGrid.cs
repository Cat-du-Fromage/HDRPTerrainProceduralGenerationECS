using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KWZTerrainECS
{
    public struct ChunkNode : IComponentData
    {
        public bool4 AdjacentChunks;
        private FixedList64Bytes<DoorSide> doorSides;
        //public DoorSide this[int index] => doorSides[index];
    }

    public struct DoorSide
    {
        private FixedList32Bytes<Int16> test1;
    }
}