using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    // --- THE MEMORY ---
    public bool IsPlayerVisible { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; } 
    public Transform CurrentPlayerTarget { get; private set; }
    
    // Logic States
    public bool IsInvestigating { get; private set; }
    
    // Components
    private GoapActionProvider provider;
    
    private void Awake()
    {
        provider = GetComponent<GoapActionProvider>();
    }

    // FIX: Moved Goal Request to Start() so the AgentType has time to initialize
    private void Start()
    {
        // 1. Auto-assign AgentType if missing
        if (provider.AgentType == null)
        {
            // Find the main GOAP Controller in the scene
            var goap = Object.FindFirstObjectByType<GoapBehaviour>();
            
            if (goap != null)
            {
                // Assign the 'ScriptMonsterAgent' type we defined in the factory
                provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
            }
            else
            {
                Debug.LogError("[MonsterBrain] Critical: No GoapBehaviour found in scene!");
                return;
            }
        }

        // 2. NOW it is safe to request goals
        UpdateGOAPState(); 
        provider.RequestGoal<KillPlayerGoal>();
    }

    // --- INPUTS ---

    public void OnPlayerSeen(Transform player)
    {
        IsPlayerVisible = true;
        CurrentPlayerTarget = player;
        LastKnownPlayerPosition = player.position;

        if (IsInvestigating)
        {
            IsInvestigating = false;
        }
        
        UpdateGOAPState();
    }

    public void OnPlayerLost()
    {
        if (IsPlayerVisible)
        {
            IsPlayerVisible = false;
            IsInvestigating = true; 
            CurrentPlayerTarget = null;
            Debug.Log($"[Brain] Player Lost. Switched to Memory at {LastKnownPlayerPosition}");
        }
        
        UpdateGOAPState();
    }

    public void OnInvestigationFinished()
    {
        IsInvestigating = false;
        LastKnownPlayerPosition = Vector3.zero;
        UpdateGOAPState();
    }

    public void OnInvestigationFailed()
    {
        OnInvestigationFinished();
    }

    public void OnArrivedAtSuspiciousLocation()
    {
        provider.WorldData.SetState(new IsAtSuspiciousLocation(), 1);
    }

    // --- INTERNAL HELPER ---
    
    private void UpdateGOAPState()
    {
        if (provider == null) return;

        // Sync public bools to GOAP WorldData
        provider.WorldData.SetState(new IsPlayerInSight(), IsPlayerVisible ? 1 : 0);
        provider.WorldData.SetState(new IsInvestigating(), IsInvestigating ? 1 : 0);
        
        // Logic for "Can Patrol"
        bool busy = IsPlayerVisible || IsInvestigating;
        provider.WorldData.SetState(new CanPatrol(), busy ? 0 : 1);
        
        // Logic for "Has Suspicious Location"
        bool hasLoc = LastKnownPlayerPosition != Vector3.zero;
        provider.WorldData.SetState(new HasSuspiciousLocation(), hasLoc ? 1 : 0);
    }
}