using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    public static LightEnergyManager Instance { get; private set; }

    [Header("Base Settings")]
    [SerializeField] private float maxDuration = 100f;
    [SerializeField] private float startingPercentage = 0.5f;

    [Header("Drain Multipliers")]
    [Tooltip("Multiplier when flashlight is ON.")]
    [SerializeField] private float flashlightCostMult = 1.5f;
    [Tooltip("Multiplier when Sprinting.")]
    [SerializeField] private float sprintCostMult = 2.0f;

    // Internal State
    private float currentEnergy;
    private float drainRateBase = 1.0f; // 1 unit per second default
    private bool isDrainPaused = false;

    // Flags controlled by other scripts
    private bool isFlashlightActive = false;
    private bool isPlayerSprinting = false;

    public float CurrentEnergy => currentEnergy;
    public float EnergyFraction => maxDuration > 0 ? currentEnergy / maxDuration : 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentEnergy = maxDuration * startingPercentage;
    }

    void Update()
    {
        if (isDrainPaused) return;

        // --- CENTRALIZED DRAIN CALCULATION ---
        float finalMultiplier = 1.0f;

        if (isFlashlightActive) 
            finalMultiplier *= flashlightCostMult;

        if (isPlayerSprinting) 
            finalMultiplier *= sprintCostMult;

        float drain = drainRateBase * finalMultiplier * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy - drain, 0f, maxDuration);
    }

    // --- PUBLIC API FOR OTHER SCRIPTS ---

    public void SetFlashlightState(bool isOn)
    {
        isFlashlightActive = isOn;
    }

    public void SetSprintState(bool isSprinting)
    {
        isPlayerSprinting = isSprinting;
    }

    public void SetDrainPaused(bool isPaused)
    {
        isDrainPaused = isPaused;
    }

    public void RestoreEnergy(float percentAmount)
    {
        float actualAmount = maxDuration * percentAmount;
        currentEnergy = Mathf.Clamp(currentEnergy + actualAmount, 0f, maxDuration);
    }
}