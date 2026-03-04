using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SpineBalanceController : MonoBehaviour
{
    [Header("Rig References")]
    public Rig balanceRig;              // Main rig
    public MultiRotationConstraint spineConstraint; // Constraint on spine

    [Header("Spine Settings")]
    public float maxTiltAngle = 30f;

    [Header("Smoothing Settings")]
    public float tiltSmoothTime = 0.15f;      // How quickly tilt changes
    public float inputSmoothTime = 0.1f;       // How quickly input is smoothed
    public float returnToCenterSpeed = 3f;     // How fast it returns when no input
    public float anticipationSpeed = 2f;       // Slight lean before movement starts

    [Header("Balance Response")]
    public float balanceStrength = 25f;
    public float inputSensitivity = 0.02f;

    [Header("Movement Impact")]
    public AnimationCurve tiltBySpeed = AnimationCurve.EaseInOut(0, 0, 10, 30); // Speed to tilt mapping
    public float forwardTiltMultiplier = 3f;
    public float strafeTiltMultiplier = 2f;

    [Header("Natural Movement")]
    public float overshootAmount = 0.2f;        // Adds a slight overshoot for natural feel
    public float wobbleAmount = 0.5f;           // Small natural wobble
    public float wobbleSpeed = 2f;               // Speed of natural wobble

    [Header("Current State - Debug")]
    [SerializeField] private float currentTilt = 0f;
    [SerializeField] private Vector2 currentBalanceInput = Vector2.zero;
    [SerializeField] private Vector2 smoothBalanceInput = Vector2.zero;
    [SerializeField] private float movementTilt = 0f;
    [SerializeField] private float targetTilt = 0f;
    [SerializeField] private Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private bool hasInput = false;

    // Component references
    private StackedController _stackedController;
    private CharacterController _controller;
    private Vector3 _lastPosition;
    private StackManager.PlayerStackInfo _topPlayer;

    // For smooth damping
    private float _tiltVelocity;
    private Vector2 _inputVelocity;
    private float _lastMovementTilt;
    private float _overshootVelocity;
    private float _currentOvershoot;

    // For natural wobble
    private float _wobblePhase;

    // Store the original offset
    private Vector3 _originalOffset;

    private void Start()
    {
        _stackedController = GetComponent<StackedController>();
        _controller = GetComponent<CharacterController>();
        _lastPosition = transform.position;
        _wobblePhase = Random.Range(0f, Mathf.PI * 2f); // Random start phase

        // Store original offset if constraint exists
        if (spineConstraint != null)
        {
            _originalOffset = spineConstraint.data.offset;
            Debug.Log($"Spine constraint found: {spineConstraint.name}");
        }
        else
        {
            Debug.LogError("Spine constraint not assigned! Please assign the MultiRotationConstraint for the spine.");
        }

        // Initialize rig if present
        if (balanceRig != null)
        {
            balanceRig.weight = 1f;
            Debug.Log("Balance rig initialized with weight 1");
        }
    }

    private void Update()
    {
        if (PauseScript.IsGamePaused) return;
        if (!StackedController.canMove) return;

        CalculateVelocity();
        GetBalanceInput();
        CalculateTargetTilt();
        CalculateNaturalWobble();
    }

    private void LateUpdate()
    {
        // Apply rotation in LateUpdate to override animation
        ApplySpineRotation();
    }

    private void CalculateVelocity()
    {
        if (_controller != null && _controller.enabled)
        {
            currentVelocity = _controller.velocity;
        }
        else
        {
            // Fallback if controller is disabled
            Vector3 currentPosition = transform.position;
            currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;
        }
    }

    private void GetBalanceInput()
    {
        if (_topPlayer?.inputHandler != null)
        {
            Vector2 rawBalance = _topPlayer.inputHandler.balance;

            // Check if we're getting input (using a small threshold)
            if (rawBalance.magnitude > 0.01f)
            {
                hasInput = true;

                // Apply sensitivity and clamp to -1..1 range
                Vector2 processedInput = new Vector2(
                    Mathf.Clamp(rawBalance.x * inputSensitivity, -1f, 1f),
                    Mathf.Clamp(rawBalance.y * inputSensitivity, -1f, 1f)
                );

                // Smooth the input using damped spring
                smoothBalanceInput = Vector2.SmoothDamp(
                    smoothBalanceInput,
                    processedInput,
                    ref _inputVelocity,
                    inputSmoothTime
                );

                currentBalanceInput = smoothBalanceInput;
            }
            else
            {
                hasInput = false;
                // Gradually return to zero when no input
                smoothBalanceInput = Vector2.Lerp(smoothBalanceInput, Vector2.zero, Time.deltaTime * returnToCenterSpeed);
                currentBalanceInput = smoothBalanceInput;
            }
        }
    }

    private void CalculateTargetTilt()
    {
        // Convert velocity to local space
        Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);
        float speed = currentVelocity.magnitude;

        // Use animation curve for more natural speed-to-tilt mapping
        float speedTiltFactor = tiltBySpeed.Evaluate(speed) / 30f; // Normalize to our max

        // MOVEMENT IMPACT with anticipation
        float targetMovementTilt = -localVelocity.z * forwardTiltMultiplier * speedTiltFactor;
        targetMovementTilt += Mathf.Abs(localVelocity.x) * strafeTiltMultiplier * 0.3f * Mathf.Sign(localVelocity.x) * speedTiltFactor;

        // Add anticipation - slight lean before movement starts
        if (speed < 0.5f && _stackedController != null)
        {
            Vector3 inputDir = _stackedController.GetMovementDirection();
            if (inputDir.magnitude > 0.1f)
            {
                // Lean slightly in the direction of input before moving
                float anticipation = inputDir.y * anticipationSpeed * Time.deltaTime;
                targetMovementTilt = Mathf.Lerp(targetMovementTilt, -inputDir.y * 5f, anticipation);
            }
        }

        // Smooth the movement tilt
        movementTilt = Mathf.Lerp(movementTilt, targetMovementTilt, Time.deltaTime * 5f);

        // BALANCE CORRECTION with easing
        float balanceCorrection = currentBalanceInput.y * balanceStrength;

        // Calculate target tilt
        float rawTargetTilt = movementTilt - balanceCorrection;
        rawTargetTilt = Mathf.Clamp(rawTargetTilt, -maxTiltAngle, maxTiltAngle);

        // Add slight overshoot for natural feel (like a real person balancing)
        float overshootTarget = rawTargetTilt + (rawTargetTilt - _lastMovementTilt) * overshootAmount;
        _lastMovementTilt = rawTargetTilt;

        // Smooth the target with damped spring (more natural than linear interpolation)
        targetTilt = Mathf.SmoothDamp(targetTilt, overshootTarget, ref _tiltVelocity, tiltSmoothTime);

        // Clamp final target
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);

        // Smooth current tilt (extra layer of smoothing)
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 8f);
    }

    private void CalculateNaturalWobble()
    {
        // Add a tiny natural wobble when balancing (like a real person)
        _wobblePhase += Time.deltaTime * wobbleSpeed;

        // Only wobble when actively balancing (has input or moving)
        if (hasInput || currentVelocity.magnitude > 0.1f)
        {
            // Small amplitude wobble
            float wobble = Mathf.Sin(_wobblePhase) * wobbleAmount * 0.1f;
            currentTilt += wobble * Time.deltaTime;
        }
    }

    private void ApplySpineRotation()
    {
        if (spineConstraint == null) return;

        // Create a rotation offset with smooth interpolation
        Vector3 rotationOffset = new Vector3(
            currentTilt,                    // Forward/back tilt
            0f,                              // No yaw (twist)
            -currentBalanceInput.x * 12f     // Side tilt from horizontal balance
        );

        // Add subtle secondary motion
        if (Mathf.Abs(currentTilt) > maxTiltAngle * 0.3f)
        {
            // Add a little extra wobble when near the limit (struggling to balance)
            float struggleWobble = Mathf.Sin(Time.time * 15f) * (Mathf.Abs(currentTilt) / maxTiltAngle) * 2f;
            rotationOffset.z += struggleWobble;
            rotationOffset.x += struggleWobble * 0.3f;
        }

        // Smoothly interpolate the offset for extra smoothness
        Vector3 currentOffset = spineConstraint.data.offset;
        Vector3 newOffset = Vector3.Lerp(currentOffset, rotationOffset, Time.deltaTime * 10f);

        // Apply to constraint
        spineConstraint.data.offset = newOffset;

        // Ensure the constraint is active
        if (balanceRig != null && balanceRig.weight < 0.9f)
        {
            balanceRig.weight = Mathf.Lerp(balanceRig.weight, 1f, Time.deltaTime * 5f);
        }

        // Visual debug
        if (spineConstraint.data.constrainedObject != null)
        {
            Transform spine = spineConstraint.data.constrainedObject;
            Debug.DrawRay(spine.position, spine.forward * 2f, Color.blue);
            Debug.DrawRay(spine.position, spine.up * 2f, Color.green);
        }
    }

    public void SetPlayers(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _topPlayer = top;
        Debug.Log($"SpineBalance: Top player set. Player has input handler: {top?.inputHandler != null}");
    }


}