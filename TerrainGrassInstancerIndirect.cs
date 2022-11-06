using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGrassInstancerIndirect : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;

    public Mesh mesh;
    public Material material;
    private Bounds bounds;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    float range;
    Vector3 terrainOffset;

    public int layerIndex = 0;
    public int population;
    public float grassThreshhold = 0.8f;
    public int rangeRectPadding = 0;


    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Color color;

        public static int Size() {
            return
                sizeof(float) * 4 * 4 +
                sizeof(float) * 4;
        }
    }


    private void Start() {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        Setup();
    }

    private void Update() {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDisable() {
        // Release gracefully.
        if (meshPropertiesBuffer != null) {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null) {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void Setup()
    {
        range = terrainData.size.x / 2;
        terrainOffset = new Vector3(transform.position.x + range, transform.position.y, transform.position.z + range);
        
        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(terrainOffset, Vector3.one * (range + range));

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        float[,,] maps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];
        for (int i = 0; i < population; i++) {
            MeshProperties props = new MeshProperties();

            float x = Random.Range( -range + rangeRectPadding, range - rangeRectPadding);
            float z = Random.Range( -range + rangeRectPadding, range - rangeRectPadding);
            float y = terrain.SampleHeight(terrainOffset + new Vector3(x, 0, z));
            
            Vector3Int alphamapCoord = ConvertToAlphamapCoordinates(terrainOffset + new Vector3(x, y, z));
            
            if(rangeRectPadding == 0)
            {
                if (!ContainsIndex(maps, alphamapCoord.x, dimension: 1))
                    continue;
 
                if (!ContainsIndex(maps, alphamapCoord.z, dimension: 0))
                    continue;
            }

            float textureBlendWeight = maps[alphamapCoord.z, alphamapCoord.x, layerIndex];

            if(textureBlendWeight >= grassThreshhold)
            {
                Vector3 position = new Vector3(x, y + 0.5f, z);
                Quaternion rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
                Vector3 scale = Vector3.one;

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = Color.Lerp(Color.blue, Color.green, Random.value);

                properties[i] = props;
            }
        }

        meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    private Vector3Int ConvertToAlphamapCoordinates(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - transform.position;

        return new Vector3Int(
            Mathf.RoundToInt((relativePosition.x / terrainData.size.x) * terrainData.alphamapWidth),
            0,
            Mathf.RoundToInt((relativePosition.z / terrainData.size.z) * terrainData.alphamapHeight)
        );
    }

    private bool ContainsIndex(float[,,] array, int index, int dimension)
    {
        if (index < 0)
            return false;

        return index < array.GetLength(dimension);
    }
}
