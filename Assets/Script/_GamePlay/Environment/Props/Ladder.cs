using UnityEngine;

public class Ladder : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ClimbDirection => -transform.forward;

    public float TopY => boxCollider.bounds.max.y;
    public float BottomY => boxCollider.bounds.min.y;

    public Vector3 GetClosestPointOnLadder(Vector3 playerPos)
    {
        Vector3 center = transform.position + boxCollider.center;

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