using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    public bool IsHidingState { get; private set; }

    [SerializeField] private KidnapMonsterConfig kidnapConfig;

    protected override void Awake()
    {
        base.Awake();
        if (kidnapConfig == null) kidnapConfig = GetComponent<KidnapMonsterConfig>();
    }

    protected override string GetAgentTypeName() => "KidnapMonsterAgent";

    protected override void RequestInitialGoal() => DecideGoal();

    public override void OnLitByFlashlight()
    {
        if (!IsFleeing && !IsHidingState)
        {
            EvaluateLightReaction();
        }
    }

    protected void Update()
    {
        if (IsHidingState)
        {
            EvaluateLightReaction();
        }
    }

    private void EvaluateLightReaction()
    {
        if (PlayerAnchor == null || PlayerAnchor.Value == null) return;

        float distToPlayer = Vector3.Distance(transform.position, PlayerAnchor.Value.position);

        if (distToPlayer < kidnapConfig.playerComeCloseFleeDistance)
        {
            if (!IsFleeing)
            {
                Debug.Log("[Kidnap] Player got too close! Abandoning Hide, switching to Flee.");
                IsHidingState = false; 
                IsFleeing = true;      
                UpdateGOAPState();
            }
        }
        // FAR ENOUGH -> HIDE
        else if (!IsFleeing) 
        {
            if (!IsHidingState)
            {
                Debug.Log("[Kidnap] Light detected. Going to Hide.");
                IsHidingState = true;
                UpdateGOAPState();
            }
        }
    }

    public void OnHideComplete()
    {
        Debug.Log("[Kidnap] Reached hiding spot.");
        IsHidingState = false;
        UpdateGOAPState();
    }

    private void DecideGoal()
    {
        if (IsFleeing)
        {
            provider.RequestGoal<FleeGoal>();
        }
        else if (IsHidingState)
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