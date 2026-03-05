using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerDeviceDetector : MonoBehaviour
{
    public InputDeviceType CurrentDevice { get; private set; }

    public event Action<InputDeviceType> OnDeviceChanged;

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        DetectDevice();
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed)
            return;

        var action = obj as InputAction;

        if (action == null || action.activeControl == null)
            return;

        var device = action.activeControl.device;

        // only detect devices belonging to THIS player
        if (!playerInput.devices.Contains(device))
            return;

        InputDeviceType newDevice = GetDeviceType(device);

        if (newDevice != CurrentDevice)
        {
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(CurrentDevice);
        }
    }

    private void DetectDevice()
    {
        if (playerInput.devices.Count == 0)
            return;

        var device = playerInput.devices[0];
        CurrentDevice = GetDeviceType(device);
    }

    private InputDeviceType GetDeviceType(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
            return InputDeviceType.KeyboardMouse;

        if (device is Gamepad gamepad)
        {
            string layout = gamepad.layout.ToLower();

            if (layout.Contains("dualshock") || layout.Contains("dualsense"))
                return InputDeviceType.PlayStation;

            return InputDeviceType.Xbox;
        }

        return InputDeviceType.KeyboardMouse;
    }
}