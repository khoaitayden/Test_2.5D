// FILE TO EDIT: MonsterBrain.cs
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config; // Cache the config

    private void Awake()
    {
        this.agent = this.GetComponent<AgentBehaviour>();
        this.provider = this.GetComponent<GoapActionProvider>();
        this.config = this.GetComponent<MonsterConfig>(); // Get the config here

        // Other setup code remains the same...
        var goap = FindFirstObjectByType<GoapBehaviour>();
        if (this.provider.AgentTypeBehaviour == null && goap != null)
        {
            this.provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
        }
    }

    private void Start()
    {
        this.provider.WorldData.SetState(new PlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        // ======================= THE NEW LOGIC FLOW =======================

        // 1. MANUALLY SENSE THE WORLD
        // Call the public static method from our sensor to check for the player.
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);

        // 2. FORCE-UPDATE THE GOAP WORLD STATE
        // We tell the GoapActionProvider what the state of the world is.
        // This is the key step that breaks the deadlock.
        this.provider.WorldData.SetState(new PlayerInSight(), isPlayerVisible ? 1 : 0);
        
        // 3. READ THE WORLD STATE AND MAKE A DECISION
        // This logic is now reliable because we know the world state is up-to-date.
        var currentGoal = this.provider.CurrentPlan?.Goal;
        
        if (currentGoal == null) return;
        
        // Switch to KillPlayerGoal if player is VISIBLE and we aren't already chasing.
        if (isPlayerVisible && currentGoal is not KillPlayerGoal)
        {
            Debug.Log("[MonsterBrain] Player is visible! Switching to KillPlayerGoal.");
            this.provider.RequestGoal<KillPlayerGoal>();
        }
        // Switch back to PatrolGoal if player is LOST and we were chasing.
        else if (!isPlayerVisible && currentGoal is KillPlayerGoal)
        {
            Debug.Log("[MonsterBrain] Player lost sight! Switching back to PatrolGoal.");
            this.provider.RequestGoal<PatrolGoal>();
        }
    }
}