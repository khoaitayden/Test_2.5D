using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterMovementAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chasePlayerSatellite; // Orbit point near player
    [SerializeField] private Transform player;
    [SerializeField] private Light playerLight;

    [Header("Light Settings")]
    [SerializeField] private float minLight = 0f;   // Player light at minimum (e.g., range = 2)
    [SerializeField] private float maxLight = 10f;  // Player light at maximum (e.g., range = 10)

    [Header("Attack Timing")]
    [SerializeField] private float minAttackDelay = 1f;   // When light is MIN → attack fast
    [SerializeField] private float maxAttackDelay = 8f;   // When light is MAX → attack slow

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 1f;

    private NavMeshAgent agent;
    private bool isChasing = false;
    private Coroutine attackCycleCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing!");
            enabled = false;
            return;
        }
        agent.stoppingDistance = stoppingDistance;

        StartAttackCycle();
    }

    void Update()
    {
        if (isChasing)
        {
            agent.SetDestination(player.position);

            if (agent.pathStatus == NavMeshPathStatus.PathComplete && 
                agent.remainingDistance <= agent.stoppingDistance)
            {
                ReturnToOrbit();
            }
        }
        else
        {
            // Stay near orbit point
            if (chasePlayerSatellite != null && 
                Vector3.Distance(transform.position, chasePlayerSatellite.position) > 1f)
            {
                agent.SetDestination(chasePlayerSatellite.position);
            }
        }
    }

    void StartAttackCycle()
    {
        if (attackCycleCoroutine != null) StopCoroutine(attackCycleCoroutine);
        attackCycleCoroutine = StartCoroutine(AttackCycle());
    }

    IEnumerator AttackCycle()
    {
        while (true)
        {
            // Get current player light level (use range or intensity)
            float currentLight = playerLight ? playerLight.range : maxLight;
            currentLight = Mathf.Clamp(currentLight, minLight, maxLight);

            // Normalize: 0 = min light (dark), 1 = max light (bright)
            float lightNormalized = Mathf.InverseLerp(minLight, maxLight, currentLight);

            // Darker = more aggressive → shorter delay
            float delay = Mathf.Lerp(minAttackDelay, maxAttackDelay, lightNormalized);

            yield return new WaitForSeconds(delay);

            // Start chasing
            isChasing = true;
            
            // Wait until reached player
            while (isChasing)
            {
                yield return null;
            }
        }
    }

    void ReturnToOrbit()
    {
        isChasing = false;
        if (chasePlayerSatellite != null)
        {
            agent.SetDestination(chasePlayerSatellite.position);
        }
        // Attack cycle continues automatically
    }
}