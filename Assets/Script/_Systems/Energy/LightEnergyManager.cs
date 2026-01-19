using UnityEngine;

public class LightEnergyManager : MonoBehaviour
{
    // REMOVED: public static LightEnergyManager Instance;

    [Header("Events")]
    [SerializeField] private GameEventSO onEmptyEnergyEvent; 
    [Header("Data References")]
    [SerializeField] private FloatVariableSO currentEnergy; 
    [SerializeField] private FloatVariableSO maxEnergy;        
    [SerializeField] private BoolVariableSO isFlashlightOn; 
    [SerializeField] private BoolVariableSO isPlayerExpose;
    [SerializeField] private BoolVariableSO isPlayerAttached; 

    [Header("Base Settings")]
    [SerializeField] private float maxDuration = 100f; // This sets the MaxEnergy SO
    [SerializeField] private float startingPercentage = 0.5f;

    [Header("Drain Multipliers")]
    [SerializeField] private float flashlightCostMult ;
    [SerializeField] private float sprintCostMult;
    [SerializeField] private float exposedCostMult;
    [SerializeField] private float attachedCostMult;

    [Header("Debug")]
    [SerializeField] private bool isDrainPaused = false;
    private float drainRateBase = 1.0f;
    private bool isEnergyDepleted = false;
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
        if (isDrainPaused || currentEnergy == null||isEnergyDepleted) return;

        float finalMultiplier = 1.0f;

        // READ from the ScriptableObjects directly
        if (isFlashlightOn.Value) 
            finalMultiplier *= flashlightCostMult;

        if (InputManager.Instance.IsSprinting) 
            finalMultiplier *= sprintCostMult;
            
        if (isPlayerExpose.Value)
            finalMultiplier *= exposedCostMult;

        if (isPlayerAttached.Value)
            finalMultiplier *= attachedCostMult;

        float drain = drainRateBase * finalMultiplier * Time.deltaTime;
        currentEnergy.ApplyChange(-drain, 0f, maxEnergy.Value);
        //Debug.Log($"Light Energy: {currentEnergy.Value}/{maxEnergy.Value}");
        if (currentEnergy.Value <= 0f)
        {
            isEnergyDepleted = true;
            currentEnergy.Value = 0f;
            if (onEmptyEnergyEvent != null) onEmptyEnergyEvent.Raise();
        }   
    }
    public void SetDrainPaused(bool isPaused)
    {
        isDrainPaused = isPaused;
    }
    public void ResetEnergy()
    {
        if (currentEnergy != null && maxEnergy != null)
        {
            // Reset to starting percentage or full
            currentEnergy.Value = maxEnergy.Value * startingPercentage;
        }
    }
}