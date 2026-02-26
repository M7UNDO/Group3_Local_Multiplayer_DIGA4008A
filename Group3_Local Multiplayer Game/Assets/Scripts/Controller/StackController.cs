using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

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

    [Header("Input Settings")]
    public float GamepadLookSensitivity = 100f;
    public float MouseLookSensitivity = 1f;
    public float MoveDeadzone = 0.1f;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // Components
    private CharacterController _controller;
    private Animator _animator;
    private GameObject _mainCamera;

    private bool _hasAnimator;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

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

    // Track device types
    private bool _bottomUsingMouse;
    private bool _topUsingMouse;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        // Get camera reference
        FindCameraReference();

        // Initialize camera angles
        if (CinemachineCameraTarget != null)
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _cinemachineTargetPitch = CinemachineCameraTarget.transform.rotation.eulerAngles.x;
        }
    }

    private void FindCameraReference()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            if (_mainCamera == null && Camera.main != null)
                _mainCamera = Camera.main.gameObject;

            if (_mainCamera == null)
            {
                Camera cam = FindFirstObjectByType<Camera>();
                if (cam != null)
                    _mainCamera = cam.gameObject;
            }
        }
    }

    private void Start()
    {
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
        {
            bottom.isTop = false;
            // Detect input device for bottom player
            if (bottom.playerInput != null)
            {
                _bottomUsingMouse = bottom.playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        if (top != null)
        {
            top.isTop = true;
            // Detect input device for top player
            if (top.playerInput != null)
            {
                _topUsingMouse = top.playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        Debug.Log($"StackedController initialized: Bottom Player {bottom?.playerIndex} (Using Mouse: {_bottomUsingMouse}), Top Player {top?.playerIndex} (Using Mouse: {_topUsingMouse})");
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
            Vector2 rawMove = _bottomPlayer.inputHandler.move;

            // Apply deadzone for gamepad
            if (!_bottomUsingMouse && rawMove.magnitude < MoveDeadzone)
            {
                _moveInput = Vector2.zero;
            }
            else
            {
                _moveInput = rawMove;
            }

            _sprintInput = _bottomPlayer.inputHandler.sprint;
            _jumpInput = _bottomPlayer.inputHandler.jump;

            // Debug for movement
            if (_moveInput.magnitude > 0.1f)
                Debug.Log($"Bottom Player Move: {_moveInput}");
        }

        // Get look input from top player
        if (_topPlayer?.inputHandler != null && _topPlayer.playerObject != null)
        {
            Vector2 rawLook = _topPlayer.inputHandler.look;

            // Apply deadzone for gamepad
            if (!_topUsingMouse && rawLook.magnitude < MoveDeadzone)
            {
                _lookInput = Vector2.zero;
            }
            else
            {
                _lookInput = rawLook;
            }

            // Update device detection if it changed
            if (_topPlayer.playerInput != null)
            {
                _topUsingMouse = _topPlayer.playerInput.currentControlScheme == "KeyboardMouse";
            }

            // Debug for look
            if (_lookInput.magnitude > 0.1f)
                Debug.Log($"Top Player Look: {_lookInput} (Using Mouse: {_topUsingMouse})");
        }
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _sprintInput ? SprintSpeed : MoveSpeed;
        if (_moveInput == Vector2.zero) targetSpeed = 0.0f;


        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        //float inputMagnitude = _input.analogMovement ? _moveInput.magnitude : 1f;
        float inputMagnitude =  _moveInput.magnitude;

        // accelerate or decelerate to target speed
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

        // normalise input direction
        Vector3 inputDirection = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;

        if (_moveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);


        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
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
        if (_lookInput.magnitude >= _threshold && !LockCameraPosition)
        {
            // Different multiplier for mouse vs controller
            float deltaTimeMultiplier = Time.deltaTime;

            /*if (_topUsingMouse)
            {
                // Mouse: use raw input (already frame-rate independent)
                deltaTimeMultiplier = MouseLookSensitivity;
            }
            else
            {
                // Gamepad: scale by time and sensitivity
                deltaTimeMultiplier = GamepadLookSensitivity * Time.deltaTime;
            }*/

            _cinemachineTargetYaw += _lookInput.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _lookInput.y * deltaTimeMultiplier;
        }

        // Clamp the pitch angle
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Yaw can rotate fully (no clamp needed, but ensure it stays within reasonable range)
        if (_cinemachineTargetYaw < -360f) _cinemachineTargetYaw += 360f;
        if (_cinemachineTargetYaw > 360f) _cinemachineTargetYaw -= 360f;

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

        int y = 10;
        int lineHeight = 25;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Move Input: {_moveInput} (Magnitude: {_moveInput.magnitude:F2})");
        y += lineHeight;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Look Input: {_lookInput} (Magnitude: {_lookInput.magnitude:F2})");
        y += lineHeight;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Jump: {_jumpInput}, Sprint: {_sprintInput}");
        y += lineHeight;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Grounded: {Grounded}, Speed: {_speed:F2}");
        y += lineHeight;

        GUI.Label(new Rect(10, y, 400, lineHeight), $"Bottom Using Mouse: {_bottomUsingMouse}, Top Using Mouse: {_topUsingMouse}");
        y += lineHeight;

        if (_bottomPlayer != null)
            GUI.Label(new Rect(10, y, 400, lineHeight), $"Bottom Player Active: {_bottomPlayer.playerObject?.activeInHierarchy}");
        y += lineHeight;

        if (_topPlayer != null)
            GUI.Label(new Rect(10, y, 400, lineHeight), $"Top Player Active: {_topPlayer.playerObject?.activeInHierarchy}");
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