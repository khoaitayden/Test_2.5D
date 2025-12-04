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
    
    // New flag to pause mechanics
    private bool isDrainPaused = false;

    public float CurrentEnergy => currentEnergy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        energyDrainRate = 1f / Mathf.Max(maxDuration, 0.1f);
        currentEnergy = Mathf.Clamp01(startingEnergy);
        isDrainPaused = false;
    }

    void Update()
    {
        // Don't drain if paused
        if (isDrainPaused) return;

        currentEnergy -= energyDrainRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp01(currentEnergy);
    }

    // New Method to control drain externally
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
        // Even if paused, we return the current energy level so the light intensity 
        // matches the remaining battery when turned back on.
        return useSmoothDimming ? currentEnergy : (currentEnergy > 0f ? 1f : 0f);
    }
}