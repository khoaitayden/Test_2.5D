using System;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    // This variable remains essential. It's the data associated with our new "fact".
    public Vector3 LastKnownPlayerPosition { get; private set; } = Vector3.zero;

    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private PatrolHistory patrolHistory;
    private Transform playerTransform;
    private bool stateChange = false;
    private bool justSpottedPlayer = false;
    private bool justLostPlayer = false;

    // This is still needed to detect the moment the player is lost.
    private bool wasPlayerVisibleLastFrame = false;

    // REMOVED: The isActivelyInvestigating flag is no longer needed.
    // The world state itself will now act as the flag.

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
        var player = GameObject.FindWithTag("Player");
        if (player != null) 
            playerTransform = player.transform;

        // Set the default state for our facts at the start of the game.
        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.WorldData.SetState(new HasSuspiciousLocation(), 0); // NEW: Initialize our new fact.

        // CHANGED: This is the ONLY place we will ever call RequestGoal.
        // It's necessary to give the AI its initial behavior.
    }

    public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Investigation complete! Resetting state.");
        
        this.LastKnownPlayerPosition = Vector3.zero;


        this.provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
        this.provider.RequestGoal<PatrolGoal,KillPlayerGoal,InvestigateGoal>(true);

    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);

        // This is the brain's primary job: report facts to the GOAP system.
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        // --- NEW AUTONOMOUS LOGIC ---
        if (justSpottedPlayer == isPlayerVisible && !wasPlayerVisibleLastFrame && justLostPlayer == !isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        justSpottedPlayer = isPlayerVisible && !wasPlayerVisibleLastFrame;
        justLostPlayer = !isPlayerVisible && wasPlayerVisibleLastFrame;
        
        // EVENT 1: Player is SPOTTED for the first time
        if (justSpottedPlayer)
        {
            Debug.Log("[MonsterBrain] Player is now visible.");
            if (patrolHistory != null) patrolHistory.Clear();
            this.provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
            
        }
        // EVENT 2: Just LOST SIGHT of the player
        else if (justLostPlayer)
        {
            if (playerTransform != null)
            {
                this.LastKnownPlayerPosition = playerTransform.position;
                Debug.Log($"[MonsterBrain] Player is no longer visible. Saving last known position: {this.LastKnownPlayerPosition}");
                this.provider.WorldData.SetState(new HasSuspiciousLocation(), 1);
            }
        } 
        this.provider.RequestGoal<PatrolGoal,KillPlayerGoal,InvestigateGoal>(true);
        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}