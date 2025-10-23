// FILE TO REPLACE: MonsterBrain.cs (The Final State-Managed Version)
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
        this.provider.WorldData.SetState(new HasInvestigated(), 0); // Initialize so we can read it
        this.provider.RequestGoal<PatrolGoal>();
    }

    private void Update()
    {
        // 1. SENSE THE WORLD
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);
        
        // ============================ THE FINAL FIX IS HERE ============================
        
        // 2. CHECK FOR COMPLETED INVESTIGATION (Cleanup State)
        // Read the result from the last frame's actions.
        var hasInvestigatedState = this.provider.WorldData.GetWorldState(typeof(HasInvestigated));
        
        // If the 'HasInvestigated' key is true, it means the action just finished.
        if (hasInvestigatedState?.Value >= 1)
        {
            Debug.Log("[MonsterBrain] Investigation is complete. Resetting memory and returning to patrol.");
            
            // CRITICAL: Reset the memory. This makes the InvestigateGoal impossible to choose again.
            this.LastKnownPlayerPosition = Vector3.zero; 
            
            // CRITICAL: Reset the world key so this check only runs once.
            this.provider.WorldData.SetState(new HasInvestigated(), 0);
            
            this.provider.RequestGoal<PatrolGoal>();
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return; // Exit the Update loop to start fresh on the next frame.
        }

        // 3. HANDLE VISION CHANGES (High Priority Events)
        if (isPlayerVisible && !wasPlayerVisibleLastFrame)
        {
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
                this.provider.WorldData.SetState(new HasInvestigated(), 0); // Reset before starting a new one.
                this.provider.RequestGoal<InvestigateGoal>();
            }
            wasPlayerVisibleLastFrame = isPlayerVisible;
            return;
        }

        // 4. HANDLE IDLE STATE (Default Action)
        if (this.provider.CurrentGoal == null && this.provider.CurrentPlan == null)
        {
             this.provider.RequestGoal<PatrolGoal>();
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}