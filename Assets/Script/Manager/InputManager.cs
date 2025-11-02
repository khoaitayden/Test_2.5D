// FILE TO EDIT: PlayerInputManager.cs

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class InputManager : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerInput playerInputActions;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerInputActions = new PlayerInput();
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        
        // Subscribe to events
        playerInputActions.Player.Movement.performed += OnMovementInput;
        playerInputActions.Player.Movement.canceled += OnMovementInput;

        // --- THIS IS THE FIX for JUMP HOLD ---
        playerInputActions.Player.Jump.performed += OnJumpInput;
        playerInputActions.Player.Jump.canceled += OnJumpRelease; // Subscribe to release event

        playerInputActions.Player.SlowWalk.performed += context => playerController.SetSlowWalk(true);
        playerInputActions.Player.SlowWalk.canceled += context => playerController.SetSlowWalk(false);

        playerInputActions.Player.Sprint.performed += context => playerController.SetSprint(true);
        playerInputActions.Player.Sprint.canceled += context => playerController.SetSprint(false);
    }

    private void OnDisable()
    {
        // Unsubscribe from all events
        playerInputActions.Player.Movement.performed -= OnMovementInput;
        playerInputActions.Player.Movement.canceled -= OnMovementInput;

        playerInputActions.Player.Jump.performed -= OnJumpInput;
        playerInputActions.Player.Jump.canceled -= OnJumpRelease; // Unsubscribe

        // ... (rest of OnDisable is the same as before) ...

        playerInputActions.Player.Disable();
    }
    
    private void OnMovementInput(InputAction.CallbackContext context)
    {
        playerController.SetMoveInput(context.ReadValue<Vector2>());
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        playerController.HandleJumpInput(true); // Signal jump button is PRESSED
    }

    // --- NEW METHOD ---
    private void OnJumpRelease(InputAction.CallbackContext context)
    {
        playerController.HandleJumpInput(false); // Signal jump button is RELEASED
    }
}