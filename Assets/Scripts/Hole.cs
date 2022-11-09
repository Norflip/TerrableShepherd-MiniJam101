using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    public float speed;
    public Vector3 currentDirection;
    public bool paused = false;
    public LayerMask walls;
    public float worldSize;

    public float minScale = 1.0f;
    public float maxScale = 2.0f;
    public float pulseSpeed = 2.0f;

    Vector3 targetPosition;

    private void Awake() {
        targetPosition = new Vector3(Random.Range(1.0f, worldSize - 1.0f), 0.0f, Random.Range(1.0f, worldSize - 1.0f));
    }

    void Update ()
    {
        if(paused)
            return;

        float s = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(Time.time * pulseSpeed, 1.0f));
        transform.localScale = new Vector3(s, transform.localScale.y, s);

        Vector3 dir = (targetPosition - transform.position);
        dir.y = 0.0f;
        
        float dst = dir.x * dir.x + dir.z * dir.z;
        dir.Normalize();

        if(dst < 0.4f)
            targetPosition = new Vector3(Random.Range(1.0f, worldSize - 1.0f), 0.0f, Random.Range(1.0f, worldSize - 1.0f));
        else
            transform.position += dir * speed * Time.deltaTime;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x);
    }
}
