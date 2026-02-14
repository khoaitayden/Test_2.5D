using UnityEngine;

public class PlayerClimbing : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Settings")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float climbSnapSpeed = 5f; 
    
    [Header("Dismount Settings")]
    [SerializeField] private float jumpOffForceHorizontal = 4f;
    [SerializeField] private float jumpOffForceUp = 5f;

    private Ladder nearbyLadder;
    private bool isClimbing;
    private float cooldownTimer;

    public bool IsClimbing => isClimbing;
    public bool IsEnteringLadder => false; 

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    public void SetLadderNearby(Ladder ladder) => nearbyLadder = ladder;
    
    public void ClearLadderNearby()
    {
        nearbyLadder = null;
        if (isClimbing) StopClimbing();
    }

    public void TryStartClimbing(bool isMovementLocked)
    {
        if (isMovementLocked || isClimbing || nearbyLadder == null) return;
        if (Time.time < cooldownTimer) return;

        float vInput = InputManager.Instance.MoveInput.y;
        if (Mathf.Abs(vInput) > 0.1f)
        {
            isClimbing = true;
        }
    }

    public void HandleClimbingPhysics()
    {
        if (!isClimbing || nearbyLadder == null) 
        {
            StopClimbing();
            return;
        }

        RotateTowardsLadder();

        float vInput = InputManager.Instance.MoveInput.y;
        Vector3 finalVelocity = CalculateClimbingVelocity(vInput);

        finalVelocity = EnforceHeadLimit(finalVelocity, vInput);

        controller.Move(finalVelocity * Time.deltaTime);

        CheckExits(vInput);
    }


    private void RotateTowardsLadder()
    {
        Quaternion targetRot = Quaternion.LookRotation(nearbyLadder.ClimbDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    private Vector3 CalculateClimbingVelocity(float vInput)
    {
        Vector3 velocity = Vector3.zero;

        velocity.y = vInput * climbSpeed;

        Vector3 targetPos = nearbyLadder.GetClosestPointOnLadder(transform.position);
        targetPos -= nearbyLadder.ClimbDirection * (controller.radius + 0.1f);
        
        Vector3 moveDir = targetPos - transform.position;
        moveDir.y = 0;

        velocity.x = moveDir.x * climbSnapSpeed;
        velocity.z = moveDir.z * climbSnapSpeed;

        return velocity;
    }

    private Vector3 EnforceHeadLimit(Vector3 currentVelocity, float vInput)
    {

        if (vInput <= 0) return currentVelocity;

        float currentHeadY = transform.position.y + controller.center.y + (controller.height * 0.5f);
        float maxHeadY = nearbyLadder.TopY + 0.1f;

        if (currentHeadY >= maxHeadY)
        {
            currentVelocity.y = 0;

            SnapPositionToLimit(maxHeadY);
        }

        return currentVelocity;
    }

    private void SnapPositionToLimit(float maxHeadY)
    {
        float requiredY = maxHeadY - (controller.center.y + (controller.height * 0.5f));
        if (Mathf.Abs(transform.position.y - requiredY) > 0.001f)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.y = requiredY;
            transform.position = clampedPos;
        }
    }
    private void CheckExits(float vInput)
    {
        if (InputManager.Instance.IsJumpHeld)
        {
            PerformDirectionalDismount();
            return;
        }

        if (vInput < 0) 
        {
            if (groundedChecker.CheckGround())
            {
                StopClimbing();
                return;
            }
        }
    }

    private void PerformDirectionalDismount()
    {
        StopClimbing();

        Vector3 vaultDir = playerMovement.WorldSpaceMoveDirection;

        if (vaultDir.magnitude < 0.1f)
        {
            vaultDir = -transform.forward;
        }

        Vector3 jumpForce = (vaultDir.normalized * jumpOffForceHorizontal) + (Vector3.up * jumpOffForceUp);
        
        controller.Move(jumpForce * Time.deltaTime); 
        
        groundedChecker.SetVerticalVelocity(jumpOffForceUp);
    }

    private void StopClimbing()
    {
        isClimbing = false;
        cooldownTimer = Time.time + 0.5f;
    }
}