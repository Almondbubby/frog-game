using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    

    public Animator grapplingHeadAnimator;
    public SpriteRenderer grapplingBodyRenderer; public SpriteRenderer grapplingHeadRenderer;



    //movement
    public float groundMoveSpeed, maxGroundMoveSpeed, airMoveSpeed, maxAirMoveSpeed, jumpSpeed, swimSpeed; //currently not using a mxAirMoveSpeed
    public float gravityStrength, waterGravityStrength;
    private float vx, vy, ax, ay; // vx/vy is velocity and ax/ay is acceleration
    private int xInput, yInput;
    private enum direction {
        left,
        right,
    }
    direction curDirection = direction.right;


    //grappling
    public float reelInGrappleVelocity;
    private float reelInBuiltUpVelocity;
    private Vector2 connectionPoint;
    
    //fly stuff
    private Vector2 lockedMousePos; 
    private Vector2 lockedHeadDist;
    //private Vector2 lockedAngle;
    private bool flyFlag;
    private float pull = 10f;


    //state management
    public enum states {
        grounded,
        airborne,
        grappling,
        grappleF,
        swim,
        
    };
    private enum actions {
        groundMove,
        airMove,
        jump,
        grapple,
        exitGrapple,
        reelInGrapple,
        swimMove,
    }
    public states curState;
    private Dictionary<Enum, List<Enum>> stateMap = new Dictionary<Enum, List<Enum>> {
        {states.grounded, new List<Enum>() {actions.groundMove, actions.jump, actions.grapple}},
        {states.airborne, new List<Enum>() {actions.airMove, actions.grapple}},
        {states.grappling, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple}},
        {states.grappleF, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple, actions.jump}},
        {states.swim, new List<Enum>() {actions.grapple, actions.reelInGrapple, actions.exitGrapple, actions.swimMove}},
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
        swim = 6,
    }

    // public AnimationID curAnimation = AnimationID.idle;




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
        rb.gravityScale = gravityStrength;
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

        if (canDoAction(actions.swimMove)) {
            rb.velocity = new Vector2(swimSpeed * xInput, swimSpeed * yInput);

            if (rb.velocity.magnitude > 0) 
                animator.SetInteger("curState", (int)AnimationID.swim);
        }


        if (Input.GetKey(KeyCode.Mouse0) && canDoAction(actions.grapple) && flyFlag == false) grapple();
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


    }

    void reelInGrapple() {

        dj.distance -= reelInGrappleVelocity;
        reelInBuiltUpVelocity += reelInGrappleVelocity;
    }

    void grapple() {
        Vector2 mousePos = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerToMouseDistance = mousePos - (Vector2)transform.position;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.Normalize(playerToMouseDistance), playerToMouseDistance.magnitude);

        if(flyFlag == true){
            //rb.gravityScale = 0.0f;
            //curState = states.grappleF;
            rb.velocity = Vector2.zero;
            transform.position = Vector3.MoveTowards(transform.position, (Vector3)lockedMousePos, 10 * Time.deltaTime);
            OnTriggerExit2D(hit.collider);
        }
        
        
        if (connectionPoint != Vector2.zero/* && (curState != states.grappling || curState != states.grappleF)*/)
            lr.SetPositions(new Vector3[] {transform.position, connectionPoint});

        if (!hit.rigidbody) return;

        print("curState: " + curState + "     " + (curState != states.grappling && curState != states.grappleF));
        if (curState != states.grappling && curState != states.grappleF) {
            dj.connectedBody = hit.rigidbody;
            connectionPoint = hit.point;

            dj.connectedAnchor = new Vector2((connectionPoint.x/2) - dj.connectedBody.transform.position.x, 0f);
        }

        if (curState != states.grappleF)
            curState = states.grappling;
        

        
        // Vector2 distBetweenHeadandPoint = connectionPoint - (Vector2)grapplingHeadAnimator.transform.position;
        // grapplingBodyRenderer.enabled = true; grapplingHeadRenderer.enabled = true; thisRenderer.enabled = false;


        //failed script for managing the player's head when they are swinging back and forth on a grappling hook
        // Vector2 headConnectionDist = connectionPoint - (Vector2)grapplingHeadRenderer.transform.parent.transform.position;
        // double headAngle = Math.Abs(Math.Atan(Math.Abs(headConnectionDist.y/headConnectionDist.x)) * 180/Math.PI);
        // print(grapplingHeadRenderer.transform.parent.transform.position);
        // print("headAngle" + headAngle);
        // grapplingHeadRenderer.transform.eulerAngles = new Vector3(0, 0, -1 *(float)headAngle);
        // grapplingHeadAnimator.SetInteger("curState", (int)AnimationID.grapple);


        if(hit.collider.gameObject.tag == "fly" && flyFlag == false){
            //curState = states.grappleF;
            rb.velocity = Vector2.zero;
            lockedMousePos = mousePos;
            // lockedHeadDist = headConnectionDist;
            transform.position = Vector3.MoveTowards(transform.position, (Vector3)mousePos, 10 * Time.deltaTime);
            flyFlag = true;
        }
    
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

    void OnTriggerEnter2D(Collider2D collision) {

        if (collision.gameObject.tag == "Enemy") {
            damage(1);
        }

        if (collision.gameObject.tag == "Obstacle"){
            dead();
        }
        
    }

    void OnTriggerStay2D(Collider2D collision) {

        if (collision.gameObject.tag == "Water") {
            curState = states.swim;
            rb.gravityScale = waterGravityStrength;
        }
    }

    void OnTriggerExit2D(Collider2D collision) {

        if (collision.gameObject.tag == "Water") {
            rb.gravityScale = gravityStrength;
        }
        if(collision.gameObject.tag == "fly"){
                rb.velocity = (Vector2.Scale(lockedHeadDist, new Vector2(50f, 50f))); //fix later
                curState = states.grappleF;
                flyFlag = false;
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }}
