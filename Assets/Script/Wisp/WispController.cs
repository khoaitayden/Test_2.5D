// WispController.cs
using UnityEngine;

public class WispController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitSpeed = 40f;
    [SerializeField] private float followLag = 0.5f;

    [Header("Lively Motion")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Camera Safety")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float minDistanceFromCamera = 2f;

    [Header("Inner Glow")]
    [SerializeField] private Light innerGlowLight;
    [SerializeField] private float maxGlowIntensity = 2f;
    [SerializeField] private float minGlowIntensity = 0.5f;

    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;

    void Start()
    {
        if (playerTransform == null || playerController == null)
        {
            Debug.LogError("WispController is missing references to the Player!", this);
            enabled = false;
            return;
        }

        orbitAngle = Random.Range(0f, 360f);

        // Initialize glow intensity based on starting energy
        if (innerGlowLight != null && LightEnergyManager.Instance != null)
        {
            float factor = LightEnergyManager.Instance.GetIntensityFactor();
            float baseGlow = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, factor);
            innerGlowLight.intensity = baseGlow;
        }
    }

    void LateUpdate()
    {
        if (!enabled || playerTransform == null || mainCameraTransform == null) return;

        // --- Orbit ---
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // --- Lag ---
        Vector3 lagOffset = Vector3.zero;
        if (playerController.WorldSpaceMoveDirection.magnitude > 0.1f)
        {
            lagOffset = -playerTransform.forward * followLag;
        }

        // --- Bob ---
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        // --- Final Position ---
        Vector3 finalTargetPosition = playerTransform.position + orbitOffset + lagOffset + Vector3.up * bobOffset;
        transform.position = Vector3.SmoothDamp(transform.position, finalTargetPosition, ref currentVelocity, 0.2f);

        // --- Billboard to camera ---
        Vector3 flatForward = mainCameraTransform.forward;
        flatForward.y = 0;
        if (flatForward.magnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(flatForward);

        // --- Camera safety ---
        Vector3 toWisp = transform.position - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            transform.position = mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }

        // --- Inner Glow with Global Dimming ---
        if (innerGlowLight != null && LightEnergyManager.Instance != null)
        {
            // Pulsation noise
            float noise1 = Mathf.PerlinNoise(Time.time * 0.7f, 0f);
            float noise2 = Mathf.PerlinNoise(Time.time * 3.1f, 100f);
            float noise3 = Mathf.PerlinNoise(Time.time * 10f, 200f) * 0.3f;
            float pulse = Mathf.Clamp01(noise1 * 0.5f + noise2 * 0.4f + noise3 * 0.1f);

            // Apply global energy
            float energyFactor = LightEnergyManager.Instance.GetIntensityFactor();
            float glow = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse * energyFactor);
            innerGlowLight.intensity = glow;
        }
    }
}