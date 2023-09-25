using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MashGen : MonoBehaviour
{

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    public Material material;
    public ComputeShader shader;

    const int threadGroupSize = 8;
    public int numPointsPerAxis = 50;
    public float boundsSize = 20;
    float isoLevel = 0.5f;
    public float brush = 5;
    public float baseHight = 5;
    public float perlinNoiseScale = 2;
    public float perlinNoiseHightScale = 5;

    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;

    Mesh mesh;
    MeshCollider meshCollider;

    Vector4[] activePoints;

    private void Start()
    {
        GameObject go = this.gameObject;
        mesh = InitMesh(ref go, ref material);
        meshCollider = InitMeshCollider(ref go, ref mesh);

        meshCollider.sharedMesh = mesh;

        GenRandomMap();

        CreateBuffers();

        UpdateMash();
    }

    // Update is called once per frame
    void Update()
    {
        CreateBuffers();

        Vector3? mousePosition = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition.Value);
        if (Physics.Raycast(ray, out RaycastHit hitData))
            mousePosition = hitData.point;
        else
            mousePosition = null;

        if (mousePosition.HasValue)
        {
            if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                bool delete = Input.GetMouseButton(1);
                UpdatePoints(mousePosition.Value, delete);
            }
        }

        UpdateMash();
    }

    private void UpdatePoints(Vector3 position, bool delete)
    {
        for(int i = 0; i < activePoints.Length; i++)
        {
            float distance = Vector3.Distance(position, activePoints[i]);
            if (distance <= brush)
                activePoints[i].w +=  delete ? 0.3f : - 0.3f;
        }
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private float CalculateHeight(float x, float y, float offset)
    {
        float xCoord = (float)x / numPointsPerAxis * perlinNoiseScale + offset;
        float yCoord = (float)y / numPointsPerAxis * perlinNoiseScale + offset;

        return Mathf.PerlinNoise(xCoord, yCoord) * perlinNoiseHightScale + baseHight;
    }

    private void GenRandomMap()
    {
        activePoints = new Vector4[numPointsPerAxis * numPointsPerAxis * numPointsPerAxis];
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);
        float offset = new System.Random().Next(1000);
        for(int x = 0; x < numPointsPerAxis; x++)
            for(int y = 0;  y < numPointsPerAxis; y++)
                for(int z = 0; z < numPointsPerAxis; z++)
                {
                    float xPos = x * pointSpacing;
                    float yPos = y * pointSpacing;
                    float zPos = z * pointSpacing;
                    float height = CalculateHeight(x, z, offset);
                    activePoints[x * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + z] = new Vector4(xPos, yPos, zPos, yPos - height);
                }
    }

    private void UpdateMash()
    {
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

        pointsBuffer.SetCounterValue(1);
        pointsBuffer.SetData(activePoints);

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(0, "points", pointsBuffer);
        shader.SetBuffer(0, "triangles", triangleBuffer);
        shader.SetInt("numPointsPerAxis", numPointsPerAxis);
        shader.SetFloat("isoLevel", isoLevel);

        shader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshCollider.sharedMesh = mesh;
    }


    void CreateBuffers()
    {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        ReleaseBuffers();
            
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }
    }

    Mesh InitMesh(ref GameObject obj, ref Material mat)
    {
        Mesh mesh = null;

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = obj.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = obj.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = mat;

        mesh = meshFilter.mesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.mesh;

        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.name = "GenMesh";

        return mesh;
    }
    MeshCollider InitMeshCollider(ref GameObject obj, ref Mesh mesh)
    {

        MeshCollider meshCol = obj.GetComponent<MeshCollider>();
        if (meshCol == null)
        {
            meshCol = obj.AddComponent<MeshCollider>();
        }
        meshCol.sharedMesh = mesh;
        meshCol.convex = false;

        return meshCol;
    }
}
