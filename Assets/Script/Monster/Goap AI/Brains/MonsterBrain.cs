using System;
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
    private PatrolHistory patrolHistory;
    private Transform playerTransform;
    private bool wasPlayerVisibleLastFrame = false;
    private bool isActivelyInvestigating = false;

    private void Awake()
    {
        this.agent = this.GetComponent<AgentBehaviour>();
        this.provider = this.GetComponent<GoapActionProvider>();
        this.config = this.GetComponent<MonsterConfig>();
        this.patrolHistory = this.GetComponent<PatrolHistory>();
        
        var goap = FindFirstObjectByType<GoapBehaviour>();
        if (this.provider.AgentTypeBehaviour == null && goap != null)
            this.provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
    }

    private void Start()
    {
        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Investigation complete! Returning to patrol.");
        
        this.LastKnownPlayerPosition = Vector3.zero;
        this.isActivelyInvestigating = false;
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        // CHECK 1: PLAYER SPOTTED (Highest Priority)
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Player spotted! Engaging chase.");
            
            // Cancel any investigation - but DON'T call OnInvestigationComplete
            // Just set the flag and let GOAP handle stopping the action
            this.isActivelyInvestigating = false;
            this.LastKnownPlayerPosition = Vector3.zero;
            
            // Clear patrol history
            if (patrolHistory != null)
                patrolHistory.Clear();
            
            this.provider.RequestGoal<KillPlayerGoal>();
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return; // Exit immediately
        }

        // CHECK 2: LOST SIGHT OF PLAYER (Start Investigation)
        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Lost sight of player. Starting investigation.");
            
            // Get player transform if needed
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) 
                    playerTransform = player.transform;
            }
            
            if (playerTransform != null)
            {
                // Save position and set flag BEFORE requesting goal
                this.LastKnownPlayerPosition = playerTransform.position;
                this.isActivelyInvestigating = true;
                
                Debug.Log($"[MonsterBrain] Saved last known position: {this.LastKnownPlayerPosition}");
                
                // Request investigation goal
                this.provider.RequestGoal<InvestigateGoal>();
            }
            else
            {
                Debug.LogWarning("[MonsterBrain] Could not find player transform!");
            }
            
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return; // Exit immediately after handling vision change
        }

        // CHECK 3: HANDLE IDLE STATE (Only if not investigating AND not chasing)
        if (!isPlayerVisible && !this.isActivelyInvestigating)
        {
            var currentGoal = this.provider.CurrentPlan?.Goal;
            
            // Only request patrol if we don't already have it or KillPlayerGoal
            if (currentGoal == null || (!(currentGoal is PatrolGoal) && !(currentGoal is KillPlayerGoal)))
            {
                Debug.Log("[MonsterBrain] No active goal. Requesting patrol.");
                this.provider.RequestGoal<PatrolGoal>();
            }
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}