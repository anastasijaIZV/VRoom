using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
     public Transform head;
     public Rigidbody rb;

     [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    public float slideSpeed = 9f;
    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;
    public float impactThreshold;
    public float itemPickupDistance;
    private float startYScale;

[Header("Camera Effects")]
    public float baseCameraFov = 60f;
    public float baseCameraHeight = .85f;

    public float walkBobbingRate = .75f;
    public float runBobbingRate = 1f;
    public float maxWalkBobbingOffset = .2f;
    public float maxRunBobbingOffset = .3f;

    public float cameraShakeThreshold = 10f;
    [Range(0f, 0.03f)] public float cameraShakeRate = .015f;
    public float maxVerticalFallShakeAngle = 40f;
    public float maxHorizontalFallShakeAngle = 40f;

    

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private LayerMask whatIsGround;
    bool grounded;
    public float maxSlopeAngle;
    public RaycastHit slopeHit;
    private bool exitingSlope;
    private bool sliding;

    private bool canMove = true;
Transform attachedObject = null;
   float attachedDistance = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startYScale = transform.localScale.y;
    }

    void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, defaultHeight * 0.5f + 0.2f, whatIsGround);

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;

        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }
        float time = 0;
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * walkSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            if (rb.velocity.magnitude > walkSpeed)
                rb.velocity = rb.velocity.normalized * walkSpeed;

            float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            float slopeAngleIncrease = 1 + (slopeAngle / 90f);

            time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
        }
    

        if (Input.GetKey(KeyCode.L) && canMove)
        {

            if (OnSlope() && rb.velocity.y < 0.1f)
                walkSpeed = slideSpeed;

            else
                walkSpeed = slideSpeed;
                runSpeed = slideSpeed;
                characterController.height = crouchHeight;

        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

//  If controls are pressed and is touching the ground
        Vector3 newVelocity;
        bool isGrounded = false;
        bool isJumping = false;
    transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * 2f);   // Adjust the multiplier for different rotation speed

        newVelocity = Vector3.up * rb.velocity.y;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f)) {
            isGrounded = true;

            //  Check the ground tag for different walking sounds
            //  Edit this part to check for the other tags of yours!
           
        }
        else isGrounded = false;

        if (isGrounded) {
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping) {
                newVelocity.y = runSpeed;
                isJumping = true;
            }
        }
        rb.velocity = transform.TransformDirection(newVelocity);

        bool isMovingOnGround = (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f) && isGrounded;
        


        //  Head bob
        if (isMovingOnGround) {
            float bobbingRate = Input.GetKey(KeyCode.LeftShift) ? runBobbingRate : walkBobbingRate;
            float bobbingOffset = Input.GetKey(KeyCode.LeftShift) ? maxRunBobbingOffset : maxWalkBobbingOffset;
            Vector3 targetHeadPosition = Vector3.up * baseCameraHeight + Vector3.up * (Mathf.PingPong(Time.time * bobbingRate, bobbingOffset) - bobbingOffset*.5f);
            head.localPosition = Vector3.Lerp(head.localPosition, targetHeadPosition, .1f);
        }
        
Vector3 e = head.eulerAngles;
        e.x -= Input.GetAxis("Mouse Y") * 2f;   //  Edit the multiplier to adjust the rotate speed
        e.x = RestrictAngle(e.x, -85f, 85f);    //  This is clamped to 85 degrees. You may edit this.
        head.eulerAngles = e;

        // FOV
        float fovOffset = (rb.velocity.y < 0f) ? Mathf.Sqrt(Mathf.Abs(rb.velocity.y)) : 0f;
        GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, baseCameraFov + fovOffset, .25f);

         if (!isGrounded && Mathf.Abs(rb.velocity.y) >= cameraShakeThreshold) {
            Vector3 newAngle = head.localEulerAngles;
            newAngle += Vector3.right * Random.Range(-maxVerticalFallShakeAngle, maxVerticalFallShakeAngle);
            newAngle += Vector3.up * Random.Range(-maxHorizontalFallShakeAngle, maxHorizontalFallShakeAngle);
            head.localEulerAngles = Vector3.Lerp(head.localEulerAngles, newAngle, cameraShakeRate);
        }
        else {
            //  We have to reset the y-rotation of the head
            //  because we added a random value to it when falling!
            e = head.localEulerAngles;
            e.y = 0f;
            head.localEulerAngles = e;
        }

        // Vertical rotation


        bool cast = Physics.Raycast(head.position, head.forward, out hit, itemPickupDistance);


        if (Input.GetKeyDown(KeyCode.F)) {
            //  Drop the picked object
            if (attachedObject != null) {
                attachedObject.SetParent(null);

                //  Disable is kinematic for the rigidbody, if any
                if (attachedObject.GetComponent<Rigidbody>() != null)
                    attachedObject.GetComponent<Rigidbody>().isKinematic = false;

                //  Enable the collider, if any
                if (attachedObject.GetComponent<Collider>() != null)
                    attachedObject.GetComponent<Collider>().enabled = true;

                attachedObject = null;
            }
            //  Pick up an object
            else {
                if (cast) {
                    if (hit.transform.CompareTag("pickable")) {
                        attachedObject = hit.transform;
                        attachedObject.SetParent(transform);

                        attachedDistance = Vector3.Distance(attachedObject.position, head.position);

                        //  Enable is kinematic for the rigidbody, if any
                        if (attachedObject.GetComponent<Rigidbody>() != null)
                            attachedObject.GetComponent<Rigidbody>().isKinematic = true;

                        //  Disable the collider, if any
                        //  This is necessary
                        if (attachedObject.GetComponent<Collider>() != null)
                            attachedObject.GetComponent<Collider>().enabled = false;
                    }
                }
            }
        }

    }

    void FixedUpdate(){

    }

    public static float RestrictAngle(float angle, float angleMin, float angleMax) {
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        if (angle > angleMax)
            angle = angleMax;
        if (angle < angleMin)
            angle = angleMin;

        return angle;
    }
        

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, defaultHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }


}

public class sliding : MonoBehaviour
{

    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    public Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideYScale;
    private float startYScale;

    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = playerObj.localScale.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.L) && (horizontalInput != 0 || verticalInput != 0))
            StartSlide();

        if (Input.GetKeyUp(KeyCode.L))
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.L))
            SlidingMovement();
    }

    private void StartSlide()
    {

        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // sliding normal
        if (!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }

        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection() * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    { 
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}