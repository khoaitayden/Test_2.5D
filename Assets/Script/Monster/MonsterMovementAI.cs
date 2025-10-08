using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterMovementAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chasePlayerSatellite; // Orbit point
    [SerializeField] private Transform player;
    [SerializeField] private Light playerLight;

    [Header("Light Settings")]
    [SerializeField] private float minLight = 2f;    // Min light range (e.g., when nearly dead)
    [SerializeField] private float maxLight = 10f;   // Max light range

    [Header("Attack Timing")]
    [SerializeField] private float minAttackDelay = 1f;   // Attack fast when dark
    [SerializeField] private float maxAttackDelay = 8f;   // Attack slow when bright

    [Header("Movement")]
    [SerializeField] private float attackDistance = 1.5f; // Distance to "attack"
    [SerializeField] private float orbitDistanceThreshold = 1f; // How close to orbit point is "close enough"

    private NavMeshAgent agent;
    private bool isChasing = false;
    private bool hasAttacked = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing!");
            enabled = false;
            return;
        }

        // Start at orbit
        GoToOrbit();
        StartCoroutine(AttackCycle());
    }

    void Update()
    {
        if (isChasing && !hasAttacked)
        {
            // Check if close enough to attack
            if (Vector3.Distance(transform.position, player.position) <= attackDistance)
            {
                OnAttack();
            }
        }
        else if (!isChasing)
        {
            // Gently stay near orbit point
            if (chasePlayerSatellite != null &&
                Vector3.Distance(transform.position, chasePlayerSatellite.position) > orbitDistanceThreshold)
            {
                agent.SetDestination(chasePlayerSatellite.position);
            }
        }
    }

    IEnumerator AttackCycle()
    {
        while (enabled)
        {
            // Get current light level from player's light range
            float currentLight = playerLight ? playerLight.range : maxLight;
            currentLight = Mathf.Clamp(currentLight, minLight, maxLight);

            // Normalize: 0 = dark (minLight), 1 = bright (maxLight)
            float lightNormalized = Mathf.InverseLerp(minLight, maxLight, currentLight);

            // Darker = more aggressive = shorter delay
            float delay = Mathf.Lerp(minAttackDelay, maxAttackDelay, lightNormalized);

            yield return new WaitForSeconds(delay);

            // Start chase (only if not already chasing)
            if (!isChasing)
            {
                StartChase();
            }
        }
    }

    void StartChase()
    {
        isChasing = true;
        hasAttacked = false;
        agent.SetDestination(player.position);
    }

    void OnAttack()
    {
        hasAttacked = true;
        Debug.Log("Monster attacks player!");

        // TODO: Play animation, deal damage, etc.

        // After attack, return to orbit
        Invoke(nameof(GoToOrbit), 0.5f); // Optional: short delay before retreating
    }

    void GoToOrbit()
    {
        isChasing = false;
        hasAttacked = false;
        if (chasePlayerSatellite != null)
        {
            agent.SetDestination(chasePlayerSatellite.position);
        }
    }
}