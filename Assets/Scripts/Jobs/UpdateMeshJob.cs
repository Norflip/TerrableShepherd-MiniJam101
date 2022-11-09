using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;

[BurstCompile]
public struct UpdateMeshJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> heightmap;

    public NativeArray<float3> vertices;
    public NativeArray<float3> normals;

    public void Execute(int index)
    {
        float3 p = vertices[index];
        p.y = heightmap[index];
        vertices[index] = p;
    }
}
