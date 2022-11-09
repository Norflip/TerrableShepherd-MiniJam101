using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Jobs;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public float3 position;
    public float3 normal;
    public float2 uv;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainMesh : MonoBehaviour
{
    public Transform cursor;
    public int resolution = 8;
    public float size;
    
    [Header("form")]
    public float radius;
    public float height;
    public float lowerSmoothTime = 0.5f;
    public bool smooth = true;
    public bool addSkirt = true;
    public float skirtYpos = -2.0f;
    public Transform bottomPlane;

    public AudioSource rumbleSource;

    public MeshCollider MeshCollider => m_meshCollider;
    MeshCollider m_meshCollider;
    Mesh mesh;
    Plane missplane;

    float3 targetHitPoint;
    float3 currentHitPoint;

    NativeArray<float> heightmap;
    NativeArray<float3> vertices;
    NativeArray<float3> normals;
    NativeArray<float> velocities;
    int vertexCount, indexCount;
    int skirtCount;

    public float rumbleDefaultVolume;
    public float targetVolumeT;
    public float currentVolumeT;
    public bool gameover;

    private void Awake() {
        rumbleDefaultVolume = rumbleSource.volume;
        rumbleSource.volume = 0.0f;

        missplane = new Plane(Vector3.up, 0.0f);
        bottomPlane.position = new Vector3(size * 0.5f, skirtYpos, size * 0.5f);
        bottomPlane.localScale = new Vector3(size, size, 1.0f);
        targetHitPoint = currentHitPoint = math.float3(size, 0, size) * 0.5f;
        
        int pointsPerAxis = resolution + 1;
        vertexCount = pointsPerAxis * pointsPerAxis;
        indexCount = (pointsPerAxis - 1) * (pointsPerAxis - 1) * 6;
        skirtCount = ((resolution + 1) * 4);

        int vertexWithSkirt = (addSkirt) ? (vertexCount + skirtCount) : vertexCount;

        vertices = new NativeArray<float3>(vertexWithSkirt, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        normals = new NativeArray<float3>(vertexWithSkirt, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        velocities = new NativeArray<float>(vertexCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        heightmap = new NativeArray<float>((resolution + 1) * (resolution + 1), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < (resolution + 1) * (resolution + 1); i++)
            heightmap[i] = 0.0f;
        
        m_meshCollider = GetComponent<MeshCollider>();
        GenerateBaseMesh(heightmap);
    }

    private void OnEnable() {
        Game.OnGameOver += GameOver;
    }
    
    private void OnDisable() {
        Game.OnGameOver -= GameOver;
    }

    private void OnDestroy() {
        heightmap.Dispose();
        vertices.Dispose();
        normals.Dispose();
        velocities.Dispose();

        Debug.Log("DESTROYED");
    }

    void GameOver ()
    {
        rumbleSource.Stop();
        rumbleSource.volume = 0.0f;
        gameover = true;
    }

    private void Update() {
        if(Time.timeScale == 0.0f)
            return;

        if(!gameover)
        {
            cursor.localScale = new Vector3(radius * 2.0f, radius * 2.0f, 1.0f);

            LowerHeightJob lowerHeightJob = new  LowerHeightJob();
            lowerHeightJob.deltaTime = Time.deltaTime;
            lowerHeightJob.velocities = velocities;
            lowerHeightJob.heightmap = heightmap;
            lowerHeightJob.smoothTime = lowerSmoothTime;
            lowerHeightJob.Schedule((resolution + 1) * (resolution + 1), 64).Complete();

            currentHitPoint = math.lerp(currentHitPoint, targetHitPoint, 10 * Time.deltaTime);

            if(!Input.GetMouseButton(0))
                targetVolumeT = 0.0f;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            missplane.Raycast(ray, out float e);
            Vector3 p = ray.GetPoint(e);
            cursor.position = new Vector3(p.x, 0.1f, p.z);

            if(Input.GetMouseButton(0))
            {
                if(m_meshCollider.Raycast(ray, out RaycastHit hit, 1000.0f))
                {
                        targetHitPoint = hit.point;//C2W(W2C(hit.point), hit.point.y);
                }
                else
                {
                        targetHitPoint = ray.GetPoint(e);
                }

                targetVolumeT = 1.0f;
                Form(currentHitPoint);
            }
            

            currentVolumeT = Mathf.Lerp(currentVolumeT, targetVolumeT, 4.0f * Time.deltaTime);
            rumbleSource.volume = rumbleDefaultVolume * currentVolumeT;
        }
    }

    private void LateUpdate() {
        
        UpdateMeshJob updateMeshJob = new UpdateMeshJob();
        updateMeshJob.heightmap = heightmap;
        updateMeshJob.vertices = vertices;
        updateMeshJob.normals = normals;
        updateMeshJob.Schedule(vertexCount, 64).Complete();

        mesh.SetVertices(vertices);
        Physics.BakeMesh(mesh.GetInstanceID(), false);
        m_meshCollider.sharedMesh = null;
        m_meshCollider.sharedMesh = mesh;
    }

    int2 vmin, vmax;

    public void Form (float3 point)
    {        
        // run job
        ModHeightJob heightJob = new ModHeightJob();
        heightJob.heightmap = heightmap;
        heightJob.velocities = velocities;
        heightJob.radius = radius;
        heightJob.point = point;
        heightJob.map_resolution = resolution + 1;
        heightJob.map_size = size;
        heightJob.height = height;
        
        int2 min = W2C(point - math.float3(radius));
        int2 max = W2C(point + math.float3(radius));
        vmin = min;
        vmax = max;

        int2 vsize = max - min;
        heightJob.area_min = min;
        heightJob.area_width = vsize.x;    
        JobHandle h = heightJob.Schedule(vsize.x * vsize.y, 64);

        if(smooth)
        {
            SmoothHeightJob heightJob1 = new SmoothHeightJob();
            heightJob1.area_max = max;
            heightJob1.area_min = min;
            heightJob1.heightmap = heightmap;
            heightJob1.map_resolution = resolution + 1;
            heightJob1.Schedule(h).Complete();
        }
        else
        {
            h.Complete();
        }
    }

    public int2 W2C (float3 p)
    {
        return WorldToCoord(p, resolution+1.0f,size);
    }

    public float3 C2W (int2 coord, float y= 0.0f)
    {
        return CoordToWorld(coord, resolution+1.0f, size, y);
    }

    public static int2 WorldToCoord (float3 p, float resolution, float size)
    {
        float2 xz = p.xz;
        xz = math.round((xz / size) * resolution);
        return math.int2(xz);
    }

    public static float3 CoordToWorld (int2 coord, float resolution, float size, float y = 0.0f)
    {
        float3 p = (math.float3(coord.x,0,coord.y) / resolution) * size;
        p.y = y;
        return p;
    }

    public void GenerateBaseMesh (NativeArray<float> heightmap)
    {
        if(mesh == null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh = new Mesh();
            mesh.MarkDynamic();
            float3 boundsSize = math.float3(size);
            mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
        }

        NativeArray<int> indicies = new NativeArray<int>(indexCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

        int pointsPerAxis = resolution + 1;
        for (int z = 0; z < pointsPerAxis; z++)
        {
            for (int x = 0; x < pointsPerAxis; x++)
            {
                float3 position = (math.float3(x,0,z) / resolution) * size;
                position.y = heightmap[z * pointsPerAxis + x];

                int vi = (z * pointsPerAxis + x);
                vertices[vi] = position;
                normals[vi] = math.float3(0,1,0);

                if(x < pointsPerAxis - 1 && z < pointsPerAxis - 1)
                {
                    int ti = (z * (pointsPerAxis - 1) + x) * 6;
                    indicies[ti + 0] = (short)(vi + pointsPerAxis);
                    indicies[ti + 1] = (short)(vi + pointsPerAxis + 1);
                    indicies[ti + 2] = (short)(vi + 1);
                    indicies[ti + 3] = (short)(vi + pointsPerAxis);
                    indicies[ti + 4] = (short)(vi + 1);
                    indicies[ti + 5] = (short)(vi);
                }
            }
        }

        NativeArray<int> skirtIndicies = new NativeArray<int>((resolution * 4) * 6, Allocator.Temp, NativeArrayOptions.ClearMemory);

        int tiOffset = 0;
        int viOffset = (resolution * pointsPerAxis + resolution)+1;
        
        //back
        for (int x = 0; x < pointsPerAxis; x++)
        {
            int i1 = x;
            Vector3 offsetPoint = vertices[i1];
            offsetPoint.y = skirtYpos;
            vertices[viOffset] = offsetPoint;

            if(x < resolution)
            {
                skirtIndicies[tiOffset + 0] = i1;
                skirtIndicies[tiOffset + 1] = i1 + 1;
                skirtIndicies[tiOffset + 2] = viOffset + 1;
                
                skirtIndicies[tiOffset + 3] = i1;
                skirtIndicies[tiOffset + 4] = viOffset + 1;
                skirtIndicies[tiOffset + 5] = viOffset; 
                tiOffset += 6;
            }

            viOffset++;
        }

        //forward
        for (int x = 0; x < resolution + 1; x++)
        {
            int i1 = (resolution) * (resolution + 1) + x;
            Vector3 offsetPoint = vertices[i1];
            offsetPoint.y = skirtYpos;
            vertices[viOffset] = offsetPoint;

            if(x < resolution)
            {
                skirtIndicies[tiOffset + 2] = i1;
                skirtIndicies[tiOffset + 1] = i1 + 1;
                skirtIndicies[tiOffset + 0] = viOffset + 1;

                // fungerar
                skirtIndicies[tiOffset + 5] = i1;
                skirtIndicies[tiOffset + 4] = viOffset + 1;
                skirtIndicies[tiOffset + 3] = viOffset; 
                tiOffset += 6;
            }

            viOffset++;
        }

        // left
        for (int y = 0; y < resolution + 1; y++)
        {
            int i1 = y * (resolution + 1);
            Vector3 offsetPoint = vertices[i1];
            offsetPoint.y = skirtYpos;
            vertices[viOffset] = offsetPoint;

            if(y < resolution)
            {
                skirtIndicies[tiOffset + 2] = i1;
                skirtIndicies[tiOffset + 1] = i1 + pointsPerAxis;
                skirtIndicies[tiOffset + 0] = viOffset + 1;

                skirtIndicies[tiOffset + 5] = i1;
                skirtIndicies[tiOffset + 4] = viOffset + 1;
                skirtIndicies[tiOffset + 3] = viOffset; 
                tiOffset += 6;
            }
            viOffset++;
        }

        //right
        for (int y = 0; y < resolution + 1; y++)
        {
            int i1 = y * (resolution + 1) + resolution;
            Vector3 offsetPoint = vertices[i1];
            offsetPoint.y = skirtYpos;
            vertices[viOffset] = offsetPoint;

            if(y < resolution)
            {
                skirtIndicies[tiOffset + 0] = i1;
                skirtIndicies[tiOffset + 1] = i1 + pointsPerAxis;
                skirtIndicies[tiOffset + 2] = viOffset + 1;
                skirtIndicies[tiOffset + 3] = i1;
                skirtIndicies[tiOffset + 4] = viOffset + 1;
                skirtIndicies[tiOffset + 5] = viOffset; 
                tiOffset += 6;
            }
            viOffset++;
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);


        mesh.subMeshCount = addSkirt ? 2 : 1; // set 2 for skirt
        mesh.SetIndices(indicies, MeshTopology.Triangles, 0);

        if(addSkirt)
        {
            mesh.SetIndices(skirtIndicies, MeshTopology.Triangles, 1);
            skirtIndicies.Dispose();
        }

        indicies.Dispose();

        //Physics.BakeMesh(mesh.GetInstanceID(), false);
        m_meshCollider.sharedMesh = null;
        m_meshCollider.sharedMesh = mesh;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentHitPoint, radius);   
        Gizmos.DrawSphere(currentHitPoint, 0.1f);   

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(C2W(vmin), 0.1f);
        Gizmos.DrawSphere(C2W(vmax), 0.1f);
        
        float3 bsize = math.float3(size, 0.0f, size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(math.float3(transform.position) + bsize * 0.5f, bsize);
    }
}
