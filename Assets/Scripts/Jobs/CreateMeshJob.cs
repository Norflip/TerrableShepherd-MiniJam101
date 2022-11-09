using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

public struct CreateMeshJob : IJobParallelFor
{
    [ReadOnly] 
    public NativeArray<float> heightmap;

    [WriteOnly] 
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Vertex> vertices;

    [WriteOnly] 
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<int> indicies;

    public int resolution;
    public float size;

    public void Execute(int index)
    {
        int pointsPerAxis = resolution + 1;
        int x = index % pointsPerAxis;
        int z = index / pointsPerAxis;

        Vertex v = new Vertex();
        v.position = (math.float3(x,0,z) / resolution) * size;
        v.position.y = heightmap[z * pointsPerAxis + x];

        v.normal = math.float3(0,1,0);
        v.uv = math.float2((float)x / (pointsPerAxis + 1), (float)z / (pointsPerAxis + 1));
        
        int vi = (z * pointsPerAxis + x);
        vertices[index] = v;

        if(x < pointsPerAxis - 1 && z < pointsPerAxis - 1)
        {
            int ti = (z * (pointsPerAxis - 1) + x) * 6;
            Debug.Log(ti);
            indicies[ti + 0] = (int)(vi + pointsPerAxis);
            indicies[ti + 1] = (int)(vi + pointsPerAxis + 1);
            indicies[ti + 2] = (int)(vi + 1);
            indicies[ti + 3] = (int)(vi + pointsPerAxis);
            indicies[ti + 4] = (int)(vi + 1);
            indicies[ti + 5] = (int)(vi);
        }
    }
}
