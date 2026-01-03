using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    public static LightEnergyManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float maxDuration = 100f; // Seconds of light
    [SerializeField] private float startingPercentage = 0.5f; // 0 to 1

    private float currentEnergy;
    private float drainRateBase;
    private float activeDrainMultiplier = 1.0f; 
    private bool isDrainPaused = false;

    // Public Getter
    public float CurrentEnergy => currentEnergy;
    public float EnergyFraction => currentEnergy / maxDuration; // Returns 0.0 to 1.0

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentEnergy = maxDuration * startingPercentage;
        drainRateBase = 1.0f; // 1 unit per second
    }

    void Update()
    {
        if (isDrainPaused) return;

        float drain = drainRateBase * activeDrainMultiplier * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy - drain, 0f, maxDuration);
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxDuration);
    }

    public void SetDrainMultiplier(float multiplier) => activeDrainMultiplier = multiplier;
    public void SetDrainPaused(bool isPaused) => isDrainPaused = isPaused;
    
    // Helper for lights to dim
    public float GetIntensityFactor()
    {
        return Mathf.Clamp01(currentEnergy / (maxDuration * 0.2f)); // Dim when last 20% remains
    }
}