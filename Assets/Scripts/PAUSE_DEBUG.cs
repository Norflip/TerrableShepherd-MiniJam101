using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PAUSE_DEBUG : MonoBehaviour
{
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            Debug.Break();
        }
    }
}
