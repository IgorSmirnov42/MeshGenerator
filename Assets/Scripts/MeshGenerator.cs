using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();
    
    private MeshFilter _filter;
    private Mesh _mesh;

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();
        
        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();
        
        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }

    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    private void Update()
    {
        vertices.Clear();
        indices.Clear();
        normals.Clear();

        Field.Update();
        
        float cubeSideLength = (float) 0.15;

        var borders = Field.Borders();
        print(borders.MinX + " " + borders.MaxX + " " + borders.MinY + " " + borders.MaxY + " " + borders.MinZ + " " + borders.MaxZ);
        int xCounter = 0;
        while (borders.MinX + xCounter * cubeSideLength < borders.MaxX)
        {
            int yCounter = 0;
            while (borders.MinY + yCounter * cubeSideLength < borders.MaxY)
            {
                int zCounter = 0;
                while (borders.MinZ + zCounter * cubeSideLength < borders.MaxZ)
                {
                    ProcessCube(borders.MinX + xCounter * cubeSideLength, 
                        borders.MinY + yCounter * cubeSideLength, 
                        borders.MinZ + zCounter * cubeSideLength, cubeSideLength);
                    ++zCounter;
                }
                ++yCounter;
            }
            ++xCounter;
        }
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        _mesh.SetNormals(normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }

    private void ProcessCube(float minX, float minY, float minZ, float cubeSideLength)
    {
        float maxX = minX + cubeSideLength;
        float maxY = minY + cubeSideLength;
        float maxZ = minZ + cubeSideLength;

        var cubeVertices = new List<Vector3>
        {
            new Vector3(minX, minY, minZ), // 0
            new Vector3(minX, maxY, minZ), // 1
            new Vector3(maxX, maxY, minZ), // 2
            new Vector3(maxX, minY, minZ), // 3
            new Vector3(minX, minY, maxZ), // 4
            new Vector3(minX, maxY, maxZ), // 5
            new Vector3(maxX, maxY, maxZ), // 6
            new Vector3(maxX, minY, maxZ), // 7
        };

        var fValues = cubeVertices.Select(vertex => Field.F(vertex)).ToList();
        int mask = 0;
        for (int i = 0; i < 8; ++i)
        {
            if (fValues[i] > 0)
            {
                mask |= 1 << i;
            }
        }
        
        if (mask == 0 || mask == 255) return;

        var edgeCenters = new List<Vector3>();

        foreach (var edge in MarchingCubes.Tables._cubeEdges)
        {
            var vertex1 = cubeVertices[edge[0]];
            var vertex2 = cubeVertices[edge[1]];
            var f1 = fValues[edge[0]];
            var f2 = fValues[edge[1]];
            if (f1 > 0 && f2 < 0 || f1 < 0 && f2 > 0)
            {
                float p = f1 / (f1 - f2);
                edgeCenters.Add(vertex1 * (1 - p) + vertex2 * p);
            }
            else
            {
                edgeCenters.Add(vertex1);
            }
        }

        float eps = (float) 0.01;
        
        var dx = new Vector3(eps, 0, 0);
        var dy = new Vector3(0, eps, 0);
        var dz = new Vector3(0, 0, eps);

        var norms = edgeCenters.Select(p => Vector3.Normalize(new Vector3(
            Field.F(p + dx) - Field.F(p - dx),
            Field.F(p + dy) - Field.F(p - dy),
            Field.F(p + dz) - Field.F(p - dz)
        ))).ToList();

        foreach (var triangle in MarchingCubes.Tables.CaseToVertices[mask])
        {
            if (triangle.x == -1) continue;
            indices.Add(vertices.Count);
            vertices.Add(edgeCenters[triangle.x]);
            normals.Add(norms[triangle.x]);
            indices.Add(vertices.Count);
            vertices.Add(edgeCenters[triangle.y]);
            normals.Add(norms[triangle.y]);
            indices.Add(vertices.Count);
            vertices.Add(edgeCenters[triangle.z]);
            normals.Add(norms[triangle.z]);
        }
    }
}