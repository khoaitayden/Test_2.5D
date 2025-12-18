using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour, PlayerInput.IPlayerActions
{
    public static InputManager Instance { get; private set; }

    [Header("Interaction Settings")]
    [Tooltip("How long (seconds) to hold F to turn the light on/off")]
    public float lightToggleHoldDuration = 0.5f;

    private PlayerInput _playerInput;

    // --- Data Properties ---
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsSlowWalking { get; private set; }
    public bool IsJumpHeld { get; private set; }
    
    // --- NEW: Property for holding the Interact key ---
    public bool IsInteractHeld { get; private set; }

    // --- Events ---
    public event Action OnJumpTriggered;
    public event Action OnJumpReleased;
    public event Action OnInteractTriggered; 
    
    // Light Events
    public event Action OnWispCycleTriggered;
    public event Action OnWispPowerToggleTriggered;

    // Internal State for "Hold" logic
    private bool _isWispSwitchDown;
    private float _wispSwitchStartTime;
    private bool _wispHoldEventFired;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        _playerInput = new PlayerInput();
        _playerInput.Player.SetCallbacks(this);
    }

    private void OnEnable() => _playerInput.Player.Enable();
    private void OnDisable() => _playerInput.Player.Disable();

    private void Update()
    {
        if (_isWispSwitchDown && !_wispHoldEventFired)
        {
            if (Time.time - _wispSwitchStartTime >= lightToggleHoldDuration)
            {
                OnWispPowerToggleTriggered?.Invoke();
                _wispHoldEventFired = true;
            }
        }
    }

    // --- IPlayerActions Implementation ---

    public void OnMovement(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    public void OnSlowWalk(InputAction.CallbackContext context) => IsSlowWalking = context.ReadValueAsButton();
    public void OnSprint(InputAction.CallbackContext context) => IsSprinting = context.ReadValueAsButton();

    public void OnInteract(InputAction.CallbackContext context)
    {
        // --- MODIFIED: Set the hold state AND fire the press event ---
        IsInteractHeld = context.ReadValueAsButton();

        if (context.performed)
        {
            OnInteractTriggered?.Invoke(); // For single-press actions like doors
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) { IsJumpHeld = true; OnJumpTriggered?.Invoke(); }
        else if (context.canceled) { IsJumpHeld = false; OnJumpReleased?.Invoke(); }
    }

    public void OnWispSwitch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _isWispSwitchDown = true;
            _wispSwitchStartTime = Time.time;
            _wispHoldEventFired = false;
        }
        else if (context.canceled)
        {
            _isWispSwitchDown = false;
            if (!_wispHoldEventFired) OnWispCycleTriggered?.Invoke();
        }
    }
}