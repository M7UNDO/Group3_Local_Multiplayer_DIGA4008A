using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SpineBalanceController : MonoBehaviour
{
    [Header("Rig References")]
    public Rig balanceRig;
    public MultiRotationConstraint spineConstraint;

    [Header("Spine Settings")]
    public float maxTiltAngle = 40f;
    public float criticalTiltAngle = 30f;

    [Header("Balance Response")]
    public float balanceStrength = 30f;
    public float inputSensitivity = 0.02f;

    [Header("Movement Impact")]
    [Tooltip("Tilt from forward movement")]
    public float forwardTiltMultiplier = 4f;
    [Tooltip("Tilt from backward movement")] public float backwardTiltMultiplier = 3f;   
    public float strafeTiltMultiplier = 2f;

    [Header("Momentum & Inertia")]
    [Tooltip("How much momentum affects tilt")]
    public float momentumTiltMultiplier = 2f;
    [Tooltip("Tilt when stopping suddenly")]
    public float decelerationTiltMultiplier = 3f; 
    [Tooltip("How smoothly momentum builds/fades")]
    public float inertiaSmoothTime = 0.3f;

    [Header("Smoothing")]
    public float tiltSmoothTime = 0.15f;
    public float inputSmoothTime = 0.1f;

    [Header("Fail State")]
    public float unstackDelay = 2.5f;
    public float warningTime = 1.5f;
    public bool unstackOnFail = true;

    [Header("Current State - Debug")]
    [SerializeField] private float currentTilt = 0f;
    [SerializeField] private float targetTilt = 0f;
    [SerializeField] private Vector2 currentBalanceInput = Vector2.zero;
    [SerializeField] private Vector2 smoothBalanceInput = Vector2.zero;
    [SerializeField] private Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private Vector3 previousVelocity = Vector3.zero;
    [SerializeField] private Vector3 acceleration = Vector3.zero;
    [SerializeField] private Vector2 moveInput = Vector2.zero;
    [SerializeField] private float movementTilt = 0f;
    [SerializeField] private float momentumTilt = 0f;
    [SerializeField] private float balanceCorrection = 0f;

    [Header("Fail State - Debug")]
    [SerializeField] private bool isInCriticalZone = false;
    [SerializeField] private float timeInCriticalZone = 0f;
    [SerializeField] private float unstackTimer = 0f;
    [SerializeField] private bool isUnstacking = false;


    private StackedController _stackedController;
    private CharacterController _controller;
    private Vector3 _lastPosition;
    private StackManager.PlayerStackInfo _topPlayer;
    private StackManager _stackManager;


    private float _tiltVelocity;
    private float _momentumTiltVelocity;
    private Vector2 _inputVelocity;

    // Events
    public System.Action<float> OnBalanceChanged;
    public System.Action OnEnterCriticalZone;
    public System.Action OnExitCriticalZone;
    public System.Action OnUnstackStarted;

    private void Start()
    {
        _stackedController = GetComponent<StackedController>();
        _controller = GetComponent<CharacterController>();
        _stackManager = StackManager.Instance;
        _lastPosition = transform.position;

        if (spineConstraint == null)
        {
            Debug.LogError("Spine constraint not assigned!");
        }

        if (balanceRig != null)
        {
            balanceRig.weight = 1f;
        }
    }

    private void Update()
    {
        if (PauseScript.IsGamePaused) return;
        if (!StackedController.canMove || isUnstacking) return;

        CalculateVelocity();
        CalculateAcceleration();
        GetInputs();
        CalculateTargetTilt();
        CheckBalanceState();
    }

    private void LateUpdate()
    {
        if (isUnstacking) return;

        ApplySpineRotation();

        OnBalanceChanged?.Invoke(GetBalancePercentage());
    }

    private void CalculateVelocity()
    {
        previousVelocity = currentVelocity;

        if (_controller != null && _controller.enabled)
        {
            currentVelocity = _controller.velocity;
        }
        else
        {
            Vector3 currentPosition = transform.position;
            currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;
        }
    }

    private void CalculateAcceleration()
    {
        // Calculate how quickly we're speeding up or slowing down
        acceleration = (currentVelocity - previousVelocity) / Time.deltaTime;
    }

    private void GetInputs()
    {
        // Get moveInput from StackedController
        if (_stackedController != null)
        {
            moveInput = _stackedController.GetMovementDirection();
        }

        // Get top player's balance input
        if (_topPlayer?.inputHandler != null)
        {
            Vector2 rawBalance = _topPlayer.inputHandler.balance;

            if (rawBalance.magnitude > 0.01f)
            {
                Vector2 processedInput = new Vector2(
                    Mathf.Clamp(rawBalance.x * inputSensitivity, -1f, 1f),
                    Mathf.Clamp(rawBalance.y * inputSensitivity, -1f, 1f)
                );

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
                smoothBalanceInput = Vector2.Lerp(smoothBalanceInput, Vector2.zero, Time.deltaTime * 3f);
                currentBalanceInput = smoothBalanceInput;
            }
        }
    }

    private void CalculateTargetTilt()
    {
        // Convert velocities to local space
        Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);
        Vector3 localAcceleration = transform.InverseTransformDirection(acceleration);

        //(immediate response to controls)
        float inputTilt = 0f;

        if (Mathf.Abs(moveInput.y) > 0.1f)
        {
            if (moveInput.y > 0) // Moving forward
            {
                inputTilt += -moveInput.y * forwardTiltMultiplier;
            }
            else // Moving backward
            {
                inputTilt += -moveInput.y * backwardTiltMultiplier;
            }
        }

        // Strafe movement
        inputTilt += Mathf.Abs(moveInput.x) * strafeTiltMultiplier * 0.5f * Mathf.Sign(moveInput.x);


        float momentumFromVelocity = -localVelocity.z * momentumTiltMultiplier;

        float accelerationTilt = 0f;

 
        if (localAcceleration.z > 0.1f)
        {
            accelerationTilt += -localAcceleration.z * decelerationTiltMultiplier * 0.5f;
        }

        else if (localAcceleration.z < -0.1f)
        {
            accelerationTilt += -localAcceleration.z * decelerationTiltMultiplier; 
            //Debug.Log($"Decelerating! Lean forward: {accelerationTilt}");
        }


        if (Mathf.Abs(previousVelocity.z) > 2f && Mathf.Abs(currentVelocity.z) < 0.5f)
        {
            // Sudden stop - lurch forward
            accelerationTilt += 5f;
            //Debug.Log("Sudden stop! Lurching forward!");
        }

        float totalMovementTilt = inputTilt + momentumFromVelocity + accelerationTilt;

    
        movementTilt = Mathf.Lerp(movementTilt, totalMovementTilt, Time.deltaTime * 5f);
        movementTilt = Mathf.Clamp(movementTilt, -maxTiltAngle, maxTiltAngle);

        
        balanceCorrection = currentBalanceInput.y * balanceStrength;

        // Calculate raw target
        float rawTargetTilt = movementTilt - balanceCorrection;
        rawTargetTilt = Mathf.Clamp(rawTargetTilt, -maxTiltAngle, maxTiltAngle);

        // Smooth the target
        targetTilt = Mathf.SmoothDamp(targetTilt, rawTargetTilt, ref _tiltVelocity, tiltSmoothTime);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 10f);

  
        //OnBalanceChanged?.Invoke(GetBalancePercentage());

 
        if (Mathf.Abs(accelerationTilt) > 1f || Mathf.Abs(momentumFromVelocity) > 1f)
        {
            //Debug.Log($"Tilt Sources - Input: {inputTilt:F1}, Momentum: {momentumFromVelocity:F1}, Acceleration: {accelerationTilt:F1}, Total: {totalMovementTilt:F1}");
        }
    }

    

    private void CheckBalanceState()
    {
        float tiltMagnitude = Mathf.Abs(currentTilt);

        
        bool wasInCriticalZone = isInCriticalZone;
        isInCriticalZone = tiltMagnitude > criticalTiltAngle;

        if (isInCriticalZone && !wasInCriticalZone)
        {
            EnterCriticalZone();
        }
        else if (!isInCriticalZone && wasInCriticalZone)
        {
            ExitCriticalZone();
        }

        
        if (isInCriticalZone)
        {
            timeInCriticalZone += Time.deltaTime;

            if (timeInCriticalZone > warningTime && !isUnstacking)
            {
                unstackTimer += Time.deltaTime;

                if (unstackTimer >= unstackDelay - warningTime)
                {
                    StartUnstack();
                }
            }
        }
        else
        {
            timeInCriticalZone = 0f;
            unstackTimer = 0f;
        }
    }

    private void EnterCriticalZone()
    {
        Debug.Log("ENTERING CRITICAL BALANCE ZONE!");
        OnEnterCriticalZone?.Invoke();
    }

    private void ExitCriticalZone()
    {
        //Debug.Log("EXITING CRITICAL BALANCE ZONE!");
        OnExitCriticalZone?.Invoke();
    }

    private void StartUnstack()
    {
        if (isUnstacking || !unstackOnFail) return;

        isUnstacking = true;
        //Debug.Log("Woahh Falling");

        OnUnstackStarted?.Invoke();

        if (_stackManager != null && _stackManager.stackActive)
        {
            StartCoroutine(UnstackAfterDelay(0.5f));
        }
    }

    private System.Collections.IEnumerator UnstackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_stackManager != null)
        {
            _stackManager.Unstack();
        }

        isUnstacking = false;
    }

    private void ApplySpineRotation()
    {
        if (spineConstraint == null) return;

        Vector3 rotationOffset = new Vector3(
            currentTilt,
            0f,
            -currentBalanceInput.x * 12f
        );

        // Add wobble when in critical zone
        if (isInCriticalZone)
        {
            float struggleIntensity = (timeInCriticalZone / warningTime) * 3f;
            float wobble = Mathf.Sin(Time.time * 20f) * struggleIntensity;
            rotationOffset.z += wobble;
            rotationOffset.x += wobble * 0.3f;
        }

        Vector3 currentOffset = spineConstraint.data.offset;
        Vector3 newOffset = Vector3.Lerp(currentOffset, rotationOffset, Time.deltaTime * 15f);
        spineConstraint.data.offset = newOffset;
    }

    public void SetPlayers(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _topPlayer = top;
    }

    public float GetBalancePercentage()
    {
        return 1f - (Mathf.Abs(currentTilt) / maxTiltAngle);
    }

    public float GetTimeUntilUnstack()
    {
        if (!isInCriticalZone) return unstackDelay;
        return Mathf.Max(0, unstackDelay - (timeInCriticalZone + unstackTimer));
    }

    public bool IsInCriticalZone()
    {
        return isInCriticalZone;
    }
}