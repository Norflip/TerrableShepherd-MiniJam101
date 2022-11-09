using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep : MonoBehaviour
{
    public float modelOffset;
    public Transform model;
    public Rigidbody body;

    [Header("legs")]
    public float legLength = 0.4f;
    public float legsSpread;
    public Vector3 legsOffset;
    public LineRenderer[] legs;

    SheepBody sheep;

    private void Awake() {
        sheep = body.GetComponent<SheepBody>();
        // create to be dynamic
        PlaceLegs();
    }
    
    public void Spawn (Vector3 p)
    {
        transform.position = Vector3.zero;
        body.transform.position = p;
    }

    void Update ()
    {
        model.transform.localPosition = body.transform.localPosition;// + normal.up * modelOffset;

        // yesus fock
        if(body.velocity.sqrMagnitude > Mathf.Epsilon && body.velocity != Vector3.zero)
        {
            model.transform.localRotation = Quaternion.LookRotation(body.velocity.normalized);
        }
        else
        {
            model.transform.localRotation = Quaternion.identity;
        }
    }

    [ContextMenu("Place legs")]
    void PlaceLegs ()
    {
        if(legs == null || legs.Length != 4)
        {
            Debug.LogWarning("A sheep has four legs.");
            return;
        }

        legs[0].transform.localPosition = (-Vector3.right * legsSpread + Vector3.forward * legsSpread) + legsOffset;
        legs[1].transform.localPosition = (Vector3.right * legsSpread + Vector3.forward * legsSpread) + legsOffset;
        legs[2].transform.localPosition = (-Vector3.right * legsSpread - Vector3.forward * legsSpread) + legsOffset;
        legs[3].transform.localPosition = (Vector3.right * legsSpread - Vector3.forward * legsSpread) + legsOffset;
    
        for (int i = 0; i < 4; i++)
        {
            legs[i].positionCount = 2;
            legs[i].SetPosition(0, Vector3.zero);
            legs[i].SetPosition(1, Vector3.down * legLength);
        }
    }
}
