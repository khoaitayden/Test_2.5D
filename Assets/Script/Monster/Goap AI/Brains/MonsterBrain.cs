using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    // Initialize to zero so InvestigateGoal knows there's nothing to investigate yet
    public Vector3 LastKnownPlayerPosition { get; private set; } = Vector3.zero;

    private AgentBehaviour agent;
    private GoapActionProvider provider;
    private MonsterConfig config;
    private Transform playerTransform;
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
        this.provider.WorldData.SetState(new HasInvestigated(), 0); // Initialize investigation state
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        // PLAYER BECAME VISIBLE
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            Debug.Log("[MonsterBrain] Player spotted! Switching to KillPlayerGoal");
            this.provider.RequestGoal<KillPlayerGoal>();
            wasPlayerVisibleLastFrame = true;
            return;
        }
        
        // PLAYER LOST SIGHT
        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            // Cache player transform
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            
            if (playerTransform != null)
            {
                this.LastKnownPlayerPosition = playerTransform.position;
                this.provider.WorldData.SetState(new HasInvestigated(), 0); // Reset investigation
                Debug.Log($"[MonsterBrain] Lost sight of player at {LastKnownPlayerPosition}. Switching to InvestigateGoal");
                this.provider.RequestGoal<InvestigateGoal>();
            }
            wasPlayerVisibleLastFrame = false;
            return;
        }

        // IDLE STATE - return to patrol if no goal active
        if (this.provider.CurrentGoal == null && this.provider.CurrentPlan == null)
        {
            Debug.Log("[MonsterBrain] Idle detected. Returning to PatrolGoal");
            this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}