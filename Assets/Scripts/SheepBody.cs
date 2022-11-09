using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepBody : MonoBehaviour
{
    public LayerMask terrainLayer;
    public float extraHeight = 5.0f;
    public float rayLength = 1.0f;
    
    float radius;
    Rigidbody body;
    float dstToTerrainLastFrame;
    bool firstFrame;

    public Vector3 normal;
    public bool isDead;
    public bool isGrounded;

    private void Awake() {
        isDead = false;
        firstFrame = true;
        body = GetComponent<Rigidbody>();
        radius = GetComponent<SphereCollider>().radius * transform.localScale.x;    
    }

    private void OnTriggerStay(Collider other) {
        if(isDead)
            return;

        Hole hole = other.GetComponent<Hole>();
        if(hole != null && isGrounded)
        {
            Kill();
        }
    }

    void Kill ()
    {
        isGrounded = false;
        isDead = true;
        GetComponent<Collider>().enabled = false;    
        Game.Instance.KillSheep(GetComponentInParent<Sheep>());
    }

    private void FixedUpdate() 
    {
        if(isDead)
            return;

        if(transform.position.y < -5.0f)
        {
            Kill();
            return;
        }    

        float distToTerrain = 0.0f;
        
        if(Physics.SphereCast(transform.position + Vector3.up * extraHeight, radius * 0.5f, Vector3.down, out RaycastHit hit, rayLength + extraHeight, terrainLayer.value))
        {
            normal = hit.normal;
            Debug.DrawLine(transform.position + Vector3.up * extraHeight, transform.position + Vector3.down * (rayLength+extraHeight));
            distToTerrain = (transform.position.y - hit.point.y);

            if(dstToTerrainLastFrame - distToTerrain > radius)
            {
                float t = (dstToTerrainLastFrame - distToTerrain) / (radius*2.0f);
                body.AddForce(Vector3.up * 15 * t + hit.normal * 25 * t + Vector3.ProjectOnPlane(hit.normal, Vector3.up) * 15 * t);
            }

            Vector3 d = (hit.point - transform.position);
            if(d.sqrMagnitude < radius * radius || hit.point.y > transform.position.y)
            {
                body.MovePosition(hit.point + Vector3.up * radius);
            }
        }

        isGrounded = Physics.Raycast(transform.position, Vector3.down, radius + 0.01f, terrainLayer.value);
        dstToTerrainLastFrame = distToTerrain;
        firstFrame = false;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
