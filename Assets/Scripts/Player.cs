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
    private bool go = false; //whether or not to move closer to mouse (grapple)

    public int health = 3;


    //movement
    public float groundMoveSpeed, airMoveSpeed, jumpSpeed; //time unit is seconds
    private float vx, vy, ax, ay; // vx/vy is velocity and ax/ay is acceleration
    private int xInput, yInput;
    private enum direction {
        left,
        right,
    }
    private direction curDirection = direction.right;

    //grappling
    public float reelInGrappleVelocity;
    private float reelInBuiltUpVelocity;
    private Vector2 connectionPoint;
    



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
    private states curState;
    private Dictionary<Enum, List<Enum>> stateMap = new Dictionary<Enum, List<Enum>> {
        {states.grounded, new List<Enum>() {actions.groundMove, actions.jump, actions.grapple}},
        {states.airborne, new List<Enum>() {actions.airMove, actions.grapple}},
        {states.grappling, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple}}
    };

    bool canDoAction(Enum action) {
        return stateMap[curState].Contains(action);
    }



    bool isGrounded() {  //generates a box slighty below the player and checks if it hit a collider (box casting)
        Collider2D hit = Physics2D.BoxCast((Vector2)(transform.position), new Vector2(Math.Abs(transform.localScale.x) * 0.7f, transform.localScale.y), 0, -Vector2.up, 0.01f).collider;
        if (hit != null && hit.isTrigger == false) return true;
        return false;
    }



    private void Start()
    {
        curState = states.airborne;
    }
    void FixedUpdate()
    {

        xInput = Math.Sign(Input.GetAxis("Horizontal"));
        yInput = Math.Sign(Input.GetAxis("Vertical"));


        // make sure the game knows when the player is grounded or not
        if (curState == states.airborne && isGrounded()) curState = states.grounded;
        if (curState == states.grounded && !isGrounded()) curState = states.airborne;

        // if the player is grounded then move with the grounded move stats
        if (canDoAction(actions.groundMove)) {
            rb.velocity += groundMoveSpeed * xInput * Vector2.right * Time.fixedDeltaTime;
        }

        if (canDoAction(actions.jump) && yInput == 1) {
            rb.velocity += jumpSpeed * Vector2.up;
            curState = states.airborne;
        }


        // if the player is not grounded then move with the air move stats
        
        if (canDoAction(actions.airMove)) {
            rb.velocity += airMoveSpeed * xInput * Vector2.right * Time.fixedDeltaTime;
        }


        if (Input.GetKey(KeyCode.Mouse0) && canDoAction(actions.grapple)) grapple();
        else if (canDoAction(actions.exitGrapple)) release();

        if (Input.GetKey(KeyCode.Space) && canDoAction(actions.reelInGrapple)) reelInGrapple();
        else reelInBuiltUpVelocity = 0;

        //Debug
        // print(curState);
    }

    void reelInGrapple() {

        dj.distance -= reelInGrappleVelocity;
        reelInBuiltUpVelocity += reelInGrappleVelocity;
    }

    void grapple() {

        Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerToMouseDistance = mousePos - (Vector2)transform.position;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.Normalize(playerToMouseDistance), playerToMouseDistance.magnitude);
        
        if (connectionPoint != Vector2.zero)
            lr.SetPositions(new Vector3[] {transform.position, connectionPoint});

        if (!hit.rigidbody) return;

        if (curState != states.grappling) {
            dj.connectedBody = hit.rigidbody;
            connectionPoint = hit.point;


            print(connectionPoint);
            print(dj.connectedBody.transform.position);

            dj.connectedAnchor = new Vector2((connectionPoint.x/2) - dj.connectedBody.transform.position.x, 0f);
        }
        
        curState = states.grappling;

    }

    void release() {

        if (isGrounded()) curState = states.grounded;
        if (!isGrounded()) curState = states.airborne;
        
        rb.velocity += (Vector2)(dj.connectedBody.transform.position - transform.position) * reelInBuiltUpVelocity;
        reelInBuiltUpVelocity = 0;
        lr.SetPositions(new Vector3[] {transform.position, transform.position});
        
        dj.connectedBody = rb;
        dj.connectedAnchor = Vector2.zero;
        connectionPoint = Vector2.zero;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            damage(1);
        }
    }

    void damage(int dmg)
    {
        health -= dmg;
        Debug.Log(health);
        if(health <= 0)
        {
            dead();
        }
    }

    void dead()
    {
        Destroy(this);
    }

    // void swing()
    // {
    //     Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
    //     if (Input.GetKeyDown(KeyCode.Mouse0))
    //     {
    //         //check if theres anything in the way
    //         RaycastHit2D hit = Physics2D.Raycast(transform.position,mousePos-(Vector2)transform.position);

    //         //check whether mouse is actually on something thats grapplable
    //         RaycastHit2D check = Physics2D.Raycast(mousePos, Vector2.zero);
    //         bool good = true;
    //         if(hit.collider != null && hit.collider.gameObject.layer != 3)
    //         {
    //             good = false;
    //         }

    //         if (hit.collider != null && hit.collider.gameObject.layer == 3 && good && check.collider != null)
    //         {
    //             dj.enabled = true;
    //             dj.connectedAnchor = mousePos;
    //             lr.enabled = true;

    //             //draw line
    //             lr.SetPosition(0, transform.position);
    //             lr.SetPosition(1, mousePos);
    //         }


    //     }
    //     else if (Input.GetKeyUp(KeyCode.Mouse0))
    //     {
    //         dj.enabled = false;
    //         lr.enabled = false;
    //     }

    //     if (dj.enabled)
    //     {
    //         //update line
    //         lr.SetPosition(0, transform.position);
    //     }
    // }
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
