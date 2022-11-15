using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetProjectionMatrix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var cam = GetComponent<Camera>();
        Debug.Log(cam.projectionMatrix);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
