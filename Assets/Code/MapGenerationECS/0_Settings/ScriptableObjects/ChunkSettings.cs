using UnityEngine;

using static Unity.Mathematics.math;

namespace KWZTerrainECS
{
    [CreateAssetMenu(fileName = "ChunkSettingsECS", menuName = "KWZTerrainECS/ChunkSettings")]
    public class ChunkSettings : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public int NumQuadPerLine { get; private set; }
        public int QuadsCount { get; private set; }
        public int NumVertexPerLine { get; private set; }
        public int VerticesCount { get; private set; }
        public int TrianglesCount { get; private set; }
        public int TriangleIndicesCount { get; private set; }

        private void OnEnable()
        {
            NumQuadPerLine = max(1,ceilpow2(clamp(NumQuadPerLine, 1, 128))-1);
            QuadsCount = NumQuadPerLine * NumQuadPerLine;
            NumVertexPerLine = NumQuadPerLine + 1;
            VerticesCount = NumVertexPerLine * NumVertexPerLine;
            TrianglesCount = (NumQuadPerLine * NumQuadPerLine) * 2;
            TriangleIndicesCount = QuadsCount * 6;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NumQuadPerLine = max(1,ceilpow2(clamp(NumQuadPerLine, 1, 128))-1);
            QuadsCount = NumQuadPerLine * NumQuadPerLine;
            NumVertexPerLine = NumQuadPerLine + 1;
            VerticesCount = NumVertexPerLine * NumVertexPerLine;
            TrianglesCount = (NumQuadPerLine * NumQuadPerLine) * 2;
            TriangleIndicesCount = QuadsCount * 6;
        }
#endif
        
        public static implicit operator DataChunk(ChunkSettings chunk)
        {
            return new DataChunk
            {
                NumQuadPerLine = chunk.NumQuadPerLine,
                QuadsCount = chunk.QuadsCount,
                NumVerticesPerLine = chunk.NumVertexPerLine,
                VerticesCount = chunk.VerticesCount,
                TrianglesCount = chunk.TrianglesCount,
                TriangleIndicesCount = chunk.TriangleIndicesCount
            };
        }
    }
}