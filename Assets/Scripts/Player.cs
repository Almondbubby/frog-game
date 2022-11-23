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
        Debug.DrawRay(transform.position, mousePos-(Vector2)transform.position);
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position,mousePos-(Vector2)transform.position);
            RaycastHit2D check = Physics2D.Raycast(mousePos, Vector2.zero);
            bool good = true;
            if (hit.collider != null) Debug.Log(hit.collider.gameObject.layer);
            if(hit.collider != null && hit.collider.gameObject.layer != 3)
            {
                good = false;
            }

            if (hit.collider != null && hit.collider.gameObject.layer == 3 && good && check.collider != null)
            {
                dj.enabled = true;
                dj.connectedAnchor = mousePos;
                lr.enabled = true;
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, mousePos);
            }

       
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
