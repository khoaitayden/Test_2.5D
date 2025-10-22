// FILE TO EDIT: MonsterBrain.cs (Corrected)
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    // Make this property public so the sensor can read it.
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private bool wasPlayerVisibleLastFrame = false;

    // ... (Awake and Start methods are unchanged) ...
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
        
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            this.provider.RequestGoal<KillPlayerGoal>();
        }
        
        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            // CORRECTED LOGIC: We get the player's position and STORE IT LOCALLY.
            var lastSeenTarget = this.agent.GetComponent<PlayerCurrentPosSensor>().GetPlayerTarget();
            
            if (lastSeenTarget != null)
            {
                this.LastKnownPlayerPosition = lastSeenTarget.Position; // Set the public variable.
                
                Debug.Log($"[MonsterBrain] PLAYER LOST! Stored last seen position. Switching to InvestigateGoal.");
                this.provider.RequestGoal<InvestigateGoal>(); // Request the goal.
            }
        }
        
        if (currentGoal is InvestigateGoal && this.provider.CurrentPlan == null && !isPlayerVisible)
        {
            this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}