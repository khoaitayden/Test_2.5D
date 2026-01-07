using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerClimbing : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerGroundedChecker groundedChecker; // To check if grounded after dismount

    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [Tooltip("How long the smooth transition ONTO the ladder takes.")]
    [SerializeField] private float ladderEntryDuration = 0.3f;
    [Tooltip("How far from the top of the ladder the player will stop climbing.")]
    [SerializeField] private float climbTopOffset = 0.5f;
    [Tooltip("The UPWARD force when jumping off the top.")]
    [SerializeField] private float topDismountUpwardForce = 10f;
    [Tooltip("The BACKWARD force to clear the ladder when jumping off the top.")]
    [SerializeField] private float topDismountBackwardForce = 3f;

    // Internal State
    private Ladder nearbyLadder;
    private float _climbCooldownTimer = 0f;
    private bool isClimbing;
    private bool isEnteringLadder;

    // Public API for PlayerController to read
    public bool IsClimbing => isClimbing;
    public bool IsEnteringLadder => isEnteringLadder;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Auto-stop climbing if ladder is gone
        if (isClimbing && nearbyLadder == null) StopClimbing();
    }

    public void SetLadderNearby(Ladder ladder)
    {
        nearbyLadder = ladder;
    }

    public void ClearLadderNearby()
    {
        nearbyLadder = null;
        if (isClimbing) StopClimbing();
    }

    public void TryStartClimbing(bool isMovementLocked)
    {
        if (isMovementLocked || isEnteringLadder) return;
        if (!isClimbing && nearbyLadder != null && Time.time > _climbCooldownTimer)
        {
            if (InputManager.Instance.MoveInput.y > 0.1f)
            {
                StartCoroutine(EnterLadderRoutine());
            }
        }
    }

    public void HandleClimbingInput()
    {
        if (!isClimbing || nearbyLadder == null) return;

        float feetY = transform.position.y - (controller.height * 0.5f);
        float stopYPosition = nearbyLadder.GetLadderTopY() - climbTopOffset;
        bool isAtTopPerch = (feetY >= stopYPosition);

        // Dismount by jumping from top
        if (isAtTopPerch && InputManager.Instance.IsJumpHeld)
        {
            StopClimbing();
            // Apply dismount forces via PlayerMovement/GroundedChecker
            // This requires PlayerController to pass these values on.
            // For now, let's keep it simple: PlayerController will re-enable normal movement.
            return;
        }

        float verticalInput = InputManager.Instance.MoveInput.y;

        // Prevent moving up past the top perch
        if (isAtTopPerch && verticalInput > 0)
        {
            verticalInput = 0;
        }

        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            controller.Move(Vector3.up * verticalInput * climbSpeed * Time.deltaTime);
        }

        // Dismount by moving down past bottom
        if (verticalInput < 0 && (controller.collisionFlags & CollisionFlags.Below) != 0)
        {
            StopClimbing();
        }
    }

    private IEnumerator EnterLadderRoutine()
    {
        isEnteringLadder = true;
        isClimbing = false;
        // PlayerController will handle stopping horizontal/vertical movement from other components

        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 ladderPos = nearbyLadder.transform.position;
        Vector3 targetPos = new Vector3(ladderPos.x, transform.position.y, ladderPos.z);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);

        while (elapsedTime < ladderEntryDuration)
        {
            if (nearbyLadder == null) { isEnteringLadder = false; yield break; }
            float t = elapsedTime / ladderEntryDuration;
            controller.enabled = false; // Disable to teleport safely
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            controller.enabled = true;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isEnteringLadder = false;
        isClimbing = true;
    }

    private void StopClimbing()
    {
        isClimbing = false;
        isEnteringLadder = false;
        _climbCooldownTimer = Time.time + 0.3f; // Prevents re-entering immediately
    }
}