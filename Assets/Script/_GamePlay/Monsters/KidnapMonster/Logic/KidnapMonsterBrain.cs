using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    public bool IsHideMode { get; private set; }
    public bool HasReachedCover { get; private set; } 

    [SerializeField] private KidnapMonsterConfig kidnapConfig;
    [SerializeField] private KidnapHideFinder hideFinder; 

    protected override void Awake()
    {
        base.Awake();
        if(kidnapConfig == null) kidnapConfig = GetComponent<KidnapMonsterConfig>();
        if(hideFinder == null) hideFinder = GetComponent<KidnapHideFinder>();
        IsHideMode=false;
        HasReachedCover=false;
    }

    protected override string GetAgentTypeName() => "KidnapMonsterAgent";

    protected override void RequestInitialGoal() => DecideGoal();

    public override void OnLitByFlashlight()
    {
        if (!IsHideMode)
        {
            Debug.Log("[Kidnap] Light detected! Entering Hide Mode.");
            
            IsHideMode = true;
            HasReachedCover = false; 
            
            UpdateGOAPState();
        }
    }

    public void SetArrivedAtCover(bool arrived)
    {
        HasReachedCover = arrived;
    }

    public void OnHideComplete()
    {
        Debug.Log("[Kidnap] Hide & Wait complete. Resuming hunt.");
        IsHideMode = false;
        HasReachedCover = false;
        UpdateGOAPState();
    }

    private void DecideGoal()
    {
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