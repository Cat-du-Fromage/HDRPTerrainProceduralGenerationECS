using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Rendering;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

using static UnityEngine.Mesh;
using static KWZTerrainECS.Utilities;
using static KWZTerrainECS.ChunkMeshBuilderUtils;

namespace KWZTerrainECS
{
    [DisallowMultipleComponent]
    public class AuthoringChunk : MonoBehaviour, IConvertGameObjectToEntity
    {
        MeshFilter meshFilter;
        MeshRenderer meshRender;
        
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRender = GetComponent<MeshRenderer>();
            InitializeChunk();
        }
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            RenderMeshDescription desc = new (meshFilter.mesh, meshRender.material);
            RenderMeshUtility.AddComponents(entity, dstManager, desc);
        }

        private void InitializeChunk()
        {
            Transform parent = transform.parent;
            TerrainSettings terrain = parent.GetComponent<AuthoringKzwTerrain>().TerrainSettings;

            int index = transform.GetSiblingIndex();
            int2 halfChunkAxis = terrain.NumChunkAxis / 2;
            int2 coord = GetXY2(index, terrain.NumChunkWidth);
            CreateChunkAt(terrain,index, coord - halfChunkAxis);
        }
        
        private void CreateChunkAt(TerrainSettings terrain, int index, int2 coordOffset)
        {
            Mesh chunkMesh = BuildMesh(terrain, coordOffset.x, coordOffset.y);
            chunkMesh.name = $"ChunkMesh_{index}";
            
            meshFilter.mesh = chunkMesh;
            meshRender.localBounds = chunkMesh.bounds;
            GetComponent<PhysicsShapeAuthoring>().SetMesh(chunkMesh);
        }
        
        private Mesh BuildMesh(TerrainSettings terrainSettings, int x = 0, int y  = 0)
        {
            Mesh terrainMesh = GenerateChunk(terrainSettings, x, y);
            terrainMesh.RecalculateBounds();
            return terrainMesh;
        }
        
        //Here : need to construct according to X,Y position of the chunk
        private Mesh GenerateChunk(TerrainSettings terrain, int x = 0, int y  = 0)
        {
            int triIndicesCount = terrain.ChunkSettings.TriangleIndicesCount;
            int verticesCount = terrain.ChunkVerticesCount;
            
            MeshDataArray meshDataArray = AllocateWritableMeshData(1);
            MeshData meshData = meshDataArray[0];
            meshData.subMeshCount = 1;

            NativeArray<VertexAttributeDescriptor> vertexAttributes = InitializeVertexAttribute();
            meshData.SetVertexBufferParams(verticesCount, vertexAttributes);
            meshData.SetIndexBufferParams(triIndicesCount, IndexFormat.UInt16);

            NativeArray<float> noiseMap = new (verticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            JobHandle noiseJh    = SetNoiseJob(terrain,new int2(x,y), noiseMap);
            JobHandle meshJh     = SetMeshJob(terrain.ChunkSettings, meshData, noiseMap, noiseJh);
            JobHandle normalsJh  = SetNormalsJob(terrain.ChunkSettings, meshData, meshJh);
            JobHandle tangentsJh = SetTangentsJob(terrain.ChunkSettings, meshData, normalsJh);
            tangentsJh.Complete();
            
            SubMeshDescriptor descriptor = new(0, triIndicesCount) { vertexCount = verticesCount };
            meshData.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds);

            Mesh terrainMesh = new Mesh { name = "ProceduralTerrainMesh" };
            ApplyAndDisposeWritableMeshData(meshDataArray, terrainMesh);
            return terrainMesh;
        }
        
        private NativeArray<VertexAttributeDescriptor> InitializeVertexAttribute()
        {
            NativeArray<VertexAttributeDescriptor> vertexAttributes = new(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3, stream: 1);
            vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, dimension: 4, stream: 2);
            vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, dimension: 2, stream: 3);
            return vertexAttributes;
        }
    }
}
