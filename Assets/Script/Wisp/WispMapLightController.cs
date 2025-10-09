// WispMapLightController.cs
using UnityEngine;

public class WispMapLightController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    private Transform mainCameraTransform;

    [Header("Lights")]
    public Light pointLight;
    public Vector3 pointLightOffset = new Vector3(1.5f, 2.5f, -2.0f);

    public Light visionLight;
    public Vector3 visionLightOffset = new Vector3(0f, 1.8f, -1.0f);

    public Light focusLight;
    public Vector3 focusLightOffset = new Vector3(0f, 1.6f, -0.5f);

    [Header("Floating & Movement")]
    public float floatStrength = 0.2f;
    public float floatSpeed = 2f;
    public float smoothTime = 0.3f;

    [Header("Controls")]
    public KeyCode switchKey = KeyCode.F;

    // Original full settings
    private float origPointI, origPointR;
    private float origVisionI, origVisionR;
    private float origFocusI, origFocusR;

    private Vector3 pointVel = Vector3.zero;
    private Vector3 visionVel = Vector3.zero;
    private Vector3 focusVel = Vector3.zero;

    private enum LightMode { Point, Vision, Focus }
    private LightMode currentMode = LightMode.Point;

    void Start()
    {
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        CacheOriginalSettings();
        ApplyLightMode();
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            currentMode = (LightMode)(((int)currentMode + 1) % 3);
            ApplyLightMode();
        }

        // Apply global energy to all lights
        if (LightEnergyManager.Instance != null)
        {
            float energy = LightEnergyManager.Instance.GetIntensityFactor();
            ApplyEnergyToAllLights(energy);

            if (energy <= 0f)
            {
                TurnOffAllLights();
            }
        }
    }

    void LateUpdate()
    {
        if (player == null || mainCameraTransform == null) return;

        Vector3 basePos = player.position;
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        UpdateLight(pointLight, pointLightOffset, ref pointVel, false, bob);
        UpdateLight(visionLight, visionLightOffset, ref visionVel, true, bob);
        UpdateLight(focusLight, focusLightOffset, ref focusVel, true, bob);
    }

    void UpdateLight(Light light, Vector3 offset, ref Vector3 velocity, bool useCameraRotation, float bob)
    {
        if (light == null) return;

        Vector3 target = player.position +
            mainCameraTransform.right * offset.x +
            Vector3.up * (offset.y + bob) +
            mainCameraTransform.forward * offset.z;

        light.transform.position = Vector3.SmoothDamp(light.transform.position, target, ref velocity, smoothTime);

        if (useCameraRotation)
            light.transform.rotation = mainCameraTransform.rotation;
        else
            light.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
    }

    void CacheOriginalSettings()
    {
        if (pointLight != null) { origPointI = pointLight.intensity; origPointR = pointLight.range; }
        if (visionLight != null) { origVisionI = visionLight.intensity; origVisionR = visionLight.range; }
        if (focusLight != null) { origFocusI = focusLight.intensity; origFocusR = focusLight.range; }
    }

    void ApplyLightMode()
    {
        TurnOffAllLights();
        switch (currentMode)
        {
            case LightMode.Point: if (pointLight != null) pointLight.enabled = true; break;
            case LightMode.Vision: if (visionLight != null) visionLight.enabled = true; break;
            case LightMode.Focus: if (focusLight != null) focusLight.enabled = true; break;
        }
    }

    void TurnOffAllLights()
    {
        if (pointLight != null) pointLight.enabled = false;
        if (visionLight != null) visionLight.enabled = false;
        if (focusLight != null) focusLight.enabled = false;
    }

    void ApplyEnergyToAllLights(float energy)
    {
        if (pointLight != null)
        {
            pointLight.intensity = origPointI * energy;
            pointLight.range = origPointR * energy;
        }
        if (visionLight != null)
        {
            visionLight.intensity = origVisionI * energy;
            visionLight.range = origVisionR * energy;
        }
        if (focusLight != null)
        {
            focusLight.intensity = origFocusI * energy;
            focusLight.range = origFocusR * energy;
        }
    }
}