using UnityEngine;

public class PlayerSatellite : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float orbitRadius;
    [SerializeField] private float rotateSpeed; // degrees per second
    [SerializeField] private float heightOffset; // keeps it above ground/player

    private float currentAngle = 0f;

    void FixedUpdate()
    {
        if (player == null) return;

        // Update orbit angle
        currentAngle += rotateSpeed * Time.fixedDeltaTime;
        currentAngle = Mathf.Repeat(currentAngle, 360f);

        // Orbit in world XZ plane (ignores player rotation)
        float x = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * orbitRadius;
        float z = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * orbitRadius;

        // Match player's world position, but keep stable Y (e.g., same height as player + offset)
        Vector3 targetPosition = player.position + new Vector3(x, heightOffset, z);

        // Optionally: smooth follow (or just set directly)
        transform.position = targetPosition;

    }

    private void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position + Vector3.up * heightOffset, orbitRadius);
        }
    }
}