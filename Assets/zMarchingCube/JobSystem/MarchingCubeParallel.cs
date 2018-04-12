using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using System;

public class MarchingCubeParallel
{
    public static float Surface { get; set; }

    /// <summary>
    /// Winding order of triangles use 2,1,0 or 0,1,2
    /// </summary>
    protected static int[] WindingOrder { get; private set; }

    //private static Vector3[] EdgeVertex { get; set; }

    private static int currentVertIndex;

    private static float[] m_voxels;

    public static Vector3[] m_verts;

    public static int[] m_indices;

    public MarchingCubeParallel(float surface = 0.5f)
    {
        Surface = surface;
        WindingOrder = new int[] { 0, 1, 2 };
        //EdgeVertex = new Vector3[12];
    }

    public struct MarchJob : IJobParallelFor
    {
        public int w, h, d;
        public int ix, iy, iz;
        //public NativeArray<Vector3> m_verts;
        //public NativeArray<int> m_indices;        
        public void Execute(int index)
        {
            int x = index / (h * d);
            int y = (index - x * h * d) / d;
            int z = (index - x * h * d) % h;

            float[] Cube = new float[8];
            for (int ii = 0; ii < 8; ii++)
            {
                ix = x + Marching.VertexOffset[ii, 0];
                iy = y + Marching.VertexOffset[ii, 1];
                iz = z + Marching.VertexOffset[ii, 2];
                Cube[ii] = m_voxels[ix + iy * w + iz * w * h];
            }

            //perform algorithm
            int i, j, vert, idx;
            int flagIndex = 0;
            float offset = 0f;
            NativeArray<Vector3> EdgeVertex = new NativeArray<Vector3>(12, Allocator.TempJob);

            for (i = 0; i < 8; i++)
            {
                //Debug.Log("Cube[i] " + Cube[i] + " Surface " + Surface);
                if (Cube[i] <= Surface)
                    flagIndex |= 1 << i;
            }
            //Debug.Log("b4 intersection : " + flagIndex);
            int edgeFlags = MarchingCube.CubeEdgeFlags[flagIndex];
            if (edgeFlags == 0) return;

            //Debug.Log("Got intersection");
            for (i = 0; i < 12; i++)
            {
                //intersection happens
                if((edgeFlags & (1 << i)) != 0)
                {
                    offset = Marching.GetOffset(Cube[MarchingCube.EdgeConnection[i, 0]], Cube[MarchingCube.EdgeConnection[i, 1]]);

                    EdgeVertex[i] = new Vector3(x + (Marching.VertexOffset[MarchingCube.EdgeConnection[i, 0], 0]
                        + offset * MarchingCube.EdgeDirection[i, 0]),
                    y + (Marching.VertexOffset[MarchingCube.EdgeConnection[i, 0], 1]
                        + offset * MarchingCube.EdgeDirection[i, 1]),
                    z + (Marching.VertexOffset[MarchingCube.EdgeConnection[i, 0], 2]
                        + offset * MarchingCube.EdgeDirection[i, 2]));
                }
            }

            for (i = 0; i < 5; i++)
            {
                if (MarchingCube.TriangleConnectionTable[flagIndex, 3 * i] < 0) break;

                //idx = x * w * d + y * d  + z;
                for ( j = 0; j < 3; j++)
                {
                    vert = MarchingCube.TriangleConnectionTable[flagIndex, 3 * i + j];
                    m_indices[currentVertIndex] = currentVertIndex + WindingOrder[j];
                    m_verts[currentVertIndex++] = EdgeVertex[vert];
                   // Debug.Log("zzz " + currentVertIndex);
                }
            }

            EdgeVertex.Dispose();
        }
    }

    private MarchJob marchJob;
    public JobHandle Generate(List<float> voxels, int width, int height, int depth, out MarchJob job)
    {
        if (Surface > 0.0f)
        {
            WindingOrder[0] = 0;
            WindingOrder[1] = 1;
            WindingOrder[2] = 2;
        }
        else
        {
            WindingOrder[0] = 2;
            WindingOrder[1] = 1;
            WindingOrder[2] = 0;
        }

        int maxLength = width * height * depth * 15;
        currentVertIndex = 0;
        m_voxels = voxels.ToArray();
        m_verts = new Vector3[maxLength];
        m_indices = new int[maxLength];

        marchJob = new MarchJob()
        {
            w = width - 1,
            h = height - 1,
            d = depth - 1          
            //m_verts = new NativeArray<Vector3>(maxLength, Allocator.Temp),
            //m_indices = new NativeArray<int>(maxLength, Allocator.Temp)
        };

        var handle = marchJob.Schedule(width * height * depth, 64);
        job = marchJob;
        return handle;       
    }
}
