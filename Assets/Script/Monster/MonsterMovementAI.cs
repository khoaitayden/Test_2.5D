using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class MonsterMovementAI : MonoBehaviour, ILitObject
{
    // Simple state machine for our AI
    private enum AIState
    {
        Chasing,
        Stunned
    }

    [Header("References")]
    [SerializeField] private Transform player;
    private NavMeshAgent agent;

    [Header("AI Settings")]
    [SerializeField] private float pathUpdateDelay = 0.25f; // How often to update the path (performance)
    
    private AIState currentState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = AIState.Chasing;
        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Chasing:
                Chasing();
                break;
            case AIState.Stunned:
                Stunned();
                break;
        }
    }


    private void Chasing()
    {
        UpdatePath();
    }
    
    private void Stunned()
    {
        agent.isStopped = true;
    }

    // --- Pathfinding ---
    private IEnumerator UpdatePath()
    {
        while (true)
        {
            if (currentState == AIState.Chasing)
            {
                agent.SetDestination(player.position);
            }
            yield return new WaitForSeconds(pathUpdateDelay);
        }
    }

    public void OnLit()
    {
        currentState = AIState.Stunned;
        agent.isStopped = true; 
    }

    public void OnUnlit()
    {
        agent.isStopped = false; 
        currentState = AIState.Chasing; 

    }
}