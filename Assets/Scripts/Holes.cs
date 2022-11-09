using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Holes : MonoBehaviour
{
    public struct HoleData
    {
        public float2 position;
        public float radius;
        public int active;
        public const int Stride = sizeof(float) * 3 + sizeof(int);
    }

    ComputeBuffer holeBuffer;

    void Update ()
    {
        

        Hole[] holes = FindObjectsOfType<Hole>();
        if(holes == null || holes.Length == 0)
            return;

 
        List<HoleData> data = new List<HoleData>();
        for (int i = 0; i < holes.Length; i++)
        {
            HoleData d = new HoleData();
            d.position = math.float2(holes[i].transform.position.x, holes[i].transform.position.z);
            d.radius = holes[i].transform.localScale.x * 0.5f;
            d.active = holes[i].gameObject.activeInHierarchy ? 1 : 0;
            data.Add(d);
        }

        if(holeBuffer == null || holeBuffer.count != holes.Length)
        {
            if(holeBuffer != null)
                holeBuffer.Release();
            holeBuffer = new ComputeBuffer(holes.Length, HoleData.Stride);
        }


        holeBuffer.SetData(data);
        Shader.SetGlobalBuffer("g_HoleBuffer", holeBuffer);
        Shader.SetGlobalInt("g_HoleCount", holes.Length);
    }

}
