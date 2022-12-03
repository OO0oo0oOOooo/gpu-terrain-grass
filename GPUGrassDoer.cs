using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUGrassDoer : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain terrain;
    private TerrainData terrainData;
    private Vector3 terrainOffset;
    private float[,,] maps;


    [Header("Parameters")]
    // [SerializeField] private Transform viewer;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;


    [Header("Instancing Parameters")]
    [SerializeField] private int population = 10000;
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private float meshYOffset = 0f;
    [SerializeField] [Range(0,1)] private float grassThreshhold = 0.6f;
    [SerializeField] private float boundsY = 100f;


    [Header("Shadows")]
    [SerializeField] private ShadowCastingMode castShadows = ShadowCastingMode.Off;
    [SerializeField] private bool receiveShadows = true;


    [Header("Chunk Parameters")]
    [SerializeField] private int chunksInX = 5;
    [SerializeField] private int chunksInZ = 5;
    [SerializeField] private float renderDistanceInChunks = 4;
    [SerializeField] private bool debugChunks = false;

    private float renderDistance { get => renderDistanceInChunks*chunkSize.x; }
    private int chunkCount { get => chunksInX*chunksInZ; }
    private Vector2 chunkSize { get => new Vector2(Mathf.CeilToInt(terrainData.size.x / chunksInX), Mathf.CeilToInt(terrainData.size.z / chunksInZ)); } // converting to int might break something in some cases
    private int chunkPopulation { get => population / chunkCount; }
    private Chunk[] chunks;
    private uint[] args;
    private Bounds terrainBounds;

    private struct Chunk 
    {
        public ComputeBuffer argsBuffer;
        public ComputeBuffer meshDataBuffer;
        public Bounds bounds;
        public Material material;

        public Vector3 RandomPosition(Terrain terr)
        {
            Vector3 pos;
            float halfX = (bounds.size.x * 0.5f);
            float halfZ = (bounds.size.z * 0.5f);

            pos.x = Random.Range( -halfX, halfX);
            pos.z = Random.Range( -halfZ, halfZ);
            pos.y = terr.SampleHeight(bounds.center + new Vector3(pos.x, 0, pos.z));

            return pos;
        }
    }

    private struct MeshData
    {
        public Matrix4x4 mat;

        public static int Size()
        {
            return sizeof(float) * 4 * 4;
        }
    }

    void Start()
    {
        if(terrain == null)
            terrain = GetComponent<Terrain>();

        if(terrainData == null)
            terrainData = terrain.terrainData;
        
        args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)chunkPopulation;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);

        terrainOffset = new Vector3(transform.position.x + (terrainData.size.x / 2), transform.position.y, transform.position.z + (terrainData.size.z / 2));
        terrainBounds = new Bounds(terrainOffset, new Vector3(terrainData.size.x, boundsY, terrainData.size.x));

        maps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        InitializeChunks();
    }

    void InitializeChunks()
    {
        chunks = new Chunk[chunkCount];

        for (int x = 0; x < chunksInX; ++x) {
            for (int y = 0; y < chunksInZ; ++y)
            {
                chunks[x * (chunksInX) + y] = InitChunk(x, y);
            }
        }
    }

    Chunk InitChunk(int xOffset, int zOffset)
    {
        Chunk chunk = new Chunk();

        Vector3 center = Vector3.zero;
        float halfChunkX = (chunkSize.x * 0.5f);
        float halfChunkZ = (chunkSize.y * 0.5f);

        center.x = (-(halfChunkX * chunksInX) + chunkSize.x * xOffset) + halfChunkX;
        center.z = (-(halfChunkZ * chunksInZ) + chunkSize.y * zOffset) + halfChunkZ;
        center.y = 0;

        Vector3 worldspaceChunkPosition = terrainOffset + center;
        chunk.bounds = new Bounds(worldspaceChunkPosition, new Vector3(chunkSize.x, boundsY, chunkSize.y));

        MeshData[] meshData = new MeshData[chunkPopulation];
        for (int i = 0; i < chunkPopulation; i++) 
        {
            MeshData mesh = new MeshData();

            Vector3 randomChunkPosition = chunk.RandomPosition(terrain);
            Vector3Int alphamapCoord = ConvertToAlphamapCoordinates(worldspaceChunkPosition + randomChunkPosition);

            if (!ContainsIndex(maps, alphamapCoord.x, 1))
                continue;

            if (!ContainsIndex(maps, alphamapCoord.z, 0))
                continue;

            // I dont know why x and z have to be swapped but if it works its good enough for me
            float textureBlendWeight = maps[alphamapCoord.z, alphamapCoord.x, layerIndex];

            // Culled meshes create unused space in the buffer, else{i-=1} can cause infinite loop
            if(textureBlendWeight >= grassThreshhold)
            {
                Vector3 position = randomChunkPosition + new Vector3(0, meshYOffset, 0);
                Quaternion rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0); // Use normal of terrain
                Vector3 scale = new Vector3(1, 1 * textureBlendWeight, 1); // Use noise texture for height or do it in shader

                mesh.mat = Matrix4x4.TRS(position, rotation, scale);

                meshData[i] = mesh;
            }
        }

        chunk.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        chunk.meshDataBuffer =  new ComputeBuffer(chunkPopulation, MeshData.Size());

        chunk.argsBuffer.SetData(args);
        chunk.meshDataBuffer.SetData(meshData);

        chunk.material = new Material(material);
        chunk.material.SetBuffer("_Properties", chunk.meshDataBuffer);

        return chunk;
    }

    void Update()
    {
        for (int i = 0; i < chunkCount; i++)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, chunks[i].bounds.center);

            if(dist < renderDistance)
                Graphics.DrawMeshInstancedIndirect(mesh, 0, chunks[i].material, chunks[i].bounds, chunks[i].argsBuffer, 0, null, castShadows, receiveShadows);
        }
    }

    private Vector3Int ConvertToAlphamapCoordinates(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - transform.position;

        return new Vector3Int
        (
            Mathf.RoundToInt((relativePosition.x / terrainData.size.x) * terrainData.alphamapWidth),
            0,
            Mathf.RoundToInt((relativePosition.z / terrainData.size.z) * terrainData.alphamapHeight)
        );
    }

    private bool ContainsIndex(float[,,] arr, int index, int dimension)
    {
        if (index < 0)
            return false;
        
        return index < arr.GetLength(dimension);
    }

    void OnDisable() 
    {
        for (int i = 0; i < chunkCount; ++i) 
        {
            ReleaseChunk(chunks[i]);
        }

        chunks = null;
    }

    void ReleaseChunk(Chunk chunk)
    {
        if (chunk.meshDataBuffer != null) 
        {
            chunk.meshDataBuffer.Release();
            chunk.meshDataBuffer = null;
        }

        if (chunk.argsBuffer != null) 
        {
            chunk.argsBuffer.Release();
            chunk.argsBuffer = null;
        }
    }

    void OnDrawGizmos() 
    {
        if(!debugChunks)
            return;
        Gizmos.color = Color.yellow;
        if (chunks != null) 
        {
            for (int i = 0; i < chunkCount; ++i) 
            {
                Gizmos.DrawWireCube(chunks[i].bounds.center, chunks[i].bounds.size);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(terrainBounds.center, terrainBounds.size);
    }
}