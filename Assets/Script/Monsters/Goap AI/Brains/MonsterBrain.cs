using System.Collections;
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
        
        // CRITICAL FIX: Stop the Provider from starting before we assign the AgentType
        if (provider != null)
        {
            provider.enabled = false;
        }
    }

    private IEnumerator Start()
    {
        // 1. Wait for the GOAP System to initialize
        yield return null; 

        // 2. Resolve Agent Type
        if (provider.AgentType == null)
        {
            var goap = Object.FindFirstObjectByType<GoapBehaviour>();
            
            if (goap == null)
            {
                Debug.LogError("[MonsterBrain] CRITICAL: No GoapBehaviour found in scene!");
                yield break;
            }

            try 
            {
                provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
            }
            catch (System.Exception)
            {
                Debug.LogError("[MonsterBrain] AgentType 'ScriptMonsterAgent' not found! Check your GoapBehaviour.");
                yield break;
            }
        }

        // 3. Setup Initial State
        UpdateGOAPState(); 
        
        // 4. ACTIVATE THE PROVIDER (Safe now)
        provider.enabled = true;
        
        // 5. Request Goal
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
        if (provider != null)
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