using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public LineRenderer lr;
    public DistanceJoint2D dj;
    public Camera cam;
    public float moveSpeed = 200.0f;
    private bool go = false; //whether or not to move closer to mouse (grapple)


    //movement
    public float xAcceleration, xTopSpeed, jumpVelocity; //time unit is seconds
    public float reelInGrappleSpeed;
    private float vx, vy, ax, ay; // vx/vy is velocity and ax/ay is acceleration
    private int xInput, yInput;



    //state management
    public enum states {
        grounded,
        airborne,
        grappling,
        
    };
    private enum actions {
        groundMove,
        airMove,
        jump,
        grapple,
        exitGrapple,
        reelInGrapple,
    }
    private actions curAction; private states curState;
    private Dictionary<Enum, List<Enum>> stateMap = new Dictionary<Enum, List<Enum>> {
        {states.grounded, new List<Enum>() {actions.groundMove, actions.jump, actions.grapple}},
        {states.airborne, new List<Enum>() {actions.airMove, actions.grapple}},
        {states.grappling, new List<Enum>() {actions.exitGrapple}}
    };

    bool canDoAction(Enum action) {
        return stateMap[curState].Contains(action);
    }



    private void Start()
    {
        // dj.enabled = false;
    }
    void FixedUpdate()
    {
        //swing();
        grapple();

        if (Input.GetKey(KeyCode.Space)) {
            reelInGrapple();
        }
    }

    void reelInGrapple() {
        dj.distance -= reelInGrappleSpeed;
    }

    void grapple() {
        Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKey(KeyCode.Mouse0)) {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.Normalize(mousePos - (Vector2)transform.position));
            lr.SetPositions(new Vector3[] {transform.position, dj.connectedBody.transform.position});
            if (!hit.rigidbody) return;
            dj.connectedBody = hit.rigidbody;
        }

        else release();

    }

    void release() {
        lr.SetPositions(new Vector3[] {transform.position, transform.position});
        dj.connectedBody = rb;
    }


    void swing()
    {
        Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //check if theres anything in the way
            RaycastHit2D hit = Physics2D.Raycast(transform.position,mousePos-(Vector2)transform.position);

            //check whether mouse is actually on something thats grapplable
            RaycastHit2D check = Physics2D.Raycast(mousePos, Vector2.zero);
            bool good = true;
            if(hit.collider != null && hit.collider.gameObject.layer != 3)
            {
                good = false;
            }

            if (hit.collider != null && hit.collider.gameObject.layer == 3 && good && check.collider != null)
            {
                dj.enabled = true;
                dj.connectedAnchor = mousePos;
                lr.enabled = true;

                //draw line
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
            //update line
            lr.SetPosition(0, transform.position);
        }
    }
    // void grapple()
    // {
    //     Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
    //     if (Input.GetKeyDown(KeyCode.Mouse1))
    //     {
    //         RaycastHit2D hit = Physics2D.Raycast(transform.position, mousePos - (Vector2)transform.position);
    //         RaycastHit2D check = Physics2D.Raycast(mousePos, Vector2.zero);
    //         bool good = true;
    //         if (hit.collider != null && hit.collider.gameObject.layer != 3)
    //         {
    //             good = false;
    //         }

    //         if (hit.collider != null && hit.collider.gameObject.layer == 3 && good && check.collider != null)
    //         {
    //             go = true;
    //             lr.enabled = true;
    //             lr.SetPosition(0, transform.position);
    //             lr.SetPosition(1, mousePos);
    //         }
    //     }
    //     if (Input.GetKeyUp(KeyCode.Mouse1))
    //     {
    //         lr.enabled = false;
    //         go = false;
    //     }

    //     if (go)
    //     {
    //         lr.SetPosition(0, transform.position);
    //         transform.position = Vector2.MoveTowards(transform.position, mousePos, moveSpeed * Time.deltaTime);
    //     }
    // }
}
