using UnityEngine;
using UnityEngine.AI;

public class MonsterMovementAI : MonoBehaviour
{
    public Transform player; // Assign the player GameObject here in Inspector
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on this GameObject!");
        }
    }

    void Update()
    {
        if (player != null && agent != null)
        {
            agent.SetDestination(player.position);
        }
    }
}