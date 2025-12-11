using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    public static LightEnergyManager Instance { get; private set; }

    [Header("Global Light Energy")]
    public float startingEnergy = 0.5f;
    public float maxDuration = 20f;
    public bool useSmoothDimming = true;

    private float currentEnergy;
    private float energyDrainRate;
    
    // NEW: Multiplier to speed up drain (default is 1.0)
    private float activeDrainMultiplier = 1.0f; 
    
    private bool isDrainPaused = false;

    public float CurrentEnergy => currentEnergy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    void Start()
    {
        energyDrainRate = 1f / Mathf.Max(maxDuration, 0.1f);
        currentEnergy = Mathf.Clamp01(startingEnergy);
        isDrainPaused = false;
    }

    void Update()
    {
        if (isDrainPaused) return;

        // NEW: Apply the multiplier to the calculation
        float effectiveDrain = energyDrainRate * activeDrainMultiplier;
        
        currentEnergy -= effectiveDrain * Time.deltaTime;
        currentEnergy = Mathf.Clamp01(currentEnergy);
    }

    // --- NEW API ---
    public void SetDrainMultiplier(float multiplier)
    {
        activeDrainMultiplier = multiplier;
    }

    public void SetDrainPaused(bool isPaused)
    {
        isDrainPaused = isPaused;
    }

    public void RestoreEnergy(float amount)
    {
        if (currentEnergy >= 1f) return;
        currentEnergy = Mathf.Clamp01(currentEnergy + amount);
    }

    public float GetIntensityFactor()
    {
        return useSmoothDimming ? currentEnergy : (currentEnergy > 0f ? 1f : 0f);
    }
}