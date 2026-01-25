using System;
using System.Collections;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.MonsterGen;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class MonsterBrain : MonoBehaviour
{
    [Header("Architecture Dependencies")]
    [SerializeField] private TraceStorageSO traceStorage;
    [SerializeField] private TransformAnchorSO playerAnchor;
    
    [Header("Monster Agent")]
    [SerializeField] private string agentType;
    
    public TraceStorageSO TraceStorage => traceStorage;
    public TransformAnchorSO PlayerAnchor => playerAnchor;
    public bool IsPlayerVisible { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; } 
    public Transform CurrentPlayerTarget { get; private set; }
    public bool IsInvestigating { get; private set; }
    public bool IsFleeing { get; private set; }
    public float HandledNoiseTimestamp { get; private set; } = -1f;
    public float LastTimeSeenPlayer { get; private set; }
    public bool IsAttacking { get; set; }
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
            var goap = FindFirstObjectByType<GoapBehaviour>();
            if (goap != null) provider.AgentType = goap.GetAgentType(agentType);
        }
        UpdateGOAPState(); 
        provider.enabled = true;
        provider.RequestGoal<KillPlayerGoal>();
    }


    public void MarkNoiseAsHandled(float timestamp)
    {
        if (timestamp > HandledNoiseTimestamp)
        {
            HandledNoiseTimestamp = timestamp;
        }
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
    // Rename this method (and update references in AttackPlayerAction)
    public void OnMovementStuck()
    {
        Debug.Log("[Brain] Stuck! Engaging Flee Mode.");
        
        IsInvestigating = false; 

        IsPlayerVisible = false;
        CurrentPlayerTarget = null;
        LastKnownPlayerPosition = Vector3.zero;

        IsFleeing = true;
        UpdateGOAPState();
    }

    public void OnFleeComplete()
    {
        Debug.Log("[Brain] Flee complete. Returning to normal behavior.");
        IsFleeing = false;
        UpdateGOAPState();
    }
    private void UpdateGOAPState()
    {
        if (provider == null) return;
        provider.WorldData.SetState(new IsPlayerInSight(), IsPlayerVisible ? 1 : 0);
        provider.WorldData.SetState(new IsInvestigating(), IsInvestigating ? 1 : 0);
        
        provider.WorldData.SetState(new IsFleeing(), IsFleeing ? 1 : 0);


        bool busy = IsPlayerVisible || IsInvestigating || IsFleeing;
        provider.WorldData.SetState(new CanPatrol(), busy ? 0 : 1);
        
        bool hasLoc = LastKnownPlayerPosition != Vector3.zero;
        provider.WorldData.SetState(new HasSuspiciousLocation(), hasLoc ? 1 : 0);
    }
}