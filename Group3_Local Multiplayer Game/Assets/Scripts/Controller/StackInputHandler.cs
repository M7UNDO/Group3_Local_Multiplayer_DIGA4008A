using UnityEngine;
using UnityEngine.InputSystem;

public class StackedInputHandler : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool interact;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        // This will be used if we want the stacked character to have its own input actions
        // For now, we're routing input through the individual players' handlers
    }

    // These methods can be called by the individual players' input handlers
    public void SetMoveInput(Vector2 moveInput)
    {
        move = moveInput;
    }

    public void SetLookInput(Vector2 lookInput)
    {
        if (cursorInputForLook)
            look = lookInput;
    }

    public void SetJumpInput(bool jumpState)
    {
        jump = jumpState;
    }

    public void SetSprintInput(bool sprintState)
    {
        sprint = sprintState;
    }

    public void SetInteractInput(bool interactState)
    {
        interact = interactState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}