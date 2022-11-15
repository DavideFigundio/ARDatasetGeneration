using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCameraMatrix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start(){
        Matrix4x4 cameraMatrix = GetComponent<Camera>().projectionMatrix;
        Debug.Log(cameraMatrix);
    }
}
