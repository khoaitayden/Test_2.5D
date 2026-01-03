using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Light spotLight;
    
    [Header("Settings")]
    [SerializeField] private float duration = 5.0f;
    [SerializeField] private float cooldown = 3.0f;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private LayerMask interactLayer;

    // State
    public bool IsActive { get; private set; }
    private float turnOffTime;
    private float nextAvailableTime;
    private ILitObject currentLitObj;

    void Start()
    {
        if (spotLight != null) spotLight.enabled = false;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnWispCycleTriggered += ActivateFlashlight;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnWispCycleTriggered -= ActivateFlashlight;
    }

    void ActivateFlashlight()
    {
        // Logic: Can only turn on if not already on AND cooldown passed
        if (IsActive) return; 
        if (Time.time < nextAvailableTime) return;

        IsActive = true;
        turnOffTime = Time.time + duration;
        
        if (spotLight != null) spotLight.enabled = true;
        
        // Play Sound?
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseWeak);
    }

    void Update()
    {
        if (!IsActive) return;

        // 1. Check Timer
        if (Time.time >= turnOffTime)
        {
            DeactivateFlashlight();
            return;
        }

        // 2. Raycast Logic (Trigger events with light)
        CheckLightInteraction();
    }

    void DeactivateFlashlight()
    {
        IsActive = false;
        nextAvailableTime = Time.time + cooldown;
        
        if (spotLight != null) spotLight.enabled = false;

        // Cleanup Lit Object
        if (currentLitObj != null)
        {
            currentLitObj.OnUnlit(LightSourceType.Flashlight);
            currentLitObj = null;
        }
    }

    void CheckLightInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, interactLayer))
        {
            ILitObject obj = hit.collider.GetComponent<ILitObject>();
            if (obj != currentLitObj)
            {
                if (currentLitObj != null) currentLitObj.OnUnlit(LightSourceType.Flashlight);
                currentLitObj = obj;
                if (currentLitObj != null) currentLitObj.OnLit(LightSourceType.Flashlight);
            }
        }
        else
        {
            if (currentLitObj != null)
            {
                currentLitObj.OnUnlit(LightSourceType.Flashlight);
                currentLitObj = null;
            }
        }
    }
}