using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour, PlayerInput.IPlayerActions
{
    public static InputManager Instance { get; private set; }

    private PlayerInput _playerInput;

    // --- Data Properties (Polled in Update) ---
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsSlowWalking { get; private set; }
    public bool IsJumpHeld { get; private set; }

    // --- Events (Subscribed to by other scripts) ---
    public event Action OnJumpTriggered;   // Happens frame button is pressed
    public event Action OnJumpReleased;    // Happens frame button is released
    public event Action OnWispSwitchTriggered; // The new 'F' key action

    private void Awake()
    {
        // Singleton Setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            return;
        }

        _playerInput = new PlayerInput();
        // Register this class to handle the callbacks defined in the Input Asset
        _playerInput.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        _playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Player.Disable();
    }

    // --- Interface Implementation (IPlayerActions) ---

    public void OnMovement(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnSlowWalk(InputAction.CallbackContext context)
    {
        IsSlowWalking = context.ReadValueAsButton();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        IsSprinting = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsJumpHeld = true;
            OnJumpTriggered?.Invoke();
        }
        else if (context.canceled)
        {
            IsJumpHeld = false;
            OnJumpReleased?.Invoke();
        }
    }

    public void OnWispSwitch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnWispSwitchTriggered?.Invoke();
        }
    }
}