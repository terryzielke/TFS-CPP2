using System;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : Singleton<InputManager>, InputSystem_Actions.IPlayerActions
{
    InputSystem_Actions input;

    public Action<Vector2> OnMoveEvent;
    public Action<bool> OnJumpEvent;
    public Action<float> OnStrafeEvent;

    private void OnEnable()
    {
        input = new InputSystem_Actions();

        input.Player.AddCallbacks(this);

        input.Enable();
    }

    private void OnDisable()
    {
        if (input == null) return;

        input.Disable();

        input.Player.RemoveCallbacks(this);
    }

    void OnDestroy()
    {
        if (input != null)
        {
            input.Dispose();
        }
    }

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
        }
        else if (context.canceled)
        {
            OnMoveEvent?.Invoke(Vector2.zero);
        }
    }

    public void OnStrafe(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            OnStrafeEvent?.Invoke(context.ReadValue<float>());
        }
        else if (context.canceled)
        {
            OnStrafeEvent?.Invoke(0f);
        }
    }

    public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnCrouch(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context) => OnJumpEvent?.Invoke(context.ReadValueAsButton());

    public void OnPrevious(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnNext(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }

    public void OnSprint(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new NotImplementedException();
    }
}
