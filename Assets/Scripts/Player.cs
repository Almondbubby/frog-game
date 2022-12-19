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
    public Animator animator; 
    public SpriteRenderer thisRenderer;
    public int health = 3;
    public Vector2 levelBeginning;
    private Vector2 lockedMousePos; //"fly" stuff
    private bool flyFlag;

    public Animator grapplingHeadAnimator;
    public SpriteRenderer grapplingBodyRenderer; public SpriteRenderer grapplingHeadRenderer;



    //movement
    public float groundMoveSpeed, maxGroundMoveSpeed, airMoveSpeed, maxAirMoveSpeed, jumpSpeed; //currently not using a mxAirMoveSpeed
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
        grappleF,
        
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
        {states.grappling, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple}},
        {states.grappleF, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple, actions.jump}}
    };

    bool canDoAction(Enum action) {
        return stateMap[curState].Contains(action);
    }


    // Animation
    public enum AnimationID {
        idle = 0,
        run = 1,
        jump = 2,
        falling = 3,
        rising = 4,
        grapple = 5,
    }

    public AnimationID curAnimation = AnimationID.idle;




    // Physics
    bool isGrounded() {  //generates a box slighty below the player and checks if it hit a collider (box casting)
        Collider2D hit = Physics2D.BoxCast((Vector2)transform.position, new Vector2(Math.Abs(transform.localScale.x) * 0.7f, transform.localScale.y), 0, -Vector2.up, 0.05f).collider;
        if (hit != null && hit.isTrigger == false) return true;
        return false;
    }



    private void Start()
    {
        curState = states.airborne;
        flyFlag = false;
    }

    void FixedUpdate()
    {

        xInput = Math.Sign(Input.GetAxis("Horizontal"));
        yInput = Math.Sign(Input.GetAxis("Vertical"));


        // make sure the game knows when the player is grounded or not
        if (curState == states.airborne && isGrounded()) curState = states.grounded;
        if (curState == states.grounded && !isGrounded()) curState = states.airborne;

        // if the player is grounded then move with the grounded move stats
        if (canDoAction(actions.groundMove) && (xInput == 0 || Math.Sign(xInput) != Math.Sign(rb.velocity.x))) {

            // make sure the player doesn't slide when there is no input in the direction it is going
            rb.velocity = Vector2.zero;

            // manage animations
            animator.SetInteger("curState", (int)AnimationID.idle);   
        }

        if (canDoAction(actions.groundMove) && xInput != 0) {

            if (groundMoveSpeed * xInput * Time.fixedDeltaTime < maxGroundMoveSpeed) 
                rb.velocity += groundMoveSpeed * xInput * Vector2.right * Time.fixedDeltaTime;
            else 
                rb.velocity = maxGroundMoveSpeed * xInput * Vector2.right * Time.fixedDeltaTime;

            // manage which direction the player is facing
            if (rb.velocity.x < 0) curDirection = direction.left;
            if (rb.velocity.x > 0) curDirection = direction.right;  

            // manage animations
            animator.SetInteger("curState", (int)AnimationID.run);  
        }

        if (canDoAction(actions.jump) && yInput == 1) {
            rb.velocity += jumpSpeed * Vector2.up;
            curState = states.airborne;

            // manage animations
            animator.SetInteger("curState", (int)AnimationID.jump);  
        }


        // if the player is not grounded then move with the air move stats
        
        if (canDoAction(actions.airMove)) {
            rb.velocity += airMoveSpeed * xInput * Vector2.right * Time.fixedDeltaTime;

            if (rb.velocity.y < 0) 
                animator.SetInteger("curState", (int)AnimationID.falling);
            if (rb.velocity.y > 0)
                animator.SetInteger("curState", (int)AnimationID.rising);
                  
            
        }


        if (Input.GetKey(KeyCode.Mouse0) && canDoAction(actions.grapple)) grapple();
        else if (canDoAction(actions.exitGrapple)) release();

        if(flyFlag == true) grapple();

        if (Input.GetKey(KeyCode.Space) && canDoAction(actions.reelInGrapple)) reelInGrapple();
        else reelInBuiltUpVelocity = 0;


        // manage direction (TODO: add a special case for when the frog is grappling)
        if (curState == states.grappling) {
            if (transform.position.x < dj.connectedBody.transform.position.x) transform.localScale = new Vector3(Math.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            if (transform.position.x > dj.connectedBody.transform.position.x) transform.localScale = new Vector3(Math.Abs(transform.localScale.x) * -1, transform.localScale.y, transform.localScale.z);
        }
        else {
            if (rb.velocity.x < 0) transform.localScale = new Vector3(Math.Abs(transform.localScale.x) * -1, transform.localScale.y, transform.localScale.z);
            if (rb.velocity.x > 0) transform.localScale = new Vector3(Math.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        //Debug
        // print(curState);
        // print(curDirection);


    }

    void reelInGrapple() {

        dj.distance -= reelInGrappleVelocity;
        reelInBuiltUpVelocity += reelInGrappleVelocity;
    }

    void grapple() {
        if(flyFlag == true){
            //rb.gravityScale = 0.0f;
            //curState = states.grappleF;
            rb.velocity = Vector2.zero;
            transform.position = Vector3.MoveTowards(transform.position, (Vector3)lockedMousePos, 10 * Time.deltaTime);

            if(((Vector2)transform.position).Equals(lockedMousePos)){
                curState = states.grappleF;
                flyFlag = false;
            }
        }
        else{
            Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 playerToMouseDistance = mousePos - (Vector2)transform.position;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.Normalize(playerToMouseDistance), playerToMouseDistance.magnitude);
            
            if (connectionPoint != Vector2.zero)
                lr.SetPositions(new Vector3[] {transform.position, connectionPoint});

            if (!hit.rigidbody) return;

            if (curState != states.grappling || curState != states.grappleF) {
                dj.connectedBody = hit.rigidbody;
                connectionPoint = hit.point;

                dj.connectedAnchor = new Vector2((connectionPoint.x/2) - dj.connectedBody.transform.position.x, 0f);
            }

            if(curState != states.grappleF){
                curState = states.grappling;
            }
            else if(isGrounded()){
                curState = states.grounded;    
            }

            // Vector2 distBetweenHeadandPoint = connectionPoint - (Vector2)grapplingHeadAnimator.transform.position;
           
            if(hit.collider.gameObject.tag == "fly" && flyFlag == false){
                //curState = states.grappleF;
                rb.velocity = Vector2.zero;
                lockedMousePos = mousePos;
                transform.position = Vector3.MoveTowards(transform.position, (Vector3)mousePos, 10 * Time.deltaTime);
                flyFlag = true;
            }
        }
        grapplingBodyRenderer.enabled = true; grapplingHeadRenderer.enabled = true; thisRenderer.enabled = false;
        Vector2 headConnectionDist = connectionPoint - (Vector2)grapplingHeadRenderer.transform.parent.transform.position;
        double headAngle = Math.Abs(Math.Atan(Math.Abs(headConnectionDist.y/headConnectionDist.x)) * 180/Math.PI);
        print(grapplingHeadRenderer.transform.parent.transform.position);
        print("headAngle" + headAngle);
        grapplingHeadRenderer.transform.eulerAngles = new Vector3(0, 0, -1 *(float)headAngle);
        grapplingHeadAnimator.SetInteger("curState", (int)AnimationID.grapple);
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

        grapplingBodyRenderer.enabled = false; grapplingHeadRenderer.enabled = false; thisRenderer.enabled = true;
        if (rb.velocity.y < 0) 
            animator.SetInteger("curState", (int)AnimationID.falling);
        if (rb.velocity.y > 0)
            animator.SetInteger("curState", (int)AnimationID.rising);

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            damage(1);
        }
        if(collision.gameObject.tag == "Obstacle"){
            dead();
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
        //Destroy(this);
        vx = vy = ax = ay = 0;
        rb.position = levelBeginning;
    }

    // void manageAnimations() {
    //     if (curState == grounded)
    // }

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
