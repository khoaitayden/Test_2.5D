using UnityEngine;
public class Ladder : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    // Direction to face (Blue arrow points into wall)
    public Vector3 ClimbDirection => -transform.forward; 

    // Helper: Where is the top of this ladder in World Space?
    public float GetLadderTopY()
    {
        // Top of the box collider
        return boxCollider.bounds.max.y;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.SetLadderNearby(this);
    }
    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.ClearLadderNearby();
    }
}