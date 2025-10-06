using UnityEngine;

public class PlayerSatellite : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform watchPoint;
    [SerializeField] private float rotateSpeed = 50f;
    private float orbitRadius;

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    private void OnDrawGizmos()
    {
        
    }
}
