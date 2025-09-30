using UnityEngine;

public class PlayerLightController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Vector3 followOffset = new Vector3(-1.5f, 1.2f, -2f); // Left, Up, Behind (local to player)

    [Header("Floating Motion")]
    public float floatStrength = 0.3f;      // How much it bobs
    public float floatSpeed = 1.8f;         // Speed of bobbing
    public float swayRadius = 0.4f;         // Horizontal drifting radius
    public float swaySpeed = 0.9f;          // How fast it drifts side-to-side

    [Header("Smooth Follow")]
    public float smoothTime = 0.5f;         // Higher = more floaty lag
    public float maxFollowSpeed = 8f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (player == null) return;

        // Base target: behind & left of player (updated with player's current rotation)
        Vector3 baseTarget = player.position 
            + player.right * followOffset.x 
            + player.up * followOffset.y 
            + player.forward * followOffset.z;

        // Add vertical bob (smooth sine wave)
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        // Add horizontal sway (slow circular drift)
        float swayAngle = Time.time * swaySpeed;
        Vector3 sway = new Vector3(
            Mathf.Cos(swayAngle) * swayRadius,
            0f,
            Mathf.Sin(swayAngle * 0.7f) * swayRadius * 0.6f // elliptical, less Z
        );

        // Combine into final target
        Vector3 finalTarget = baseTarget + Vector3.up * bob + sway;

        // Smoothly follow with inertia
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalTarget,
            ref velocity,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime
        );
    }
}