/*using UnityEngine;

public class SimpleSpineBalance : MonoBehaviour
{
    [Header("Spine Bones")]
    public Transform spineBone;      
    public Transform chestBone;        

    [Header("Balance Settings")]
    public float maxTiltAngle = 30f;
    public float tiltSpeed = 5f;
    public float balanceStrength = 20f;

    [Header("Movement Impact")]
    public float forwardTiltMultiplier = 2f;
    public float strafeTiltMultiplier = 1.5f;

    [Header("Current State")]
    [SerializeField] private float currentTilt = 0f;
    [SerializeField] private Vector2 currentBalanceInput = Vector2.zero;

    // Private variables
    private StackedController _stackedController;
    private Vector3 _lastPosition;
    private Vector3 _velocity;
    private StackManager.PlayerStackInfo _topPlayer;

    // Store original rotations
    private Quaternion _originalSpineRotation;
    private Quaternion _originalChestRotation;

    private float _targetTilt;
    private float _tiltVelocity;

    private void Start()
    {
        _stackedController = GetComponent<StackedController>();
        _lastPosition = transform.position;

        // Store original rotations
        if (spineBone != null)
        {
            _originalSpineRotation = spineBone.localRotation;
        }

        if (chestBone != null)
        {
            _originalChestRotation = chestBone.localRotation;
        }
    }

    private void Update()
    {
        if (PauseScript.IsGamePaused) return;
        if (!StackedController.canMove) return;

        CalculateVelocity();
        GetBalanceInput();
        CalculateTargetTilt();
    }

    private void LateUpdate()
    {
        // Apply rotation in LateUpdate to override the animation
        ApplySpineRotation();
    }

    private void CalculateVelocity()
    {
        Vector3 currentPosition = transform.position;
        _velocity = (currentPosition - _lastPosition) / Time.deltaTime;
        _lastPosition = currentPosition;
    }

    private void GetBalanceInput()
    {
        if (_topPlayer?.inputHandler != null)
        {
            Vector2 rawBalance = _topPlayer.inputHandler.balance;

            // Adjust sensitivity here
            currentBalanceInput = new Vector2(
                Mathf.Clamp(rawBalance.x * 0.01f, -1f, 1f),
                Mathf.Clamp(rawBalance.y * 0.01f, -1f, 1f)
            );

            // Debug to see if input is coming through
            if (rawBalance.magnitude > 0.1f)
            {
                Debug.Log($"Balance Input: {rawBalance} -> Normalized: {currentBalanceInput}");
            }
        }
    }

    private void CalculateTargetTilt()
    {
        // Convert velocity to local space
        Vector3 localVelocity = transform.InverseTransformDirection(_velocity);

        // Movement creates tilt (negative = lean back when moving forward)
        float movementTilt = -localVelocity.z * forwardTiltMultiplier;

        // Add some side tilt from strafing
        movementTilt += localVelocity.x * strafeTiltMultiplier * 0.5f;

        // Balance input counters the tilt
        // Y axis = forward/back balance
        // X axis = side balance
        float balanceCorrection = currentBalanceInput.y * balanceStrength;

        // Calculate final target
        _targetTilt = movementTilt - balanceCorrection;
        _targetTilt = Mathf.Clamp(_targetTilt, -maxTiltAngle, maxTiltAngle);

        // Smooth the tilt
        currentTilt = Mathf.SmoothDamp(currentTilt, _targetTilt, ref _tiltVelocity, tiltSpeed * Time.deltaTime);
    }

    private void ApplySpineRotation()
    {
        if (spineBone == null)
        {
            Debug.LogError("Spine bone not assigned!");
            return;
        }

        // Create tilt rotation
        // X axis = forward/back tilt
        // Z axis = side tilt from horizontal balance
        Quaternion tiltRotation = Quaternion.Euler(
            currentTilt,          
            0f,                              
            -currentBalanceInput.x * 10f     
        );


        spineBone.localRotation = _originalSpineRotation * tiltRotation;


        if (chestBone != null)
        {

            Quaternion chestTilt = Quaternion.Euler(
                currentTilt * 0.7f,
                0f,
                -currentBalanceInput.x * 7f
            );
            chestBone.localRotation = _originalChestRotation * chestTilt;
        }
    }

    public void SetPlayers(StackManager.PlayerStackInfo bottom, StackManager.PlayerStackInfo top)
    {
        _topPlayer = top;
    }


    private void OnGUI()
    {

        GUI.Label(new Rect(10, 10, 300, 20), $"Balance Input: {currentBalanceInput}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Current Tilt: {currentTilt:F2}°");
        GUI.Label(new Rect(10, 50, 300, 20), $"Target Tilt: {_targetTilt:F2}°");
        GUI.Label(new Rect(10, 70, 300, 20), $"Velocity: {_velocity}");
    }
}*/