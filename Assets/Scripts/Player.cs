using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public LineRenderer lr;
    public DistanceJoint2D dj;
    public Camera cam;

    private void Start()
    {
        dj.enabled = false;
    }
    void Update()
    {
        grapple();
    }

    void grapple()
    {
        Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            dj.enabled = true;
            dj.connectedAnchor = mousePos;
            lr.enabled = true;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, mousePos);
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            dj.enabled = false;
            lr.enabled = false;
        }

        if (dj.enabled)
        {
            lr.SetPosition(0, transform.position);
        }
    }
}
