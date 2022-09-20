using UnityEngine;
using Unity.Mathematics;
    
using static Unity.Mathematics.math;

namespace KWZTerrainECS
{
    [CreateAssetMenu(fileName = "TerrainSettingsECS", menuName = "KWZTerrainECS/TerrainSettings")]
    public class TerrainSettings : ScriptableObject
    {
        [field: SerializeField] public int NumChunkWidth { get; private set; }
        [field: SerializeField] public int NumChunkHeight { get; private set; }
        [field: SerializeField] public ChunkSettings ChunkSettings { get; private set; }
        [field: SerializeField] public NoiseSettings NoiseSettings { get; private set; }

        public int2 NumChunkAxis => new int2(NumChunkWidth, NumChunkHeight);
        public int ChunksCount => NumChunkWidth * NumChunkHeight;
        
        //QUAD
        public int NumQuadX => NumChunkWidth * ChunkSettings.NumQuadPerLine;
        public int NumQuadY => NumChunkHeight * ChunkSettings.NumQuadPerLine;

        public int2 NumQuadsAxis => new int2(NumQuadX, NumQuadY);
        public int MapQuadCount => NumQuadX * NumQuadY;
        
        //VERTEX
        public int NumVerticesX => NumQuadX + 1;
        public int NumVerticesY => NumQuadY + 1;
        public int2 NumVerticesAxis => new int2(NumVerticesX, NumVerticesY);
        public int MapVerticesCount => NumVerticesX * NumVerticesY;
        
        //CHUNK DIRECT ACCESS
        //==============================================================================================================
        //QUAD
        public int ChunkQuadsPerLine => ChunkSettings.NumQuadPerLine;
        public int ChunkQuadsCount => ChunkSettings.QuadsCount;
        
        //VERTEX
        public int ChunkVerticesPerLine => ChunkSettings.NumVertexPerLine;
        public int ChunkVerticesCount => ChunkSettings.VerticesCount;
        
        private void OnEnable()
        {
            NumChunkWidth = max(1, ceilpow2(NumChunkWidth));
            NumChunkHeight = max(1, ceilpow2(NumChunkHeight));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NumChunkWidth = max(1, ceilpow2(NumChunkWidth));
            NumChunkHeight = max(1, ceilpow2(NumChunkHeight));
        }
#endif
        public static implicit operator DataTerrain(TerrainSettings terrain)
        {
            return new DataTerrain
            {
                NumChunksAxis = terrain.NumChunkAxis,
                NumQuadsAxis = terrain.NumQuadsAxis,
                NumVerticesAxis = terrain.NumVerticesAxis
            };
        }
    }
}

