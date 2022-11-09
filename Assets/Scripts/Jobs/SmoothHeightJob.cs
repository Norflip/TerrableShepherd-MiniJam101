using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct SmoothHeightJob : IJob
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float> heightmap;
    
    public int2 area_min;
    public int2 area_max;
    public int map_resolution;

    public void Execute()
    {
        NativeArray<float> tmp = new NativeArray<float>(heightmap.Length, Allocator.Temp);
        NativeArray<float>.Copy(heightmap, tmp);

        area_min += 1;
        area_max -= 1;
        
        int2 size = area_max - area_min;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int2 coord = area_min + math.int2(x,y);
                if(InsideBounds(coord))
                {
                    int radius = 1;
                    float sum = 0.0f;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int2 dcoord = ClampToBounds(coord + math.int2(dx,dy));
                            int i = dcoord.y * map_resolution + dcoord.x;
                            sum += heightmap[i];
                        }
                    }

                    float r = (radius * 2 + 1) * (radius * 2 + 1);
                    heightmap[coord.y * map_resolution + coord.x] = sum / r;
                }
            }
        }    
    }

    bool InsideBounds (int2 c)
    {
        return (c.x >= 0 && c.y >= 0) && (c.x < map_resolution && c.y < map_resolution);
    }

    int2 ClampToBounds (int2 c)
    {
        c.x = math.clamp(c.x, 0, map_resolution-1);
        c.y = math.clamp(c.y, 0, map_resolution-1);
        return c;
    }
}
