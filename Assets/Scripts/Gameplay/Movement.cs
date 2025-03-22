using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Visuals")]
    public float ViewBobbingStrength;
    public float ViewBobbingSpeed;
    public float ViewBobbingHorStrength;
    public float ViewBobbingHorSpeed;
    public float LerpSpeed = 2f;
    public float CameraTiltForce = 1;
    public Camera cam;
    [Header("Moving")]
    public float moveSpeed = 6f;
    float _moveSpeed;
    public float airSpeed = 3f;
    public float sprintAdd;
    public float crouchSubtract;
    public float GroundCheckHeight = 1.5f;
    float horMovement;
    float vertMovement;
    [Header("Physics")]
    float drag = 6;
    public float normalDrag;
    public float airDrag = 3;
    float add;

    private FootstepsSFX footsteps;
    
    public Vector3 MoveDirection {
        get { return moveDirection; }
    }

    Vector3 moveDirection;


    bool isGrounded;
    [Header("Jumping")]
    public float jumpForce = 5f;

    [Header("Custom Gravity")]
    public bool useGravity;
    public float CustomGravity;
    [HideInInspector]
    public Rigidbody rb;
    public CapsuleCollider col;
    bool drunkWalking;
    Vector3 drunkDirection;

    public MouseRotation mouse;
    private Vector3 og_offset;
    public bool moweLawn;
    public Stats stats;
    public bool isSprinting;
    private float baseFOV;
    private float fov;
    public bool isCrouching = false;
    [HideInInspector]
    public bool isMoving;
    public Player playersc;
    float initialHeight;
    private void Start()
    {
        footsteps = GetComponent<FootstepsSFX>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        normalDrag = drag;
        og_offset = mouse.transform.localPosition;
        baseFOV = cam.fieldOfView;
        fov = baseFOV;
        initialHeight = col.height;
    }
    
    public Vector3 GetAngles()
    {
        Vector3 res = new Vector3(mouse.transform.eulerAngles.x, transform.eulerAngles.y, 0);
        return res;
    }
    
    bool sw;

    private void Update()
    {
        playersc.inputs.MoveDirection = moveDirection;
        playersc.inputs.isMoving = isMoving;
        playersc.inputs.isSprinting = isSprinting;
        playersc.inputs.isCrouching = isCrouching;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, Time.deltaTime * 10);
        isGrounded = Physics.Raycast(transform.position, Vector3.down, GroundCheckHeight);
        horMovement = NonUIInput.GetAxisRaw("Horizontal");
        vertMovement = NonUIInput.GetAxisRaw("Vertical");

        moveDirection = mouse.transform.forward * vertMovement + mouse.transform.right * horMovement;
        moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
        
        if (moveDirection.magnitude > 0)
            isMoving = true;
        else
            isMoving = false;
        rb.drag = drag;
        if (isGrounded)
        {
            if (!sw)
            {
                footsteps.PlayStepSound();
                sw = true;
            }
        }
        else
            sw = false;
        if (NonUIInput.GetKey(KeyCode.LeftShift))
        {

            add = sprintAdd * 1000;
            isSprinting = true;
            fov = baseFOV + 10;

        }
        else
        {
            fov = baseFOV;
            add = 0;
            isSprinting = false;

        }

        if (NonUIInput.GetKey(KeyCode.LeftControl))
        {
            col.height = 0.8f;
            isCrouching = true;
        }
        else
        {
            col.height = initialHeight;
            isCrouching = false;

        }

        if (NonUIInput.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }




        if (!isGrounded)
        {
            drag = airDrag;
            _moveSpeed = airSpeed * 1000;
        }
        else
        {

            drag = normalDrag;
            _moveSpeed = moveSpeed * 1000 + add;
            if (isCrouching)
                _moveSpeed -= crouchSubtract * 1000;

        }



        //Camera 
    }
    bool stepSw, inertiaSw;
    float t, t2, lerp_t;
    Vector3 lerpedPos;


    private void FixedUpdate()
    {

       
        //get drunk lmao

        if (stats.alcohol > 0.8f)
        {
            if (!drunkWalking)
                StartCoroutine(drunkMovement());
        }

        //get sober

        
        if (useGravity)
        {
            rb.useGravity = false;
            rb.AddForce(Vector3.down * CustomGravity);//(rb.mass * rb.mass));
        }
        else
        {
            rb.useGravity = true;
        }
        if (moveDirection.magnitude > 0 && isGrounded && rb.velocity.magnitude > 2) //View bobbing
        {
            float vbs = ViewBobbingSpeed;
            if (isCrouching)
                vbs /= 2;
            t += Time.deltaTime * vbs;
            t2 += Time.deltaTime * vbs / 2;
            float sin;
            float lrsin;
            
            if (!isSprinting)
            {
                sin = Mathf.Sin(t) * ViewBobbingStrength;
                lrsin = Mathf.Sin(t2 + 90 * Mathf.Deg2Rad) * ViewBobbingHorStrength;
            }
            else
            {
                sin = Mathf.Sin(t * 2) * ViewBobbingStrength;
                lrsin = Mathf.Sin((t2*2) + 90 * Mathf.Deg2Rad) * ViewBobbingHorStrength;
            }

            
            Vector3 newPos = new Vector3(og_offset.x+lrsin, og_offset.y + sin, og_offset.z);
            lerpedPos = newPos;
            if (sin < ViewBobbingStrength - ViewBobbingStrength / 10)
            {
                if (!stepSw)
                {
                    if (isGrounded)
                        footsteps.PlayStepSound();
                    stepSw = true;
                }
            }
            else
                stepSw = false;
            
        }
        else
        {
            lerpedPos = og_offset;

        }
        rb.AddForce(moveDirection.normalized * _moveSpeed * Time.deltaTime, ForceMode.Acceleration);
        rb.AddForce(drunkDirection.normalized * (_moveSpeed / 2) * stats.alcohol * Time.deltaTime, ForceMode.Acceleration);

       
        mouse.transform.localPosition = Vector3.Lerp(mouse.transform.localPosition, lerpedPos, Time.deltaTime * LerpSpeed);
        

    }

    IEnumerator drunkMovement()
    {
        drunkWalking = true;

        drunkDirection = transform.forward * Random.Range(-1f, 1f) * (stats.alcohol) + transform.right * Random.Range(-1f, 1f) * (stats.alcohol);

        yield return new WaitForSeconds(Random.Range(1, Mathf.Clamp(stats.alcohol * 10, 1, 2)));
        drunkDirection = transform.forward * 0 + transform.right * 0;
        yield return new WaitForSeconds(Random.Range(0, 3 - Mathf.Clamp(stats.alcohol * 5, 0, 4)));
        drunkWalking = false;

    }
}
