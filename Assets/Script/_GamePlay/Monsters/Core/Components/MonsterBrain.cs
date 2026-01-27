using System.Collections;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public abstract class MonsterBrain : MonoBehaviour
{
    // --- SHARED DEPENDENCIES ---
    protected MonsterConfigBase config;
    protected GoapActionProvider provider;

    // --- PUBLIC PROPERTIES (For Sensors) ---
    public TraceStorageSO TraceStorage => config?.traceStorage;
    public TransformAnchorSO PlayerAnchor => config?.playerAnchor;

    // --- SHARED STATE ---
    public bool IsPlayerVisible { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; } 
    public Transform CurrentPlayerTarget { get; private set; }
    public bool IsInvestigating { get; private set; }
    public bool IsFleeing { get; protected set; }
    public bool IsAttacking { get; set; }
    
    public float HandledNoiseTimestamp { get; private set; } = -1f;
    public float LastTimeSeenPlayer { get; private set; }

    protected abstract string GetAgentTypeName();
    protected abstract void RequestInitialGoal();

    protected virtual void Awake()
    {
        provider = GetComponent<GoapActionProvider>();
        config = GetComponent<MonsterConfigBase>();
        
        if (provider != null) provider.enabled = false;
        if (config == null) Debug.LogError($"{name} requires a MonsterConfigBase component!", this);
    }

    protected virtual IEnumerator Start()
    {
        yield return null; // Wait for GOAP System to initialize

        if (provider.AgentType == null)
        {
            var goap = FindFirstObjectByType<GoapBehaviour>();
            if (goap != null) 
            {
                // Child class defines the name string
                provider.AgentType = goap.GetAgentType(GetAgentTypeName());
            }
        }

        UpdateGOAPState(); 
        provider.enabled = true;
        
        RequestInitialGoal();
    }


    public void MarkNoiseAsHandled(float timestamp)
    {
        if (timestamp > HandledNoiseTimestamp) HandledNoiseTimestamp = timestamp;
    }

    public void OnPlayerSeen(Transform player)
    {
        IsPlayerVisible = true;
        CurrentPlayerTarget = player;
        LastKnownPlayerPosition = player.position;
        LastTimeSeenPlayer = Time.time; 
        
        if (IsInvestigating) IsInvestigating = false;
        UpdateGOAPState();
    }

    public void OnPlayerLost()
    {
        if (IsPlayerVisible)
        {
            IsPlayerVisible = false;
            LastTimeSeenPlayer = Time.time;
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

    public void OnMovementStuck()
    {
        Debug.Log($"[{name}] Stuck! Engaging Flee Mode.");
        IsInvestigating = false; 
        IsPlayerVisible = false;
        CurrentPlayerTarget = null;
        LastKnownPlayerPosition = Vector3.zero;
        IsFleeing = true;
        UpdateGOAPState();
    }

    public void OnFleeComplete()
    {
        Debug.Log($"[{name}] Flee complete.");
        IsFleeing = false;
        UpdateGOAPState();
    }
    public void WipeMemory()
    {
        IsPlayerVisible = false;
        CurrentPlayerTarget = null;
        LastKnownPlayerPosition = Vector3.zero;
        LastTimeSeenPlayer = -1f;
        
        IsInvestigating = false;
        IsAttacking = false;
        
        // Force GOAP update immediately
        UpdateGOAPState();
    }
    public virtual void OnLitByFlashlight()
    {
    }
    protected virtual void UpdateGOAPState()
    {
        if (provider == null) return;
        provider.WorldData.SetState(new IsPlayerInSight(), IsPlayerVisible ? 1 : 0);
        provider.WorldData.SetState(new IsInvestigating(), IsInvestigating ? 1 : 0);
        provider.WorldData.SetState(new IsFleeing(), IsFleeing ? 1 : 0);

        bool busy = IsPlayerVisible || IsInvestigating || IsFleeing || IsAttacking;
        provider.WorldData.SetState(new CanPatrol(), busy ? 0 : 1);
        
        bool hasLoc = LastKnownPlayerPosition != Vector3.zero;
        provider.WorldData.SetState(new HasSuspiciousLocation(), hasLoc ? 1 : 0);
    }
}