using UnityEngine;
using UnityEngine.InputSystem;

public class StackedController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MoveSpeed = 3.0f;
    public float SprintSpeed = 6.0f;
    public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    [Header("Jump Settings")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Header("Grounded Settings")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public bool LockCameraPosition = false;

    // Components
    private CharacterController _controller;
    private Animator _animator;
    private GameObject _mainCamera;

    // Movement variables
    private float _speed;
    private float _animationBlend;
    private float _targetRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // Input references
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _interactInput;

    // Player references for input sources
    private StackManager.PlayerStackInfo _bottomPlayer;
    private StackManager.PlayerStackInfo _topPlayer;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        // Get camera reference
        if (_mainCamera == null)
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    public void Initialize(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _bottomPlayer = bottom;
        _topPlayer = top;

        bottom.isTop = false;
        top.isTop = true;

        // Set up input handlers for both players
        SetupInputHandlers();
    }

    private void SetupInputHandlers()
    {
        // Bottom player controls movement (WASD/Left Stick)
        var bottomInput = _bottomPlayer.inputHandler;

        // Top player controls interactions (E, etc)
        var topInput = _topPlayer.inputHandler;
    }

    private void Update()
    {
        // Get inputs from both players
        GatherInputs();

        // Handle movement and physics
        GroundedCheck();
        Move();
        JumpAndGravity();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GatherInputs()
    {
        if (_bottomPlayer?.inputHandler != null)
        {
            // Bottom player controls movement
            _moveInput = _bottomPlayer.inputHandler.move;
            _sprintInput = _bottomPlayer.inputHandler.sprint;
            _jumpInput = _bottomPlayer.inputHandler.jump;
        }

        if (_topPlayer?.inputHandler != null)
        {
            // Top player controls camera/look
            _lookInput = _topPlayer.inputHandler.look;

            // Future: top player controls interaction
            // _interactInput = _topPlayer.inputHandler.interact;
        }
    }

    private void Move()
    {
        // Calculate target speed
        float targetSpeed = _sprintInput ? SprintSpeed : MoveSpeed;
        if (_moveInput == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = 1f; // Using full magnitude for consistent movement

        // Smooth speed changes
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // Handle rotation based on movement input (bottom player)
        if (_moveInput != Vector2.zero)
        {
            Vector3 inputDirection = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // Move the character
        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // Update animator
        if (_animator != null)
        {
            _animator.SetFloat("Speed", _animationBlend);
            _animator.SetFloat("MotionSpeed", inputMagnitude);
        }
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x,
            transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        if (_animator != null)
            _animator.SetBool("Grounded", Grounded);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            if (_verticalVelocity < 0.0f)
                _verticalVelocity = -2f;

            // Jump only if bottom player presses jump
            if (_jumpInput)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                if (_animator != null)
                    _animator.SetBool("Jump", true);
            }
            else
            {
                if (_animator != null)
                    _animator.SetBool("Jump", false);
            }
        }
        else
        {
            if (_animator != null)
            {
                _animator.SetBool("FreeFall", true);
                _animator.SetBool("Jump", false);
            }
        }

        // Apply gravity
        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void CameraRotation()
    {
        // Camera controlled by top player's look input
        if (_lookInput.sqrMagnitude >= 0.01f && !LockCameraPosition)
        {
            float deltaTimeMultiplier = 1.0f; // Using 1 for mouse, can make dynamic later

            // Get current rotation from Cinemachine target
            Vector3 currentRotation = CinemachineCameraTarget.transform.rotation.eulerAngles;
            float targetYaw = currentRotation.y + _lookInput.x * deltaTimeMultiplier;
            float targetPitch = currentRotation.x - _lookInput.y * deltaTimeMultiplier;

            // Clamp pitch
            targetPitch = Mathf.Clamp(targetPitch, BottomClamp, TopClamp);

            // Apply rotation
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(targetPitch, targetYaw, 0.0f);
        }
    }
}