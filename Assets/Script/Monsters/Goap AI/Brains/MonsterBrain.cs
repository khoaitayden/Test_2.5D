using System.Collections;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    // --- EXISTING MEMORY ---
    public bool IsPlayerVisible { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; } 
    public Transform CurrentPlayerTarget { get; private set; }
    public bool IsInvestigating { get; private set; }
    
    // --- NEW NOISE MEMORY ---
    // The timestamp of the noise we have processed (or are currently processing).
    // Any noise older than this is considered "Old News" and ignored.
    public float HandledNoiseTimestamp { get; private set; } = -1f;

    private GoapActionProvider provider;
    
    private void Awake()
    {
        provider = GetComponent<GoapActionProvider>();
        if (provider != null) provider.enabled = false;
    }

    private IEnumerator Start()
    {
        yield return null; 
        if (provider.AgentType == null)
        {
            var goap = Object.FindFirstObjectByType<GoapBehaviour>();
            if (goap != null) provider.AgentType = goap.GetAgentType("ScriptMonsterAgent");
        }
        UpdateGOAPState(); 
        provider.enabled = true;
        provider.RequestGoal<KillPlayerGoal>();
    }

    // --- NOISE API (NEW) ---

    public void MarkNoiseAsHandled(float timestamp)
    {
        // We finished investigating this time.
        // Update memory so we don't look at it (or anything older) again.
        if (timestamp > HandledNoiseTimestamp)
        {
            HandledNoiseTimestamp = timestamp;
            // Debug.Log($"[Brain] Noise Handled. Ignoring traces older than: {HandledNoiseTimestamp}");
        }
    }

    // --- EXISTING INPUTS ---

    public void OnPlayerSeen(Transform player)
    {
        IsPlayerVisible = true;
        CurrentPlayerTarget = player;
        LastKnownPlayerPosition = player.position;
        if (IsInvestigating) IsInvestigating = false;
        UpdateGOAPState();
    }

    public void OnPlayerLost()
    {
        if (IsPlayerVisible)
        {
            IsPlayerVisible = false;
            IsInvestigating = true; 
            CurrentPlayerTarget = null;
        }
        UpdateGOAPState();
    }

    public void OnInvestigationFinished()
    {
        IsInvestigating = false;
        LastKnownPlayerPosition = Vector3.zero;
        UpdateGOAPState();
    }

    public void OnInvestigationFailed() => OnInvestigationFinished();

    public void OnArrivedAtSuspiciousLocation()
    {
        if (provider != null) provider.WorldData.SetState(new IsAtSuspiciousLocation(), 1);
    }
    
    private void UpdateGOAPState()
    {
        if (provider == null) return;
        provider.WorldData.SetState(new IsPlayerInSight(), IsPlayerVisible ? 1 : 0);
        provider.WorldData.SetState(new IsInvestigating(), IsInvestigating ? 1 : 0);
        bool busy = IsPlayerVisible || IsInvestigating;
        provider.WorldData.SetState(new CanPatrol(), busy ? 0 : 1);
        bool hasLoc = LastKnownPlayerPosition != Vector3.zero;
        provider.WorldData.SetState(new HasSuspiciousLocation(), hasLoc ? 1 : 0);
    }
}