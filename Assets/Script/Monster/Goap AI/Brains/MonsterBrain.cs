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
        var player = GameObject.FindWithTag("Player");
        if (player != null) 
            playerTransform = player.transform;

        // Set the default state of the world.
        this.provider.WorldData.SetState(new CanPatrol(), 1);

        // Tell the AI its one and only purpose in life.
        this.provider.RequestGoal<KillPlayerGoal>();
    }

    public void OnInvestigationComplete()
    {
        Debug.Log("[MonsterBrain] Investigation complete! Resetting state.");
        this.LastKnownPlayerPosition = Vector3.zero;
        this.provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
        // After searching, the monster is now free to patrol again.
        this.provider.WorldData.SetState(new CanPatrol(), 1);
    }

    private void Update()
    {
        bool isPlayerVisible = PlayerInSightSensor.IsPlayerInSight(this.agent, this.config);
        this.provider.WorldData.SetState(new IsPlayerInSight(), isPlayerVisible ? 1 : 0);

        bool justSpottedPlayer = isPlayerVisible && !wasPlayerVisibleLastFrame;
        bool justLostPlayer = !isPlayerVisible && wasPlayerVisibleLastFrame;

        if (justSpottedPlayer)
        {
            Debug.Log("[MonsterBrain] Player is now visible.");
            // When we see the player, we are no longer "patrolling".
            this.provider.WorldData.SetState(new CanPatrol(), 0);
            this.provider.WorldData.SetState(new HasSuspiciousLocation(), 0);
        }
        else if (justLostPlayer)
        {
            if (playerTransform != null)
            {
                this.LastKnownPlayerPosition = playerTransform.position;
                Debug.Log($"[MonsterBrain] Player is no longer visible. Saving last known position: {this.LastKnownPlayerPosition}");
                this.provider.WorldData.SetState(new HasSuspiciousLocation(), 1);
                // We are no longer patrolling when we have a clue.
                this.provider.WorldData.SetState(new CanPatrol(), 0);
            }
        }

        wasPlayerVisibleLastFrame = isPlayerVisible;
    }
}