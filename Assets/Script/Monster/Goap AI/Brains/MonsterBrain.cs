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
    private bool isActivelyInvestigating = false;


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

  public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Received 'OnInvestigationComplete' signal from action.");
        this.isActivelyInvestigating = false;
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        // CHECK 1: HANDLE VISION CHANGES (Highest Priority)
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
            this.isActivelyInvestigating = false; // Seeing the player always cancels investigation.
            this.provider.RequestGoal<KillPlayerGoal>();
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }
        
        if (!isPlayerVisible && wasPlayerVisibleLastFrame)
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            if (playerTransform != null)
            {
                this.LastKnownPlayerPosition = playerTransform.position;
                this.isActivelyInvestigating = true; // We are now investigating.
                this.provider.RequestGoal<InvestigateGoal>();
            }
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        // CHECK 2: HANDLE IDLE STATE
        // If no plan is active AND we are not supposed to be investigating, then patrol.
        if (this.provider.CurrentPlan == null && !this.isActivelyInvestigating)
        {
             this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}