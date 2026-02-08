using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    public bool IsHideMode { get; set; }
    public bool HasReachedCover { get; set; } 
    public bool IsSafe { get; set; }
    public bool CanHide { get; set; }


    [SerializeField] private KidnapMonsterConfig kidnapConfig;
    [SerializeField] private KidnapHideFinder hideFinder; 

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

    // Renamed for clarity: Returns TRUE if player is dangerously close
    private bool IsPlayerTooClose()
    {
        if (kidnapConfig.playerAnchor == null || kidnapConfig.playerAnchor.Value == null) return false;

        float dist = Vector3.Distance(this.transform.position, kidnapConfig.playerAnchor.Value.position);    
        // If distance is LESS than threshold, we are too close
        return dist < kidnapConfig.playerComeCloseKidnapDistance;
    }

    public override void OnLitByFlashlight()
    {
        // 1. Priority Check: If player is in grabbing range, DON'T Hide. Attack!
        if (IsPlayerTooClose())
        {
            Debug.Log("[Kidnap] Player too close! Ignoring light, attacking.");
            IsHideMode = false; // Cancel hide if active
            UpdateGOAPState();
            return;
        }

        if (!IsHideMode)
        {
            Debug.Log("[Kidnap] Light detected! Entering Hide Mode.");
            IsHideMode = true;
            HasReachedCover = false;
            IsSafe = false; 
            CanHide = true;
            UpdateGOAPState();
        }
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
        UpdateGOAPState();
    }

    private void DecideGoal()
    {
        // PRIORITY 1: If Player is too close -> FORCE ATTACK (Kidnap)
        if (IsPlayerTooClose())
        {
            provider.RequestGoal<KidnapGoal>();
            return;
        }

        // PRIORITY 2: If in Hide Mode -> Hide/Flee
        if (IsHideMode)
        {
            provider.RequestGoal<HideGoal>();
        }
        // PRIORITY 3: Normal Hunt
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