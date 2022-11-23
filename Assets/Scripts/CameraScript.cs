using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform target;
    void Update()
    {
        var position = transform.position;
        position.x = target.transform.position.x;
        transform.position = position;
    }
}
