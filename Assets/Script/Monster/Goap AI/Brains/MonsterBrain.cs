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
        // Initialize world state
        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Investigation complete! Returning to patrol.");
        
        // Clear the last known position so InvestigateGoal becomes impossible
        this.LastKnownPlayerPosition = Vector3.zero;
        this.isActivelyInvestigating = false;
        
        // Request patrol goal
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        // CHECK 1: HANDLE VISION CHANGES (Highest Priority)
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Player spotted! Engaging chase.");
            
            // Cancel any investigation
            this.isActivelyInvestigating = false;
            this.LastKnownPlayerPosition = Vector3.zero;
            
            // Clear patrol history - we'll make new patterns after chase
            if (patrolHistory != null)
                patrolHistory.Clear();
            
            this.provider.RequestGoal<KillPlayerGoal>();
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Lost sight of player. Starting investigation.");
            
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            
            if (playerTransform != null)
            {
                this.LastKnownPlayerPosition = playerTransform.position;
                this.isActivelyInvestigating = true;
                this.provider.RequestGoal<InvestigateGoal>();
                Debug.Log($"[MonsterBrain] Saved last known position: {this.LastKnownPlayerPosition}");
            }
            
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        // CHECK 2: HANDLE IDLE STATE
        // Only request patrol if we're not investigating AND current goal is null or not patrol
        if (!isPlayerVisible && !this.isActivelyInvestigating)
        {
            var currentGoal = this.provider.CurrentPlan?.Goal;
            
            // Only request patrol if we don't already have it
            if (currentGoal == null || !(currentGoal is PatrolGoal))
            {
                Debug.Log("[MonsterBrain] No active goal or wrong goal. Requesting patrol.");
                this.provider.RequestGoal<PatrolGoal>();
            }
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}