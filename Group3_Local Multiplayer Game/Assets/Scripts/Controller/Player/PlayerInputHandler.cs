using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool crouch;
    public bool stack;
    public bool grab;
    public Vector2 balance;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;


    private InputActionAsset inputAsset;
    private InputActionMap playerMap;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction stackAction;
    private InputAction grabAction;
    private InputAction balanceAction;

    private void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            inputAsset = playerInput.actions;
            playerMap = inputAsset.FindActionMap("Player");

            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            balanceAction = playerMap.FindAction("Balance");
            jumpAction = playerMap.FindAction("Jump");
            sprintAction = playerMap.FindAction("Sprint");
            crouchAction = playerMap.FindAction("Crouch");
            stackAction = playerMap.FindAction("Stack");
            grabAction = playerMap.FindAction("Grab");
        }
    }


    private void OnEnable()
    {
        if (playerMap == null) return;

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMovePerformed;

        lookAction.performed += OnLookPerformed;
        lookAction.canceled += OnLookPerformed;

        balanceAction.performed += OnBalancePerformed;
        balanceAction.canceled += OnBalancePerformed;

        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;

        grabAction.performed += OnGrabPerformed;

        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;

        crouchAction.performed += OnCrouchPerformed;
        crouchAction.canceled += OnCrouchCanceled;

        stackAction.performed += OnStackPerformed;

        playerMap.Enable();
    }

    

    private void OnDisable()
    {
        if (playerMap == null) return;

        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMovePerformed;

        lookAction.performed -= OnLookPerformed;
        lookAction.canceled -= OnLookPerformed;

        balanceAction.performed -= OnBalancePerformed;
        balanceAction.canceled -= OnBalancePerformed;

        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;

        sprintAction.performed -= OnSprintPerformed;
        sprintAction.canceled -= OnSprintCanceled;

        crouchAction.performed -= OnCrouchPerformed;
        crouchAction.canceled -= OnCrouchCanceled;

        grabAction.performed -= OnGrabPerformed;

        stackAction.performed -= OnStackPerformed;

        playerMap.Disable();
    }

    private void OnBalancePerformed(InputAction.CallbackContext ctx)
    {
        BalanceInput(ctx.ReadValue<Vector2>());
    }

    // NEW: Balance input method
    public void BalanceInput(Vector2 newBalanceState)
    {
        balance = newBalanceState;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        MoveInput(ctx.ReadValue<Vector2>());
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        if (cursorInputForLook)
            LookInput(ctx.ReadValue<Vector2>());
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        JumpInput(true);
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        JumpInput(false);
    }

    private void OnGrabPerformed(InputAction.CallbackContext ctx)
    {
        GrabInput(true);
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        SprintInput(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        SprintInput(false);
    }
    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        CrouchInput(true);
    }

    private void OnCrouchCanceled(InputAction.CallbackContext ctx)
    {
        CrouchInput(false);
    }

    private void OnStackPerformed(InputAction.CallbackContext ctx)
    {
        StackInput(true);
    }

    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }
    public void OnGrab(InputValue value)
    {
        GrabInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }
    public void OnCrouch(InputValue value)
    {
        CrouchInput(value.isPressed);
    }

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }
    public void GrabInput(bool newGrabState)
    {
        grab = newGrabState;
    }



    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }
    public void CrouchInput(bool newCrouchState)
    {
        crouch = newCrouchState;
    }

    public void StackInput(bool newStackState)
    {
        stack = newStackState;

        if (!stack || crouch) return;

        //if already stacked, unstack
        if (StackManager.Instance != null && StackManager.Instance.stackActive)
        {
            StackManager.Instance.Unstack();
            return;
        }


        FindAndStackWithOtherPlayer();
    }

    private void FindAndStackWithOtherPlayer()
    {
        // Get all players
        PlayerInputHandler[] allPlayers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player.gameObject != gameObject && player.isActiveAndEnabled)
            {
                // Found another player, attempt to stack
                StackManager.Instance?.AttemptStack(gameObject, player.gameObject);
                break;
            }
        }
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
