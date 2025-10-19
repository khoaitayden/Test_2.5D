using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
public class MonsterBrain : MonoBehaviour
{
private AgentBehaviour agent;
private GoapActionProvider provider;
private GoapBehaviour goap;
private void Awake()
{
    this.goap = FindFirstObjectByType<GoapBehaviour>();
    this.agent = this.GetComponent<AgentBehaviour>();
    this.provider = this.GetComponent<GoapActionProvider>();
    
    if (this.goap == null)
    {
        Debug.LogError("[MonsterBrain] No GoapBehaviour found in scene! Create an empty GameObject and add GoapBehaviour component!");
        return;
    }
    
    // Set the agent type if not set
    if (this.provider.AgentTypeBehaviour == null)
    {
        var agentType = this.goap.GetAgentType("ScriptMonsterAgent");
        if (agentType != null)
        {
            this.provider.AgentType = agentType;
            Debug.Log("[MonsterBrain] Agent Type set to: ScriptMonsterAgent");
        }
        else
        {
            Debug.LogError("[MonsterBrain] Could not find AgentType 'ScriptMonsterAgent'! Make sure GoapBehaviour has MonsterAgentTypeFactory configured!");
        }
    }
}

private void Start()
{
    // CRITICAL: Initialize PlayerInSight to 0 so the planner knows this world key exists
    // Without this, goals that depend on PlayerInSight won't be evaluated!
    this.provider.WorldData.SetState(new PlayerInSight(), 0);
    
    // Start with patrol goal
    this.provider.RequestGoal<PatrolGoal>();
}

private void Update()
{
    // Manually check for player nearby and update world state
    var config = GetComponent<MonsterConfig>();
    if (config != null)
    {
        var colliders = new Collider[1];
        var count = Physics.OverlapSphereNonAlloc(
            transform.position, 
            config.ViewRadius, 
            colliders, 
            config.PlayerLayerMask
        );
        
        bool playerInSight = count > 0;
        
        // Update world state manually
        this.provider.WorldData.SetState(new PlayerInSight(), playerInSight ? 1 : 0);
        
        // Debug
        if (playerInSight && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[MonsterBrain] Player detected at distance: {Vector3.Distance(transform.position, colliders[0].transform.position):F2}");
        }
    }
    
    // Check world state and switch goals dynamically
    var worldData = this.provider.WorldData;
    
    // Check if player is in sight
    var playerInSightState = worldData.GetWorldState(typeof(PlayerInSight));
    bool playerNearby = playerInSightState?.Value >= 1;
    
    // Get current goal from CurrentPlan
    var currentGoal = this.provider.CurrentPlan?.Goal;
    
    // Don't do anything if we don't have a goal yet
    if (currentGoal == null)
        return;
    
    // Switch to KillPlayerGoal if player is in sight and not already chasing
    if (playerNearby && currentGoal is not KillPlayerGoal)
    {
        Debug.Log("[MonsterBrain] Player detected! Switching to KillPlayerGoal");
        this.provider.RequestGoal<KillPlayerGoal>();
    }
    // Switch back to PatrolGoal if player is lost and not already patrolling
    else if (!playerNearby && currentGoal is KillPlayerGoal)
    {
        Debug.Log("[MonsterBrain] Player lost! Switching to PatrolGoal");
        this.provider.RequestGoal<PatrolGoal>();
    }
}
}
