using UnityEngine;
using UnityEngine.InputSystem;
public class MovementScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Start is called before the first frame update
    public Rigidbody rigidbody;
    float horizInput; //for horizontal movement input
    float vertInput; //vertical

    [Header("Movement Settings")]
    public float maxSpeed = 2;
    public float accelerateTime = 2f;
    public float decelerateTime = 2f;
    float deceleration;
    float acceleration;
    public Vector3 playerInput;
    Vector3 playerDirection;
    public Vector3 velocity;
    public Vector3 inputDirection;
    [Header("Jump Settings")]
    public float gravity;
    float initialJumpSpeed;
    public float apexHeight = 3f;
    public float apexTime = 0.5f;

    [Header("Ground Check Settings")]
    public bool isGrounded = false;
    public float groundCheckOffset = 0.5f;
    public Vector3 groundCheckSize = new(0.4f, 0.1f);
    public LayerMask groundCheckMask;
    public Transform camera;

    [Header("Tightrope Movement Settings")]
    public float tightRopeSpeed = 0.5f;
    float maxBalance;
    public float recoverySpeed = 2f;
    public float tiltSpeed = 60f;
    float playerTiltAngle = 0;

    float tightRopeAccelTime = 1f;
    float tightRopeAcceleration;
    [Header("Tight Rope Detection")]
    public LayerMask tightRope;
    public bool onHPole;
    RaycastHit poleHit;
    public float detectDistance = 0.5f;
    public float poleRadius = 0.5f;
    bool directionChosen;
    float tiltDirection;
    bool completelyTilted = false;
    public enum MovementType
    {
        running,
        climbing,
        tightrope

    }
    public MovementType currentMovement;
    [Header("Camera For Tight Rope")]

    public PlayerCamera playerCamera;
    [Header("Animation Settings")]

    public Animator playerAnimator;
    bool isRunning = false;


    [Header("Ledge Grabbing Settings")]
    public bool isGrabbing;
    public float grabDistance;
    public float ledgeCheckOffSet;
    public LayerMask ledgeCheck;
    public float ledgeSpeed;
    RaycastHit ledgeHit;
    Vector3 currentLedge;
    float ledgeDistance;
    public bool lockedOn;
    float timeToReGrab = 1;
    public bool ledgeJumpActive;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.freezeRotation = true;
        acceleration = maxSpeed / accelerateTime;
        deceleration = maxSpeed / decelerateTime;
        tightRopeAcceleration = tiltSpeed / tightRopeAccelTime;
        gravity = -2 * apexHeight / (Mathf.Pow(apexTime, 2));
        initialJumpSpeed = 2 * apexHeight / apexTime;

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(timeToReGrab);
        PlayerInputs();
        CameraFacing();
        checkForPole();
        CheckForGround();
        CharacterAnimating();
        if (timeToReGrab < 0.6f)
        {
            timeToReGrab += Time.deltaTime;
        }
        if (timeToReGrab >= 0.6f)
        {
            CheckLedge();
            timeToReGrab = 1;
        }
    }
    private void PlayerInputs()
    {
        horizInput = Input.GetAxisRaw("Horizontal");
        vertInput = Input.GetAxisRaw("Vertical");
        playerInput = new Vector3(horizInput, 0, vertInput);


    }
    private void FixedUpdate()
    {
        HandleMovement();

    }
    private void PlayerMovement(Vector3 playerDir)
    {


        velocity.x = CalculatePlayerMovement(playerDir.x, velocity.x);
        velocity.z = CalculatePlayerMovement(playerDir.z, velocity.z);
       

        rigidbody.linearVelocity = new Vector3(velocity.x, velocity.y, velocity.z);
    }

    private float CalculatePlayerMovement(float input, float velocity)
    {
        if (lockedOn == false)
        {
            if (input != 0)
            {

                velocity += acceleration * input * Time.fixedDeltaTime;
                velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);


            }
            else
            {
                velocity = Mathf.MoveTowards(velocity, 0, deceleration * Time.deltaTime);

            }
        }
        return velocity;
    }

    public void HandleMovement()
    {

        switch (currentMovement)
        {
            case MovementType.running:
                PlayerMovement(playerDirection);
                completelyTilted = false;
                JumpUpdate();
                break;
                    }
    }



    private void ClimbingMovement()
    {

        Vector3 climbAxis;
        Vector3 absoluteDirection = new Vector3(Mathf.Abs(currentLedge.x), 0, Mathf.Abs(currentLedge.z));
        if (absoluteDirection.x > absoluteDirection.z)
        {
            climbAxis = Vector3.right * playerDirection.x;
        }
        else
        {
            climbAxis = Vector3.forward * playerDirection.z;
        }

        rigidbody.linearVelocity = new Vector3(climbAxis.x, 0, climbAxis.z);
        if (Input.GetKey(KeyCode.Space))
        {
            rigidbody.linearVelocity = new Vector3(climbAxis.x, velocity.y, climbAxis.z);

        }
        rigidbody.freezeRotation = true;
        if (Input.GetKey(KeyCode.Q))
        {
            rigidbody.freezeRotation = true;
            Debug.Log("input");
            currentMovement = MovementType.running;
            timeToReGrab = 0;
            isGrabbing = false;
            lockedOn = false;
            // rigidbody.constraints = RigidbodyConstraints.None;
            //JumpUpdate is used to bring the play down with the gravity,thought i figured it out and it isnt working so!
            JumpUpdate();




        }
        if (isGrabbing == false)
        {


            lockedOn = false;
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.None;
            //JumpUpdate is used to bring the play down with the gravity, 


        }

        if (isGrounded)
        {
            //gravity from the actual rigidbody turns false 
            //rigidbody.useGravity = false;
            currentMovement = MovementType.running;
        }
    }



    public void CameraFacing()
    {
        //camera directions change with input depending on where the players camera is facing
        //the cameras forward position is multiplied with the Z axis of the player input. forward and backwards movement
        Vector3 cameraForward = camera.transform.forward * playerInput.z;
        Vector3 cameraRight = camera.transform.right * playerInput.x;
        //avoids instances where the player goes up when facing their camera up
        cameraForward.y = 0;
        cameraRight.y = 0;
        playerDirection = (cameraForward + cameraRight).normalized;

    }
    private void CheckForGround()
    {
        //checks the ground to see whether the player is grounded or not!
        Debug.DrawLine(transform.position + Vector3.down * groundCheckOffset, transform.position + Vector3.down * groundCheckOffset - Vector3.down * groundCheckSize.y / 2, Color.red);
        isGrounded = Physics.CheckBox(transform.position + Vector3.down * groundCheckOffset, groundCheckSize / 2, Quaternion.identity, groundCheckMask.value); //if physics box collides with the ground, player is grounded
    }
    private void JumpUpdate()
    {
        if (!isGrounded || lockedOn == false)
        {

            velocity.y += gravity * Time.fixedDeltaTime;
        }
        if (isGrounded || lockedOn == true)
        {


            velocity.y = -0.1f;
            velocity.y = Mathf.Max(velocity.y, -200);

            if (currentMovement == MovementType.climbing)
            {

                rigidbody.constraints = RigidbodyConstraints.None;
                rigidbody.useGravity = false;

                rigidbody.freezeRotation = true;
                timeToReGrab = 1;
            }





        }

        if ((isGrounded || lockedOn == true) && Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Jump!");
            velocity.y = initialJumpSpeed;
            isGrounded = false;
            lockedOn = false;

        }

    }
    private void CheckLedge()
    {
        Debug.DrawRay(rigidbody.position, rigidbody.transform.forward, Color.yellow);
        if (isGrabbing = Physics.Raycast(transform.position, rigidbody.transform.forward, out ledgeHit, grabDistance, ledgeCheck.value))
        {
            lockedOn = true;
            currentLedge = ledgeHit.transform.forward;
            ledgeDistance = Vector3.Distance(currentLedge, rigidbody.position);
            currentMovement = MovementType.climbing;
            ActivateClimbingFreeze();
        }
        else
        {

            isGrabbing = false;
            lockedOn = false;
        }

    }
    private void ActivateClimbingFreeze()
    {
        if (isGrabbing)
        {
            Quaternion lookLedge = Quaternion.LookRotation(ledgeHit.transform.right + ledgeHit.transform.forward);
            Debug.Log("Climbing");
            if (ledgeDistance > 1.5)
            {
                // rigidbody.MoveRotation(Quaternion.Slerp(lookLedge, lookLedge , ledgeSpeed));
                rigidbody.MoveRotation(Quaternion.Slerp(rigidbody.rotation, rigidbody.rotation, ledgeSpeed));
                rigidbody.linearVelocity = (currentLedge + rigidbody.transform.forward);


            }

            rigidbody.MoveRotation(Quaternion.Slerp(rigidbody.rotation, rigidbody.rotation, ledgeSpeed));
            //rigidbody.constraints = RigidbodyConstraints.FreezePositionY;
            rigidbody.linearVelocity = (currentLedge + rigidbody.transform.forward);

            rigidbody.freezeRotation = true;

        }

    }
    private void checkForPole()
    {
        if (completelyTilted == false)
        {
            onHPole = Physics.SphereCast(rigidbody.position, poleRadius, Vector3.down, out poleHit, detectDistance, tightRope);
        }
        if (onHPole && completelyTilted == false)
        {
            currentMovement = MovementType.tightrope;
        }
        else
        {
            currentMovement = MovementType.running;

            rigidbody.freezeRotation = true;
            playerTiltAngle = 0;
        }
    }

    private void TightRopeWalk()
    {

        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX;
        Vector3 walkAxis;
        Vector3 absoluteDirection = new Vector3(Mathf.Abs(poleHit.transform.position.x), 0, Mathf.Abs(poleHit.transform.position.z));

        walkAxis = Vector3.right * playerDirection.x;


        walkAxis += Vector3.forward * playerDirection.z;

        float xClamp = Mathf.Clamp(rigidbody.position.x, poleHit.collider.bounds.min.x, poleHit.collider.bounds.max.x);
        float zClamp = Mathf.Clamp(rigidbody.position.z, poleHit.collider.bounds.min.z, poleHit.collider.bounds.max.z);

        rigidbody.position = new Vector3(xClamp, rigidbody.position.y, zClamp);


        velocity.y = initialJumpSpeed;
        PlayerImbalancing(walkAxis);

        if (Input.GetKey(KeyCode.Space))
        {
            rigidbody.linearVelocity = new Vector3(walkAxis.x, velocity.y, walkAxis.z);


        }




    }
    private void PlayerImbalancing(Vector3 walkAxis)
    {
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX;

        if (Random.value < 0.5f && directionChosen == false)
        {
            directionChosen = true;
            tiltDirection = -1;
            Debug.Log(tiltDirection + " : VER: -1");
        }
        if (Random.value > 0.5f && directionChosen == false)
        {
            directionChosen = true;
            tiltDirection = 1;
            Debug.Log(tiltDirection + " : VER: 1");
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {


            playerTiltAngle += tiltDirection * Time.deltaTime * tightRopeAcceleration;

        }
        else
        {
            if (playerTiltAngle < 0)
            {
                playerTiltAngle += Time.deltaTime * recoverySpeed;

            }
            if (playerTiltAngle > 0)
            {
                playerTiltAngle -= Time.deltaTime * recoverySpeed;


            }

            directionChosen = false;
        }

        //Player has to press Q and E to rebalance themselfs
        /*  if (Input.GetKey(KeyCode.Q))
          {
              playerTiltAngle += Time.deltaTime * recoverySpeed;
              directionChosen = false;
          }
          if (Input.GetKey(KeyCode.E))
          {
              playerTiltAngle -= Time.deltaTime * recoverySpeed;
              directionChosen = false;
          }*/

        //combining both tilting rotation with the camera player direction rotation 
        Quaternion tilt = Quaternion.AngleAxis(playerTiltAngle, Vector3.forward);
        Quaternion finalRotation = tilt * playerCamera.directionToRotate;
        rigidbody.MoveRotation(Quaternion.Slerp(rigidbody.rotation, finalRotation, playerCamera.rotationSpeed * Time.deltaTime));


        rigidbody.linearVelocity = new Vector3(walkAxis.x, 0, walkAxis.z) * tightRopeSpeed * Time.deltaTime;
        if (playerTiltAngle > 40)
        {
            rigidbody.constraints = RigidbodyConstraints.None;
            rigidbody.linearVelocity = Vector3.right * 5;
            completelyTilted = true;
        }
        if (playerTiltAngle < -40)
        {
            rigidbody.constraints = RigidbodyConstraints.None;
            rigidbody.linearVelocity = Vector3.left * 5;

            completelyTilted = true;

        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(233f, 33f, 333f, 5f);
        Gizmos.DrawSphere(transform.position + Vector3.down, poleRadius);

    }
    public void CharacterAnimating()
    {
     
        playerAnimator.SetBool("IsMoving", isRunning);
        if ((playerInput.x > 0 || playerInput.x < 0) || (playerInput.z > 0 || playerInput.z < 0))
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
        
     
    }

}

