using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class Generator : MonoBehaviour {

    public Material m_material;
    public int sizeScale = 1;
    public int seed = 0;
    public bool useJobSystem;
    List<GameObject> meshes = new List<GameObject>();

    List<Vector3> verts;
    List<int> indices;
    int width  ;
    int height ;
    int length;
    void Generate() {

        DateTime startTime = System.DateTime.Now;

        INoise perlin = new PerlinNoise(seed, 2f);
        FractalNoise fractal = new FractalNoise(perlin, 3, 1f);

        MarchingCube marching = new MarchingCube();
        Marching.Surface = 0f;

        width =  sizeScale;
        height =sizeScale;
        length = sizeScale;

        float[] voxels = new float[width * height * length];

        //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    float fx = x / (width - 1.0f);
                    float fy = y / (height - 1.0f);
                    float fz = z / (length - 1.0f);

                    int idx = x + y * width + z * width * height;

                    voxels[idx] = fractal.Sample3D(fx, fy, fz);
                }
            }
        }

        verts = new List<Vector3>();
        indices = new List<int>();

        
        if(useJobSystem)
        {
            MarchingCubeParallel marching_p = new MarchingCubeParallel(0);
            MarchingCubeParallel.MarchJob job;
            JobHandle handle = marching_p.Generate(new List<float>(voxels), width, height, length, out job);
            StartCoroutine(Wait4Complete(handle, job));            
        }   
        else 
        {
            marching.Generate(voxels, width, height, length, verts, indices);
            int maxVertsPerMesh = 30000; //must be divisible by 3, ie 3 verts == 1 triangle
            int numMeshes = verts.Count / maxVertsPerMesh + 1;
            for (int i = 0; i < numMeshes; i++)
            {

                List<Vector3> splitVerts = new List<Vector3>();
                List<int> splitIndices = new List<int>();

                for (int j = 0; j < maxVertsPerMesh; j++)
                {
                    int idx = i * maxVertsPerMesh + j;

                    if (idx < verts.Count)
                    {
                        splitVerts.Add(verts[idx]);
                        splitIndices.Add(j);
                    }
                }

                if (splitVerts.Count == 0) continue;

                Mesh mesh = new Mesh();
                mesh.SetVertices(splitVerts);
                mesh.SetTriangles(splitIndices, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                GameObject go = new GameObject("Mesh");
                go.transform.parent = transform;
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                go.GetComponent<Renderer>().material = m_material;
                go.GetComponent<MeshFilter>().mesh = mesh;
                go.transform.localPosition = new Vector3(-width / 2, -height / 2, -length / 2);

                meshes.Add(go);
            }
        
        }
        double timeCost = System.DateTime.Now.Subtract(startTime).TotalMilliseconds;
        print("Time Cost : " + timeCost + " at size " + (8 * sizeScale));
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.G))
            Generate();

    }

    IEnumerator Wait4Complete(JobHandle handle, MarchingCubeParallel.MarchJob job)
    {
        while (!handle.IsCompleted)
            yield return null;

        handle.Complete();
        verts = new List<Vector3>(MarchingCubeParallel.m_verts);
        print("vert length " + verts.Count);
        indices = new List<int>(MarchingCubeParallel.m_indices);
        print("indices length " + indices.Count);
        //if (job.m_indices.IsCreated)
        //    job.m_indices.Dispose();
        //if (job.m_verts.IsCreated)
        //    job.m_verts.Dispose();

        int maxVertsPerMesh = 60000; //must be divisible by 3, ie 3 verts == 1 triangle
        int numMeshes = verts.Count / maxVertsPerMesh + 1;
        for (int i = 0; i < numMeshes; i++)
        {

            List<Vector3> splitVerts = new List<Vector3>();
            List<int> splitIndices = new List<int>();

            for (int j = 0; j < maxVertsPerMesh; j++)
            {
                int idx = i * maxVertsPerMesh + j;

                if (idx < verts.Count)
                {
                    splitVerts.Add(verts[idx]);
                    splitIndices.Add(j);
                }
            }

            if (splitVerts.Count == 0) continue;

            Mesh mesh = new Mesh();
            mesh.SetVertices(splitVerts);
            mesh.SetTriangles(splitIndices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            GameObject go = new GameObject("Mesh");
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.GetComponent<Renderer>().material = m_material;
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.transform.localPosition = new Vector3(-width / 2, -height / 2, -length / 2);

            meshes.Add(go);
        }
    }
}
