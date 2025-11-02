// FILE TO EDIT: MonsterBrain.cs

using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    public Vector3 LastKnownPlayerPosition { get; private set; } = Vector3.zero;

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
        
        provider.WorldData.SetState(new CanPatrol(), 1);
        provider.RequestGoal<KillPlayerGoal>();
    }

    public void OnArrivedAtSuspiciousLocation()
    {
        provider.WorldData.SetState(new IsAtSuspiciousLocation(), 1);
    }

    public void OnInvestigationFinished()
    {
        Debug.Log("[Brain] Investigation finished. Clearing all investigation states.");
        LastKnownPlayerPosition = Vector3.zero;
        provider.WorldData.SetState(new IsAtSuspiciousLocation(), 0);
        provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
        provider.WorldData.SetState(new CanPatrol(), 1);
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
            provider.WorldData.SetState(new CanPatrol(), 0);
            provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
            provider.WorldData.SetState(new IsAtSuspiciousLocation(), 0);
        }
        else if (justLostPlayer)
        {
            // --- SIMPLIFIED LOGIC ---
            // No delay. If the player is lost, immediately set the clue.
            if (playerTransform != null)
            {
                LastKnownPlayerPosition = playerTransform.position;
                Debug.Log($"[Brain] Player lost. Setting last known position instantly: {LastKnownPlayerPosition}");
                
                // Set the clue and stop patrolling. The planner will take over.
                provider.WorldData.SetState(new HasSuspiciousLocation(), 1);
                provider.WorldData.SetState(new CanPatrol(), 0);
            }
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}