// LightEnergyManager.cs
using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    public static LightEnergyManager Instance { get; private set; }

    [Header("Global Light Energy")]
    public float startingEnergy = 0.5f;      // 0 = dead, 1 = full
    public float maxDuration = 20f;          // Time to drain from full to zero
    public bool useSmoothDimming = true;

    private float currentEnergy;
    private float energyDrainRate;

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
    }

    void Update()
    {
        // Drain energy every frame (even if no light is on — optional)
        currentEnergy -= energyDrainRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp01(currentEnergy);
    }

    // Call this later to restore light (e.g., near campfire)
    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp01(currentEnergy + amount);
    }

    public float GetIntensityFactor()
    {
        return useSmoothDimming ? currentEnergy : (currentEnergy > 0f ? 1f : 0f);
    }
}