using System;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    [Header("Behavior Tuning")]
    [Tooltip("How long (in seconds) the monster will continue to 'believe' it's chasing the player after losing sight.")]

    public Vector3 LastKnownPlayerPosition { get; private set; } = Vector3.zero;

    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private PatrolHistory patrolHistory;
    private Transform playerTransform;

    // --- NEW STATE VARIABLES ---
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
        // Player transform can be found here once.
        var player = GameObject.FindWithTag("Player");
        if (player != null) 
            playerTransform = player.transform;

        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Investigation complete! Returning to patrol.");
        
        this.LastKnownPlayerPosition = Vector3.zero;
        this.isActivelyInvestigating = false;
        wasPlayerVisibleLastFrame = false;
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);
        
        // --- REVISED LOGIC ---

        // EVENT 1: Player is SPOTTED (and we weren't just chasing them a second ago)
        if (isPlayerVisible)
        {
            Debug.Log("[MonsterBrain] Player spotted! Engaging chase.");
            this.isActivelyInvestigating = false;
            this.LastKnownPlayerPosition = Vector3.zero;
            if (patrolHistory != null) patrolHistory.Clear();
            this.provider.RequestGoal<KillPlayerGoal>();
        }
        // EVENT 2: LOST SIGHT OF PLAYER (but only after the grace period expires)
        else if (wasPlayerVisibleLastFrame && !isPlayerVisible)
        {
            // The check for 'isActivelyInvestigating' prevents starting a new investigation
            // while one is already in progress.
            if (!isActivelyInvestigating && playerTransform != null)
            {
                 this.LastKnownPlayerPosition = playerTransform.position;
                 this.isActivelyInvestigating = true;
                 Debug.Log($"[MonsterBrain] Saved last known position: {this.LastKnownPlayerPosition}");
                 this.provider.RequestGoal<InvestigateGoal>();
            }
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}