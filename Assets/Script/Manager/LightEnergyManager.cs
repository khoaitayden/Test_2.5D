// LightEnergyManager.cs
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
        currentEnergy -= energyDrainRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp01(currentEnergy);
    }

    public void RestoreEnergy(float amount)
    {
        if (currentEnergy >= 1f)
        {
            // Optional: log or trigger "full" event
            return;
        }

        currentEnergy = Mathf.Clamp01(currentEnergy + amount);
    }

    public float GetIntensityFactor()
    {
        return useSmoothDimming ? currentEnergy : (currentEnergy > 0f ? 1f : 0f);
    }
}