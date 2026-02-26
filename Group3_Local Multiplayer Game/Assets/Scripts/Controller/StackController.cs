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
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

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

    // Player references for input sources
    private StackManager.PlayerStackInfo _bottomPlayer;
    private StackManager.PlayerStackInfo _topPlayer;

    private const float _threshold = 0.01f;

    // Track if device is mouse (for camera sensitivity)
    private bool _isUsingMouse;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        // Get camera reference - try multiple ways
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            // If no camera with MainCamera tag, try Camera.main
            if (_mainCamera == null && Camera.main != null)
                _mainCamera = Camera.main.gameObject;

            // If still null, find any camera
            if (_mainCamera == null)
            {
                Camera cam = FindFirstObjectByType<Camera>();
                if (cam != null)
                    _mainCamera = cam.gameObject;
            }
        }

        // Initialize camera angles
        if (CinemachineCameraTarget != null)
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _cinemachineTargetPitch = CinemachineCameraTarget.transform.rotation.eulerAngles.x;
        }
    }

    private void Start()
    {
        // Double-check camera target assignment
        if (CinemachineCameraTarget == null)
        {
            Debug.LogError("CinemachineCameraTarget is not assigned on StackedController!", this);
        }

        if (_mainCamera == null)
        {
            Debug.LogError("Main camera reference could not be found!", this);
        }
    }

    public void Initialize(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _bottomPlayer = bottom;
        _topPlayer = top;

        if (bottom != null)
            bottom.isTop = false;
        if (top != null)
            top.isTop = true;

        Debug.Log($"StackedController initialized: Bottom Player {bottom?.playerIndex}, Top Player {top?.playerIndex}");
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
        // Reset inputs
        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _jumpInput = false;
        _sprintInput = false;

        // Get movement input from bottom player
        if (_bottomPlayer?.inputHandler != null && _bottomPlayer.playerObject != null)
        {
            _moveInput = _bottomPlayer.inputHandler.move;
            _sprintInput = _bottomPlayer.inputHandler.sprint;
            _jumpInput = _bottomPlayer.inputHandler.jump;
        }

        // Get look input from top player
        if (_topPlayer?.inputHandler != null && _topPlayer.playerObject != null)
        {
            _lookInput = _topPlayer.inputHandler.look;

            // Determine if using mouse (for deltaTime multiplier)
            if (_topPlayer.playerInput != null)
            {
                _isUsingMouse = _topPlayer.playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        // Debug logging (remove in production)
        if (_moveInput != Vector2.zero)
            Debug.Log($"Move Input: {_moveInput}");
        if (_lookInput != Vector2.zero)
            Debug.Log($"Look Input: {_lookInput}");
    }

    private void Move()
    {
        if (_controller == null) return;

        // Calculate target speed
        float targetSpeed = _sprintInput ? SprintSpeed : MoveSpeed;
        if (_moveInput == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

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
        if (_moveInput != Vector2.zero && _mainCamera != null)
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
        if (_controller == null) return;

        Vector3 spherePosition = new Vector3(transform.position.x,
            transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        if (_animator != null)
            _animator.SetBool("Grounded", Grounded);
    }

    private void JumpAndGravity()
    {
        if (_controller == null) return;

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

            if (_animator != null)
                _animator.SetBool("FreeFall", false);
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
        if (CinemachineCameraTarget == null) return;

        // Only rotate if there's look input and camera isn't locked
        if (_lookInput.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // Different multiplier for mouse vs controller
            float deltaTimeMultiplier = _isUsingMouse ? 1.0f : Time.deltaTime * 100f; // Higher multiplier for controller

            _cinemachineTargetYaw += _lookInput.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _lookInput.y * deltaTimeMultiplier;
        }

        // Clamp the pitch angle
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Yaw can rotate fully
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);

        // Apply rotation to camera target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
            _cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw,
            0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    // Debug visualization
    private void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUI.Label(new Rect(10, 10, 400, 20), $"Move Input: {_moveInput}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Look Input: {_lookInput}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Jump: {_jumpInput}, Sprint: {_sprintInput}");
        GUI.Label(new Rect(10, 70, 400, 20), $"Grounded: {Grounded}");
        GUI.Label(new Rect(10, 90, 400, 20), $"Speed: {_speed:F2}");
        GUI.Label(new Rect(10, 110, 400, 20), $"Using Mouse: {_isUsingMouse}");

        if (_bottomPlayer != null)
            GUI.Label(new Rect(10, 130, 400, 20), $"Bottom Player Active: {_bottomPlayer.playerObject?.activeInHierarchy}");
        if (_topPlayer != null)
            GUI.Label(new Rect(10, 150, 400, 20), $"Top Player Active: {_topPlayer.playerObject?.activeInHierarchy}");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw grounded check sphere
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.color = Grounded ? transparentGreen : transparentRed;
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }
}