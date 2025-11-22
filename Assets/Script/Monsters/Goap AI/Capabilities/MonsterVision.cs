using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MonsterConfig config;
    [SerializeField] private float detectionFrequency = 0.1f; // Run 10 times a second, not 60 (optimization)
    [SerializeField] private float sightLostDelay = 1.0f;     // The "Memory Buffer" - prevents stuttering
    
    [Header("Debug Read-Only")]
    [SerializeField] private bool canSeePlayerNow; 
    
    private MonsterBrain brain;
    private float scanTimer;
    private float timeSinceLastSeen;
    
    private void Awake()
    {
        brain = GetComponent<MonsterBrain>();
        if(config == null) config = GetComponent<MonsterConfig>();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;
        
        if (scanTimer >= detectionFrequency)
        {
            scanTimer = 0f;
            PerformVisionCheck();
        }
    }

    private void PerformVisionCheck()
    {
        // 1. Run the heavy physics logic (moved here from the Sensor)
        // using the static method you already had is fine, or the implementation below
        bool canSee = PlayerInSightSensor.IsPlayerInSight(GetComponent<CrashKonijn.Agent.Core.IActionReceiver>(), config);
        
        canSeePlayerNow = canSee;

        if (canSee)
        {
            // Reset buffer
            timeSinceLastSeen = 0f;
            
            // Identify WHO we see (Logic for multiple players support in future)
            // For now, assuming tag find for simplification, but ideally cached
            var player = GameObject.FindWithTag("Player")?.transform; // In prod, cache this
            
            if (player != null)
                brain.OnPlayerSeen(player);
        }
        else
        {
            // We DON'T see the player right now.
            // Do we switch state immediately? NO. We wait for the buffer.
            timeSinceLastSeen += detectionFrequency;
            
            if (timeSinceLastSeen > sightLostDelay)
            {
                // Ok, he's really gone.
                brain.OnPlayerLost();
            }
            // Else: We assume we still see him for AI stability
        }
    }
}