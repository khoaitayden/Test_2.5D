using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterMovementAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chasePlayerSatellite; // Safe orbit point
    [SerializeField] private Transform player;
    [SerializeField] private Light lightSource;

    [Header("Detection & Behavior")]
    [SerializeField] private float baseDetectionRange = 10f;
    [SerializeField] private float minAggressionChance = 0.1f; // 10% when light is max
    [SerializeField] private float maxAggressionChance = 0.9f; // 90% when light is off
    [SerializeField] private float requiredExposureTime = 2f; // Must be in range for this long
    [SerializeField] private float retreatWaitTime = 3f; // Time to wait at satellite before next try

    private NavMeshAgent agent;
    private float exposureTimer = 0f;
    private bool isExposed = false;
    private Coroutine retreatCoroutine;

    private enum State { Idle, Chasing, Retreating }
    private State currentState = State.Idle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
            enabled = false;
            return;
        }

        // Start at satellite point
        if (chasePlayerSatellite != null)
            agent.SetDestination(chasePlayerSatellite.position);
    }

    void Update()
    {
        float lightIntensity = GetEffectiveLightLevel();
        float detectionRange = baseDetectionRange * (1f - lightIntensity); // Darker = larger range

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool inRange = distanceToPlayer <= detectionRange;

        // Update exposure timer
        if (inRange)
        {
            exposureTimer += Time.deltaTime;
            isExposed = true;
        }
        else
        {
            exposureTimer = 0f;
            isExposed = false;
        }

        // Decide behavior based on state and conditions
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState(lightIntensity);
                break;
            case State.Chasing:
                HandleChasingState();
                break;
            case State.Retreating:
                // Handled by coroutine; just wait
                break;
        }
    }

    void HandleIdleState(float lightIntensity)
    {
        // If exposed long enough, maybe attack
        if (isExposed && exposureTimer >= requiredExposureTime)
        {
            // Calculate chance to attack (lower if light is bright)
            float attackChance = Mathf.Lerp(maxAggressionChance, minAggressionChance, lightIntensity);
            
            if (Random.value < attackChance)
            {
                // Start chasing
                agent.SetDestination(player.position);
                currentState = State.Chasing;
                exposureTimer = 0f;
            }
            else
            {
                // Too bright — retreat immediately
                StartRetreat();
            }
        }
        else
        {
            // Not exposed enough — stay near satellite
            if (chasePlayerSatellite != null && Vector3.Distance(transform.position, chasePlayerSatellite.position) > 1f)
            {
                agent.SetDestination(chasePlayerSatellite.position);
            }
        }
    }

    void HandleChasingState()
    {
        // Keep updating destination in case player moves
        agent.SetDestination(player.position);

        // If we get close enough, "attack" (you can trigger animation, damage, etc.)
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            OnAttackSuccess();
        }

        // If light becomes too bright during chase, abort
        float lightIntensity = GetEffectiveLightLevel();
        if (lightIntensity > 0.7f) // Threshold to abort
        {
            StartRetreat();
        }
    }

    void OnAttackSuccess()
    {
        Debug.Log("Monster attacks player!");
        // TODO: Play attack animation, deal damage, etc.

        // After attack, retreat
        StartRetreat();
    }

    void StartRetreat()
    {
        if (retreatCoroutine != null)
            StopCoroutine(retreatCoroutine);

        retreatCoroutine = StartCoroutine(RetreatRoutine());
    }

    IEnumerator RetreatRoutine()
    {
        currentState = State.Retreating;

        // Go back to satellite
        if (chasePlayerSatellite != null)
        {
            agent.SetDestination(chasePlayerSatellite.position);
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }
        }

        // Wait before allowing next attack
        yield return new WaitForSeconds(retreatWaitTime);

        currentState = State.Idle;
        exposureTimer = 0f;
    }

    // Normalize light: 0 = dark, 1 = max brightness
    float GetEffectiveLightLevel()
    {
        if (lightSource == null) return 0f;

        // Option 1: Use intensity (for directional/point lights)
        // return Mathf.Clamp01(lightSource.intensity / 8f); // assuming max intensity ~8

        // Option 2: Use range (since you mentioned light range earlier)
        float maxRange = 10f; // adjust to your light's max possible range
        return Mathf.Clamp01(lightSource.range / maxRange);
    }
}