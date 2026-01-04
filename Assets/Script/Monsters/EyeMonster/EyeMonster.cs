using UnityEngine;
using System.Collections;

public class EyeMonster : MonoBehaviour, ILitObject
{
    [Header("Burn Settings")]
    [Tooltip("How long (in seconds) the eye must be lit to burn away.")]
    [SerializeField] private float timeToVanish;

    [Tooltip("How fast the eye 'heals' when not lit (seconds recovered per second).")]
    [SerializeField] private float regenRate; 

    [Header("Dependencies")]
    [SerializeField] private EyeMonsterManager manager;

    private Transform mainCameraTransform;
    
    // "Heat" represents how much the eye has been burned (0 to timeToVanish)
    private float currentHeat = 0f;
    private bool isLitByFlashlight = false;

    void Start()
    {
        if (Camera.main != null) 
            mainCameraTransform = Camera.main.transform;
            
        if (manager == null) 
            manager = FindFirstObjectByType<EyeMonsterManager>();
    }

    void OnEnable()
    {
        currentHeat = 0f;
        isLitByFlashlight = false;
    }

    void LateUpdate()
    {
        // 1. Billboarding
        if (mainCameraTransform != null)
        {
            transform.LookAt(mainCameraTransform);
        }

        // 2. Heat / Burn Logic
        if (isLitByFlashlight)
        {
            // Increase Heat
            currentHeat += Time.deltaTime;
            
            // Check for Death
            if (currentHeat >= timeToVanish)
            {
                Vanish();
            }
        }
        else
        {
            // Decrease Heat (Regen)
            if (currentHeat > 0f)
            {
                currentHeat -= regenRate * Time.deltaTime;
                if (currentHeat < 0f) currentHeat = 0f;
            }
        }
        
        // Optional Debug: Shake based on heat?
        // if (currentHeat > 0) transform.localScale = Vector3.one * (1f - (currentHeat/timeToVanish) * 0.2f);
    }

    private void Vanish()
    {
        Debug.Log("<color=green>[EyeMonster]</color> Eye burned away!");
        
        TraceEventBus.Emit(transform.position, TraceType.EnviromentNoiseWeak);

        if (manager != null)
        {
            manager.DespawnEye();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // --- ILitObject Implementation ---

    public void OnLit(LightSourceType sourceType)
    {
        if (sourceType == LightSourceType.Flashlight)
        {
            isLitByFlashlight = true;
        }
    }

    public void OnUnlit(LightSourceType sourceType)
    {
        if (sourceType == LightSourceType.Flashlight)
        {
            isLitByFlashlight = false;
        }
    }
}