using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public enum InputSource { Any, KeyboardMouse, Gamepad }

    [Header("Device Assignment")]
    [SerializeField] private InputSource inputSource = InputSource.Any; // Any = every device, like the old InputManager
    [SerializeField] private int gamepadIndex = 0; // which connected gamepad to use when inputSource is Gamepad

    InputSystem_Actions input;

    public Action<Vector2> OnMoveEvent;
    public Action<bool> OnJumpEvent;
    public Action<float> OnStrafeEvent;

    private void OnEnable()
    {
        input = new InputSystem_Actions();
        input.Player.AddCallbacks(this);
        AssignDevices();
        input.Enable();
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.Disable();
        input.Player.RemoveCallbacks(this);
        input.Dispose();
        input = null;
    }

    // Limits this player's actions to their own device so other players' input is ignored
    void AssignDevices()
    {
        switch (inputSource)
        {
            case InputSource.KeyboardMouse:
                input.devices = new InputDevice[] { Keyboard.current, Mouse.current };
                break;

            case InputSource.Gamepad:
                if (gamepadIndex < Gamepad.all.Count)
                {
                    input.devices = new InputDevice[] { Gamepad.all[gamepadIndex] };
                }
                else
                {
                    Debug.LogWarning(name + " wants gamepad " + gamepadIndex + " but only " + Gamepad.all.Count + " are connected.");
                    input.devices = new InputDevice[0]; // no device rather than listening to everything
                }
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.started || context.performed) OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
        else if (context.canceled) OnMoveEvent?.Invoke(Vector2.zero);
    }

    public void OnStrafe(InputAction.CallbackContext context)
    {
        if (context.started || context.performed) OnStrafeEvent?.Invoke(context.ReadValue<float>());
        else if (context.canceled) OnStrafeEvent?.Invoke(0f);
    }

    public void OnJump(InputAction.CallbackContext context) => OnJumpEvent?.Invoke(context.ReadValueAsButton());

    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnSprint(InputAction.CallbackContext context) { }
}
