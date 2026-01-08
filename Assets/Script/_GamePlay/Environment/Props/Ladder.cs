using UnityEngine;

public class Ladder : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    // The direction to face while climbing (Blue Arrow of the ladder)
    public Vector3 ClimbDirection => -transform.forward;

    // The Top position in World Space
    public float TopY => boxCollider.bounds.max.y;
    public float BottomY => boxCollider.bounds.min.y;

    // Returns the closest point on the vertical line of the ladder
    // This allows the player to climb smoothly even if they enter from the side
    public Vector3 GetClosestPointOnLadder(Vector3 playerPos)
    {
        Vector3 center = transform.position + boxCollider.center;
        
        // We only care about aligning X and Z. Y is controlled by the player.
        // Project player position onto the ladder's vertical plane
        Vector3 targetPos = new Vector3(center.x, playerPos.y, center.z);
        return targetPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerClimbing climber = other.GetComponent<PlayerClimbing>();
        if (climber != null) climber.SetLadderNearby(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerClimbing climber = other.GetComponent<PlayerClimbing>();
        if (climber != null) climber.ClearLadderNearby();
    }
}