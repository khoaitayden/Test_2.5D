using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    // --- State Properties ---
    public bool IsHideMode { get; set; }
    public bool HasReachedCover { get; set; } 
    public bool IsSafe { get; set; }
    public bool CanHide { get; set; }

    [Header("Dependencies")]
    [SerializeField] private KidnapMonsterConfig kidnapConfig;
    [SerializeField] private KidnapHideFinder hideFinder;
    [SerializeField] private MonsterVision vision; 


    // Internal State
    private float lightExposureTimer = 0f;
    private bool isCurrentlyLit = false;

    protected override void Awake()
    {
        base.Awake();
        if(kidnapConfig == null) kidnapConfig = GetComponent<KidnapMonsterConfig>();
        if(hideFinder == null) hideFinder = GetComponent<KidnapHideFinder>();
        
        IsHideMode = false;
        HasReachedCover = false;
        IsSafe = false;
        CanHide = false;
    }

    protected override string GetAgentTypeName() => "KidnapMonsterAgent";

    protected override void RequestInitialGoal() => DecideGoal();

    // --- MAIN UPDATE LOOP ---
   private void Update()
    {
        HandleLightExposure();
    }

    // We can keep this empty or remove it, since we handle logic in Update now
    public override void OnLitByFlashlight() { }

    private void HandleLightExposure()
    {
        // 1. CHECK THE VISION COMPONENT DIRECTLY
        // This is stable. It doesn't depend on execution order.
        bool isLit = (vision != null && vision.IsLit);

        if (isLit)
        {
            lightExposureTimer += Time.deltaTime;
        }
        else
        {
            if (lightExposureTimer > 0)
                lightExposureTimer -= Time.deltaTime * kidnapConfig.lightDecaySpeed;
        }

        // Clamp logic
        //lightExposureTimer = Mathf.Clamp(lightExposureTimer, 0f, kidnapConfig.lightToleranceDuration);
        
        // Debug to see it working
        Debug.Log($"Light Timer: {lightExposureTimer} / {kidnapConfig.lightToleranceDuration}");

        // 2. Check Threshold
        if (lightExposureTimer >= kidnapConfig.lightToleranceDuration)
        {
            TriggerHideLogic();
        }
    }

    private void TriggerHideLogic()
    {
        if (IsHideMode || IsPlayerTooClose()) return;

        Debug.Log("[Kidnap] Light tolerance exceeded. Entering Hide Mode.");
        IsHideMode = true;
        HasReachedCover = false;
        IsSafe = false; 
        CanHide = true;
        UpdateGOAPState();
    }
    // --- EXISTING LOGIC ---

    private bool IsPlayerTooClose()
    {
        if (kidnapConfig.playerAnchor == null || kidnapConfig.playerAnchor.Value == null) return false;
        float dist = Vector3.Distance(this.transform.position, kidnapConfig.playerAnchor.Value.position);    
        return dist < kidnapConfig.playerComeCloseKidnapDistance;
    }

    public void SetArrivedAtCover(bool arrived)
    {
        HasReachedCover = arrived;
    }

    public void OnSafetyAchieved()
    {
        Debug.Log("[Kidnap] Safety Achieved.");
        IsSafe = true;
        OnHideComplete(); 
    }

    public void OnHideComplete()
    {
        Debug.Log("[Kidnap] Resuming hunt.");
        IsHideMode = false;
        HasReachedCover = false;
        IsSafe = false; 
        CanHide = false;
        
        // Reset light timer so we don't hide immediately again unless shined on
        lightExposureTimer = 0f; 
        
        UpdateGOAPState();
    }

    private void DecideGoal()
    {
        // PRIORITY 1: ATTACK
        if (IsPlayerTooClose())
        {
            provider.WorldData.SetState<HasKidnappedPlayer>(0);
            provider.RequestGoal<KidnapGoal>();
            return;
        }

        // PRIORITY 2: HIDE (Only if timer exceeded)
        if (IsHideMode)
        {
            provider.RequestGoal<HideGoal>();
        }
        // PRIORITY 3: HUNT
        else
        {   
            provider.WorldData.SetState<HasKidnappedPlayer>(0);
            provider.RequestGoal<KidnapGoal>();
        }
    }

    protected override void UpdateGOAPState()
    {
        base.UpdateGOAPState();
        DecideGoal();
    }
}