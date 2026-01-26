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

    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsSlowWalking { get; private set; }
    public bool IsJumpHeld { get; private set; }
    public bool IsFlashlightHeld { get; private set; }
    public Vector2 LookInput { get; private set; }

    public event Action OnJumpTriggered;
    public event Action OnJumpReleased;
    public event Action OnInteractTriggered; 
    
    public event Action OnWispCycleTriggered;
    public event Action OnWispPowerToggleTriggered;

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

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            MoveInput = Vector2.zero;
            IsSprinting = false;
            IsSlowWalking = false;
            IsJumpHeld = false;
            IsFlashlightHeld = false;
            _isWispSwitchDown = false;
        }
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }
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
    public void OnMovement(InputAction.CallbackContext context) 
    {
        if (context.canceled)
        {
            MoveInput = Vector2.zero;
        }
        else
        {
            MoveInput = context.ReadValue<Vector2>();
        }
    }

    public void OnSlowWalk(InputAction.CallbackContext context) => IsSlowWalking = context.ReadValueAsButton();
    public void OnSprint(InputAction.CallbackContext context) => IsSprinting = context.ReadValueAsButton();
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) OnInteractTriggered?.Invoke();
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

    public void OnFlashLight(InputAction.CallbackContext context) 
    {
        IsFlashlightHeld = context.ReadValueAsButton();
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
            if (!_wispHoldEventFired)
            {
                OnWispCycleTriggered?.Invoke();
            }
        }
    }
}
