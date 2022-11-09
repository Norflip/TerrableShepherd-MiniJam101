using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct ModHeightJob : IJobParallelFor
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float> heightmap;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float> velocities;
    
    public float3 point;
    public float radius;
    public float height;
    
    public int2 area_min;
    public int area_width;

    public int map_resolution;
    public float map_size;

    public void Execute(int index)
    {
        int x = index % area_width;
        int y = index / area_width;
        int2 coord = area_min + math.int2(x,y);

        if(coord.x < 0 || coord.y < 0 || coord.x >= map_resolution || coord.y >= map_resolution)
            return;

        int i = coord.y * map_resolution + coord.x;
        float3 offset = TerrainMesh.CoordToWorld(coord, map_resolution, map_size) - point;
        float sqrDst = offset.x * offset.x + offset.z * offset.z;

        if (sqrDst <= radius * radius) {

            float sphereH = radius - 0.5f;
            float dy = (math.max(0, math.sqrt((sphereH*sphereH) - math.lengthsq(offset))) / radius) * height;

            if(float.IsInfinity(dy) || float.IsNaN(dy))
                dy = 0.0f;

            if(dy > heightmap[i])
                velocities[i] = 0.0f;
            heightmap[i] = math.max(heightmap[i], dy);
        }
    }
}
