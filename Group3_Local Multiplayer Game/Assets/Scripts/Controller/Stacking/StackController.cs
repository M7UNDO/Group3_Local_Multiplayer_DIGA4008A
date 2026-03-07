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
    public static bool canMove {  get; private set; } = true;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Jump Settings")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    public float JumpTimeout = 0.5f;
    public float FallTimeout = 0.15f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

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

    [Header("Cinemachine")]
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    [Header("Components")]
    private CharacterController _controller;
    [SerializeField]private Animator _animator;
    private GameObject _mainCamera;

    private bool _hasAnimator;

    [Header("Animation")]
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;


    [Header("Player Movement")]
    private float _speed;
    private float _animationBlend;
    private float _targetRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;


    [Header("Pick Up")]
    [SerializeField] private Outline outline;
    private ObjectGrabbable currentObject;
    float pickUpDistance = 10f;
    public LayerMask pickUpLayerMask;
    public Transform objectGrabPointTransform;
    [SerializeField] private ObjectGrabbable objectGrabbable;
    public Transform playerCameraTransform;
    private bool pickedUp;


    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _grabInput;
    private bool _previousGrabInput;


    public StackManager.PlayerStackInfo _bottomPlayer;
    public StackManager.PlayerStackInfo _topPlayer;

    private const float _threshold = 0.01f;

    private bool _bottomUsingMouse;
    private bool _topUsingMouse;

    public Vector2 GetTopPlayerBalanceInput()
    {
        if (_topPlayer?.inputHandler != null)
        {
            return _topPlayer.inputHandler.balance;
        }
        return Vector2.zero;
    }

    public Vector3 GetCurrentVelocity()
    {
        return _controller.velocity;
    }

    public Vector3 GetMovementDirection()
    {
        return _moveInput;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        FindCameraReference();

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
        _hasAnimator = TryGetComponent(out _animator);
        if (CinemachineCameraTarget == null)
        {
            Debug.LogError("CinemachineCameraTarget is not assigned on StackedController!", this);
        }

        if (_mainCamera == null)
        {
            Debug.LogError("Main camera reference could not be found!", this);
        }

        AssignAnimationIDs();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    public static void SetMovement(bool isAbleToMove)
    {
        canMove = isAbleToMove;
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    public void Initialize(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _bottomPlayer = bottom;
        _topPlayer = top;

        if (bottom != null)
        {
            bottom.isTop = false;
            if (bottom.playerInput != null)
            {
                _bottomUsingMouse = bottom.playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        if (top != null)
        {
            top.isTop = true;
            if (top.playerInput != null)
            {
                _topUsingMouse = top.playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        SpineBalanceController spineBalance = GetComponent<SpineBalanceController>();
        if (spineBalance != null)
        {
            spineBalance.SetPlayers(bottom, top);
        }
    }


    private void Update()
    {
        if (!canMove) return;
        GatherInputs();

        //movement and physics
        GroundedCheck();
        Move();
        JumpAndGravity();
        DetectObject();
        Grab();
    }

    private void LateUpdate()
    {
        if (!canMove) return;
        CameraRotation();
    }

    private void GatherInputs()
    {
        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _jumpInput = false;
        _sprintInput = false;
        _grabInput = false;


        // BOTTOM PLAYER INPUT

        Vector2 bottomMove = Vector2.zero;
        Vector2 bottomLook = Vector2.zero;

        if (_bottomPlayer?.inputHandler != null)
        {
            // Movement
            Vector2 rawMove = _bottomPlayer.inputHandler.move;

            if (!_bottomUsingMouse && rawMove.magnitude < MoveDeadzone)
                bottomMove = Vector2.zero;
            else
                bottomMove = rawMove;

            /* Look
            Vector2 rawLook = _bottomPlayer.inputHandler.look;

            if (!_bottomUsingMouse && rawLook.magnitude < MoveDeadzone)
                bottomLook = Vector2.zero;
            else
                bottomLook = rawLook;*/

            _sprintInput = _bottomPlayer.inputHandler.sprint;
            _jumpInput = _bottomPlayer.inputHandler.jump;

            _moveInput = bottomMove;

            // Device change detection
            if (_bottomPlayer.playerInput != null)
                _bottomUsingMouse = _bottomPlayer.playerInput.currentControlScheme == "KeyboardMouse";
        }

        // TOP PLAYER INPUT

        Vector2 topLook = Vector2.zero;

        if (_topPlayer?.inputHandler != null)
        {
            Vector2 rawLook = _topPlayer.inputHandler.look;

            if (!_topUsingMouse && rawLook.magnitude < MoveDeadzone)
                topLook = Vector2.zero;
            else
                topLook = rawLook;

            _grabInput = _topPlayer.inputHandler.grab;

            // Device change detection
            if (_topPlayer.playerInput != null)
                _topUsingMouse = _topPlayer.playerInput.currentControlScheme == "KeyboardMouse";
        }

        Vector2 combinedLook = bottomLook + topLook;


        bool anyMouse = _topUsingMouse || _bottomUsingMouse;

        if (anyMouse)
            combinedLook *= MouseLookSensitivity;       
        else
            combinedLook *= GamepadLookSensitivity * Time.deltaTime; 

        _lookInput = combinedLook;

        // Debug
        /*if (_lookInput.magnitude > 0.1f)
            Debug.Log($"Merged Look = {_lookInput} (Bottom:{bottomLook}, Top:{topLook})");*/
    }

    private void Move()
    {
        float targetSpeed = _sprintInput? SprintSpeed : MoveSpeed;
        if (_moveInput == Vector2.zero) targetSpeed = 0.0f;


        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _moveInput.magnitude;

  
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
            _targetRotation =
                Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                CinemachineCameraTarget.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                _targetRotation,
                ref _rotationVelocity,
                RotationSmoothTime
            );

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

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private Outline currentOutline;

    private void DetectObject()
    {
        if (pickedUp)
        {
            print("Object picked Up");
            return;
        }

        ObjectGrabbable lookAtobject = null;

        if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycastHit, pickUpDistance, pickUpLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out ObjectGrabbable grabbable))
            {
                lookAtobject = grabbable;

                Outline newOutline = raycastHit.transform.GetComponent<Outline>();

   
                if (currentOutline != newOutline)
                {

                    if (currentOutline != null)
                        currentOutline.enabled = false;


                    currentOutline = newOutline;
                    currentOutline.enabled = true;
                }

                return;
            }
        }

        lookAtobject = null;

        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }

    private void Grab()
    {
        if (_topPlayer?.inputHandler == null) return;

        if (_topPlayer.inputHandler.GrabAction.WasPressedThisFrame())
        {
            if (objectGrabbable == null)
            {
                if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycastHit, pickUpDistance, pickUpLayerMask))
                {
                    if (raycastHit.transform.TryGetComponent(out objectGrabbable))
                    {
                        objectGrabbable.Grab(objectGrabPointTransform);
                        pickedUp = true;
                    }
                }
            }
            else
            {
                
                objectGrabbable.Drop();
                objectGrabbable = null;
            }
        }
    }





    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_verticalVelocity < 0.0f)
                _verticalVelocity = -2f;

            if (_jumpInput && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
                
            }
            else
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                }
                
            }

            if (_jumpTimeoutDelta >= 0.0f)
                _jumpTimeoutDelta -= Time.deltaTime;

            
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, false);
            }


        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
                
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
            }
            
        }

        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void CameraRotation()
    {
        if (CinemachineCameraTarget == null) return;

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


        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);


        if (_cinemachineTargetYaw < -360f) _cinemachineTargetYaw += 360f;
        if (_cinemachineTargetYaw > 360f) _cinemachineTargetYaw -= 360f;

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

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }
}