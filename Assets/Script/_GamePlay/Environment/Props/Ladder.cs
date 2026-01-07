using UnityEngine;

public class Ladder : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ClimbDirection => -transform.forward; 

    public float GetLadderTopY()
    {
        return boxCollider.bounds.max.y;
    }

    private void OnTriggerEnter(Collider other)
    {
        // OLD: PlayerController player = other.GetComponent<PlayerController>();
        // OLD: if (player != null) player.SetLadderNearby(this);

        // NEW: Look for the specific capability
        PlayerClimbing climber = other.GetComponent<PlayerClimbing>();
        if (climber != null) 
        {
            climber.SetLadderNearby(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // NEW:
        PlayerClimbing climber = other.GetComponent<PlayerClimbing>();
        if (climber != null) 
        {
            climber.ClearLadderNearby();
        }
    }
}