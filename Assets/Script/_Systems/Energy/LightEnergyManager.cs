using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    // REMOVED: public static LightEnergyManager Instance;

    [Header("Data References")]
    [SerializeField] private FloatVariableSO currentEnergy; // Drag "var_CurrentEnergy"
    [SerializeField] private FloatVariableSO maxEnergy;     // Drag "var_MaxEnergy"    // Drag "var_IsSprinting"
    [SerializeField] private BoolVariableSO isFlashlightOn; // Drag "var_IsFlashlightOn"

    [Header("Base Settings")]
    [SerializeField] private float maxDuration = 100f; // This sets the MaxEnergy SO
    [SerializeField] private float startingPercentage = 0.5f;

    [Header("Drain Multipliers")]
    [SerializeField] private float flashlightCostMult = 1.5f;
    [SerializeField] private float sprintCostMult = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool isDrainPaused = false;
    private float drainRateBase = 1.0f;

    void Awake()
    {
        // Initialize the Data assets
        // We do this here so other scripts reading Start() get correct values
        if (maxEnergy != null) maxEnergy.Value = maxDuration;
        
        if (currentEnergy != null && maxEnergy != null)
        {
            currentEnergy.Value = maxEnergy.Value * startingPercentage;
        }
    }

    void Update()
    {
        if (isDrainPaused || currentEnergy == null) return;

        float finalMultiplier = 1.0f;

        // READ from the ScriptableObjects directly
        if (isFlashlightOn.Value) 
            finalMultiplier *= flashlightCostMult;

        if (InputManager.Instance.IsSprinting) 
            finalMultiplier *= sprintCostMult;

        float drain = drainRateBase * finalMultiplier * Time.deltaTime;
        currentEnergy.ApplyChange(-drain, 0f, maxEnergy.Value);

    
    }

    public void SetDrainPaused(bool isPaused)
    {
        isDrainPaused = isPaused;
    }
}