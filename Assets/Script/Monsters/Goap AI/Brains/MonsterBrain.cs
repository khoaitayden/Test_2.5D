// FILE TO EDIT: MonsterBrain.cs

using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    public Vector3 LastKnownPlayerPosition { get; private set; } = Vector3.zero;

    public bool IsInvestigating { get; private set; }
    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private Transform playerTransform;
    private bool wasPlayerVisibleLastFrame = false;

    private void Awake()
    {
        agent = GetComponent<AgentBehaviour>();
        provider = GetComponent<GoapActionProvider>();
        config = GetComponent<MonsterConfig>();
        
        var goap = FindFirstObjectByType<GoapBehaviour>();
        if (provider.AgentTypeBehaviour == null && goap != null)
            provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
    }

    private void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        IsInvestigating = false;
        provider.WorldData.SetState(new CanPatrol(), 1);
        provider.WorldData.SetState(new IsInvestigating(), 0);
        provider.RequestGoal<KillPlayerGoal>();
    }

    public void OnInvestigationFinished()
    {
        Debug.Log("[Brain] Investigation finished. Clearing all investigation states.");
        LastKnownPlayerPosition = Vector3.zero;
        IsInvestigating = false; 
        
        provider.WorldData.SetState(new IsInvestigating(), 0);
        provider.WorldData.SetState(new CanPatrol(), 1);
        provider.RequestGoal<KillPlayerGoal>();
    }
    public void OnInvestigationFailed()
    {
        Debug.LogWarning("[Brain] Investigation FAILED. Resetting all investigation states and returning to patrol.");
        OnInvestigationFinished();
    }
    public void OnArrivedAtSuspiciousLocation()
    {
        provider.WorldData.SetState(new IsAtSuspiciousLocation(), 1);
        provider.RequestGoal<KillPlayerGoal>();
    }
    
    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(agent, config);
        provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        bool justSpottedPlayer = isPlayerVisible && !wasPlayerVisibleLastFrame;
        bool justLostPlayer = !isPlayerVisible && wasPlayerVisibleLastFrame;

        if (justSpottedPlayer)
        {
            Debug.Log("[Brain] Player is now visible. Preparing to attack.");
            IsInvestigating = false;
            provider.WorldData.SetState(new CanPatrol(), 0);
            provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
            provider.WorldData.SetState(new IsAtSuspiciousLocation(), 0);
            provider.WorldData.SetState(new IsInvestigating(), 0);
        }
        else if (justLostPlayer)
        {
            if (playerTransform != null)
            {
                LastKnownPlayerPosition = playerTransform.position;
                Debug.Log($"[Brain] Player lost. Setting last known position instantly: {LastKnownPlayerPosition}");
                IsInvestigating = true;
                provider.WorldData.SetState(new IsInvestigating(), 1);
                provider.WorldData.SetState(new HasSuspiciousLocation(), 1);
                provider.WorldData.SetState(new CanPatrol(), 0);
            }
        }
        Debug.Log($"[Brain] current player last known position: {LastKnownPlayerPosition}");

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}