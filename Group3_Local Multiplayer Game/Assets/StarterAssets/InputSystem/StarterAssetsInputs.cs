using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM

        // ===== OLD SYSTEM STYLE INPUT REFERENCES =====
        private InputActionAsset inputAsset;
        private InputActionMap playerMap;

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;


        private void Awake()
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                inputAsset = playerInput.actions;
                playerMap = inputAsset.FindActionMap("Player");

                moveAction = playerMap.FindAction("Move");
                lookAction = playerMap.FindAction("Look");
                jumpAction = playerMap.FindAction("Jump");
                sprintAction = playerMap.FindAction("Sprint");
            }
        }


        private void OnEnable()
        {
            if (playerMap == null) return;

            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMovePerformed;

            lookAction.performed += OnLookPerformed;
            lookAction.canceled += OnLookPerformed;

            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;

            sprintAction.performed += OnSprintPerformed;
            sprintAction.canceled += OnSprintCanceled;

            playerMap.Enable();
        }


        private void OnDisable()
        {
            if (playerMap == null) return;

            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMovePerformed;

            lookAction.performed -= OnLookPerformed;
            lookAction.canceled -= OnLookPerformed;

            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;

            sprintAction.performed -= OnSprintPerformed;
            sprintAction.canceled -= OnSprintCanceled;

            playerMap.Disable();
        }


        // ===== ACTION CALLBACKS =====

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

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            SprintInput(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext ctx)
        {
            SprintInput(false);
        }


        // ===== ORIGINAL STARTER ASSETS API (UNCHANGED) =====

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

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

#endif


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

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
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
}