// FILE TO EDIT: MonsterBrain.cs (FIXED)
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private bool wasPlayerVisibleLastFrame = false;

    private void Awake()
    {
        this.agent = this.GetComponent<AgentBehaviour>();
        this.provider = this.GetComponent<GoapActionProvider>();
        this.config = this.GetComponent<MonsterConfig>();
        var goap = FindFirstObjectByType<GoapBehaviour>();
        if (this.provider.AgentTypeBehaviour == null && goap != null)
            this.provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
    }

    private void Start()
    {
        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);
        
        var currentGoal = this.provider.CurrentPlan?.Goal;
        if (currentGoal == null) return;
        
        // --- Decision 1: Player spotted! ---
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            this.provider.RequestGoal<KillPlayerGoal>();
        }
        
        // --- Decision 2: Player lost! ---
        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            // FIX #1: Use our new, simpler method to get the player's position.
            // This avoids the 'ComponentReference' error.
            var lastSeenPosition = this.agent.GetComponent<PlayerCurrentPosSensor>().GetPlayerTarget();
            
            if (lastSeenPosition != null)
            {
                // FIX #2: The correct API call is provider.SetTarget, not provider.Memory.Set.
                this.provider.SetTarget(new PlayerLastSeenTarget(), lastSeenPosition);
                
                Debug.Log($"[MonsterBrain] PLAYER LOST! Stored last seen position. Switching to InvestigateGoal.");
                this.provider.RequestGoal<InvestigateGoal>();
            }
        }
        
        // --- Decision 3: Investigation over ---
        if (currentGoal is InvestigateGoal && this.provider.CurrentPlan == null && !isPlayerVisible)
        {
            this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}