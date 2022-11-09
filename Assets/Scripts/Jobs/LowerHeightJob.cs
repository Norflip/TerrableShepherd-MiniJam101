using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct LowerHeightJob : IJobParallelFor
{
    public NativeArray<float> heightmap;
    public NativeArray<float> velocities;
    public float smoothTime;
    public float deltaTime;

    public void Execute(int index)
    {
        float output = 0.0f;
        velocities[index] = SmoothDamp(heightmap[index], 0.0f, velocities[index], smoothTime, 1000.0f, deltaTime, out output);
        heightmap[index] = output;
    }

    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Mathf.cs
    // Gradually changes a value towards a desired goal over time.
    float SmoothDamp(float current, float target, float currentVelocity, float smoothTime, float maxSpeed, float deltaTime, out float output)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = math.max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
        float change = current - target;
        float originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = math.clamp(change, -maxChange, maxChange);
        target = current - change;

        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        output = target + (change + temp) * exp;

        // Prevent overshooting
        if (originalTo - current > 0.0F == output > originalTo)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

        return currentVelocity;
    }
}