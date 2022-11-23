using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform target;
    void Update()
    {
        transform.position = new Vector3(transform.position.x, 0, -10);
    }
}
