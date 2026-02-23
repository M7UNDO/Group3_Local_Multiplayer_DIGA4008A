using Unity.Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Input Fields")]
    [Space(5)]
    private InputActionAsset inputAsset;
    private InputActionMap player;
    private InputAction move;

    [Header("Movement and Jumping")]
    [Space(5)]
    [SerializeField]
    private float maxSpeed;
    private bool isSprinting = false;
    [SerializeField]
    private float sprintMultiplier;
    private Vector3 forceDirection = Vector3.zero;
    [SerializeField]
    private float moveForce;
    [SerializeField]
    private float rotationSpeed = 5f;

    [Header("Slope Handling")]
    [Space(5)]

    private RaycastHit slopeHit;
    public float maxSlopeAngle;


    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private Transform cameraTransform;
    public bool isGrounded;
    public float playerHeight;
    public LayerMask layer;


    [SerializeField]
    private int priorityBoostAmount = 10;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inputAsset = GetComponent<PlayerInput>().actions;
        player = inputAsset.FindActionMap("Player");
        Cursor.lockState = CursorLockMode.Locked;

    }

    private void OnEnable()
    {
        player.FindAction("Jump").performed += Jump;
        player.FindAction("Sprint").performed += Speed;
        player.FindAction("Sprint").canceled += LimitSpeed;
        move = player.FindAction("Move");
        player.Enable();
    }


    private void Speed(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void LimitSpeed(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    private void OnDisable()
    {
        player.FindAction("Jump").performed -= Jump;
        player.FindAction("Sprint").performed -= ctx => isSprinting = true;
        player.FindAction("Sprint").canceled -= ctx => isSprinting = false;
        player.Disable();
    }

    private void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, layer);

        // Quaternion targetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

    }

    private void FixedUpdate()
    {
        Vector2 input = move.ReadValue<Vector2>();
        float currentSpeed = isSprinting ? moveForce * sprintMultiplier : moveForce;
        Vector3 rawMove = input.x * GetCameraRight(playerCamera) + input.y * GetCameraForward(playerCamera);
        rawMove *= currentSpeed;

        // Apply force normally
        forceDirection += rawMove;
        rb.AddForce(forceDirection, ForceMode.Impulse);
        forceDirection = Vector3.zero;

        if (OnSlope())
        {
            rb.AddForce(GetSlopeDirection() * currentSpeed * 20, ForceMode.Impulse);
        }

        // Extra gravity
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity -= Vector3.down * Physics.gravity.y * Time.fixedDeltaTime;

        // Clamp max horizontal speed
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0;
        if (horizontalVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            rb.linearVelocity = horizontalVelocity.normalized * maxSpeed + Vector3.up * rb.linearVelocity.y;

        LookAt();

        rb.useGravity = !OnSlope();



    }

    private void LookAt()
    {
        Vector3 direction = rb.linearVelocity;
        direction.y = 0f;

        if (move.ReadValue<Vector2>().sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
            rb.rotation = Quaternion.LookRotation(direction, Vector3.up);
        else
            rb.angularVelocity = Vector3.zero;
    }

    private Vector3 GetCameraForward(Camera cam)
    {
        Vector3 forward = cam.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    private Vector3 GetCameraRight(Camera cam)
    {
        Vector3 right = cam.transform.right;
        right.y = 0;
        return right.normalized;
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            //forceDirection += Vector3.up * jumpForce;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);


            animator.SetTrigger("jumping");
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeDirection()
    {
        return Vector3.ProjectOnPlane(forceDirection, slopeHit.normal).normalized;
    }


}