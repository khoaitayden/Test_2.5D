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
    private bool isInvestigating = false;

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
        // Initialize world state
        this.provider.WorldData.SetState(new HasInvestigated(), 0);
        this.provider.WorldData.SetState(new IsPlayerInSight(), 0);
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);
        
        // CHECK 1: Is investigation complete?
        var hasInvestigatedState = this.provider.WorldData.GetWorldState(typeof(HasInvestigated));
        if (hasInvestigatedState?.Value >= 1)
        {
            Debug.Log("[MonsterBrain] Investigation complete! Returning to patrol.");
            
            // Reset everything
            this.LastKnownPlayerPosition = Vector3.zero;
            this.provider.WorldData.SetState(new HasInvestigated(), 0);
            this.isInvestigating = false;
            this.provider.RequestGoal<PatrolGoal>();
            
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        // CHECK 2: Detect if we're currently investigating
        var currentGoal = this.provider.CurrentPlan?.Goal;
        if (currentGoal is InvestigateGoal)
        {
            isInvestigating = true;
        }

        // CHECK 3: Handle vision state changes
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Player spotted! Engaging chase.");
            this.isInvestigating = false;
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
                this.provider.WorldData.SetState(new HasInvestigated(), 0);
                this.isInvestigating = true;
                this.provider.RequestGoal<InvestigateGoal>();
                Debug.Log($"[MonsterBrain] Saved last known position: {this.LastKnownPlayerPosition}");
            }
            
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        // CHECK 4: Handle idle state (no goal)
        if (currentGoal == null && !isInvestigating)
        {
            Debug.Log("[MonsterBrain] No active goal. Defaulting to patrol.");
            this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}