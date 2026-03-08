using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class StackedDeviceDetector : MonoBehaviour
{
    public InputDeviceType CurrentDevice { get; private set; }

    public event Action<InputDeviceType> OnDeviceChanged;

    private StackedController stackedController;

    private void Awake()
    {
        stackedController = GetComponent<StackedController>();
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

        // Check bottom player device
        if (stackedController._bottomPlayer?.playerInput != null)
        {
            if (stackedController._bottomPlayer.playerInput.devices.Contains(device))
            {
                SetDevice(device);
                return;
            }
        }

        // Check top player device
        if (stackedController._topPlayer?.playerInput != null)
        {
            if (stackedController._topPlayer.playerInput.devices.Contains(device))
            {
                SetDevice(device);
                return;
            }
        }
    }

    private void SetDevice(InputDevice device)
    {
        InputDeviceType newDevice = GetDeviceType(device);

        if (newDevice != CurrentDevice)
        {
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(CurrentDevice);
        }
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