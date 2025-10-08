using UnityEngine;

public class WispLightController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    private Transform mainCameraTransform;

    [Header("Point Light Settings")]
    public Light pointLight;
    public Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    [Header("Vision Spot Light Settings")]
    public Light visionLight;
    public Vector3 visionLightOffset = new Vector3(0f, 1.8f, -1.0f);

    [Header("Focus Spot Light Settings")]
    public Light focusLight;
    public Vector3 focusLightOffset = new Vector3(0f, 1.6f, -0.5f);

    [Header("Floating Motion")]
    public float floatStrength = 0.2f;
    public float floatSpeed = 2f;

    [Header("Smooth Follow")]
    public float smoothTime = 0.3f;

    [Header("Controls")]
    public KeyCode switchKey = KeyCode.F;

    private Vector3 pointVelocity = Vector3.zero;
    private Vector3 visionVelocity = Vector3.zero;
    private Vector3 focusVelocity = Vector3.zero;

    private enum LightMode { Point, Vision, Focus }
    private LightMode currentMode = LightMode.Point;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        ApplyLightMode();
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            // Cycle: Point → Vision → Focus → Point...
            currentMode = (LightMode)(((int)currentMode + 1) % 3);
            ApplyLightMode();
        }
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;

        Vector3 basePosition = player.position;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        // === Point Light ===
        if (pointLight != null)
        {
            Vector3 pointTarget = basePosition;
            pointTarget += mainCameraTransform.right * pointLightOffset.x;
            pointTarget += Vector3.up * (pointLightOffset.y + bob);
            pointTarget += mainCameraTransform.forward * pointLightOffset.z;

            pointLight.transform.position = Vector3.SmoothDamp(
                pointLight.transform.position,
                pointTarget,
                ref pointVelocity,
                smoothTime
            );

            pointLight.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
        }

        // === Vision Light ===
        if (visionLight != null)
        {
            Vector3 visionTarget = basePosition;
            visionTarget += mainCameraTransform.right * visionLightOffset.x;
            visionTarget += Vector3.up * (visionLightOffset.y + bob);
            visionTarget += mainCameraTransform.forward * visionLightOffset.z;

            visionLight.transform.position = Vector3.SmoothDamp(
                visionLight.transform.position,
                visionTarget,
                ref visionVelocity,
                smoothTime
            );

            visionLight.transform.rotation = mainCameraTransform.rotation;
        }

        // === Focus Light ===
        if (focusLight != null)
        {
            Vector3 focusTarget = basePosition;
            focusTarget += mainCameraTransform.right * focusLightOffset.x;
            focusTarget += Vector3.up * (focusLightOffset.y + bob);
            focusTarget += mainCameraTransform.forward * focusLightOffset.z;

            focusLight.transform.position = Vector3.SmoothDamp(
                focusLight.transform.position,
                focusTarget,
                ref focusVelocity,
                smoothTime
            );

            focusLight.transform.rotation = mainCameraTransform.rotation;
        }
    }

    void ApplyLightMode()
    {
        bool isPoint = currentMode == LightMode.Point;
        bool isVision = currentMode == LightMode.Vision;
        bool isFocus = currentMode == LightMode.Focus;

        if (pointLight != null) pointLight.enabled = isPoint;
        if (visionLight != null) visionLight.enabled = isVision;
        if (focusLight != null) focusLight.enabled = isFocus;

        string modeName = currentMode.ToString();
        Debug.Log("Switched to Light Mode: " + modeName);
    }
}