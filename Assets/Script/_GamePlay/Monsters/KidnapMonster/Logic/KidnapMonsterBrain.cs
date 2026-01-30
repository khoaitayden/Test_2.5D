using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    public bool IsHideMode { get; private set; }

    [SerializeField] private KidnapMonsterConfig kidnapConfig;
    [SerializeField] private KidnapHideFinder hideFinder; 

    protected override void Awake()
    {
        base.Awake();
        if(kidnapConfig == null) kidnapConfig = GetComponent<KidnapMonsterConfig>();
        if(hideFinder == null) hideFinder = GetComponent<KidnapHideFinder>();
    }

    protected override string GetAgentTypeName() => "KidnapMonsterAgent";

    protected override void RequestInitialGoal() => DecideGoal();

    public override void OnLitByFlashlight()
    {
        if (!IsHideMode)
        {
            Debug.Log("[Kidnap] Light detected! Entering Hide Mode.");
            IsHideMode = true;
            UpdateGOAPState();
            DecideGoal();
        }
    }

    public void OnHideComplete()
    {
        Debug.Log("[Kidnap] Hide complete. Resuming hunt.");
        IsHideMode = false;
        UpdateGOAPState();
        DecideGoal();
    }

    private void DecideGoal()
    {
        if (IsHideMode)
        {
            Debug.Log("[Kidnap] Entering Hide Mode.");
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
    }
}