#ifndef HOLE_NODES
#define HOLE_NODES

struct HoleData
{
    float2 position;
    float radius;
    int active;
};

StructuredBuffer<HoleData> g_HoleBuffer;
int g_HoleCount;



void GetDistanceToHole_float (float2 xz, out float Dst, out float Clip, out float2 holePos)
{
    float mindst = 10000.0f;
    for(int i= 0; i < g_HoleCount; i++)
    {
        if(g_HoleBuffer[i].active == 1)
        {
            float d = length(xz - g_HoleBuffer[i].position) - g_HoleBuffer[i].radius;
            if(d < mindst)
            {
                mindst = d;
                holePos = g_HoleBuffer[i].position;
            }
        }
    }

    Dst = mindst;

    float distanceChange = fwidth(Dst) * 0.5;
    Clip = smoothstep(distanceChange, -distanceChange, Dst);
}


#endif