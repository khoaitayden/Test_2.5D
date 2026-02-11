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
        if (!IsHideMode)
        {
            HandleLightExposure();
        }
    }
    // We can keep this empty or remove it, since we handle logic in Update now
    public override void OnLitByFlashlight() { }

    private void HandleLightExposure()
    {
        bool isLit = vision != null && vision.IsLit;

        if (isLit)
        {
            lightExposureTimer += Time.deltaTime;
        }
        else
        {
            if (lightExposureTimer > 0)
                lightExposureTimer -= Time.deltaTime * kidnapConfig.lightDecaySpeed;
        }

        lightExposureTimer = Mathf.Clamp(lightExposureTimer, 0f, kidnapConfig.lightToleranceDuration);
        //Debug.Log(lightExposureTimer);

        // TRIGGER LOGIC
        if (lightExposureTimer >= kidnapConfig.lightToleranceDuration)
        {
            TriggerHideLogic();
        }
    }

    private void TriggerHideLogic()
    {
        if (IsPlayerTooClose()) return;

        Debug.Log("[Kidnap] Light tolerance exceeded. Entering Hide Mode.");
        
        IsHideMode = true;
        
        // Reset flags for fresh hide sequence
        HasReachedCover = false;
        IsSafe = false; 
        CanHide = true;

        lightExposureTimer = 0f; 

        UpdateGOAPState();
    }


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
        
        lightExposureTimer = 0f; 
        
        UpdateGOAPState();
    }

    private void DecideGoal()
    {
        if (IsPlayerTooClose())
        {
            provider.RequestGoal<KidnapGoal>();
            return;
        }

        if (IsHideMode)
        {
            provider.RequestGoal<HideGoal>();
        }
        else
        {   
            provider.RequestGoal<KidnapGoal>();
        }
    }

    protected override void UpdateGOAPState()
    {
        base.UpdateGOAPState();
        DecideGoal();
    }
}