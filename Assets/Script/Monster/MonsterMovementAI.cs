using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class MonsterMovementAI : MonoBehaviour
{
    [SerializeField] private Transform player;
    private NavMeshAgent agent;

    [SerializeField] private float chaseChance;
    [SerializeField] Light lightSource;
    private float chaseTimer;


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
        //agent.stoppingDistance=lightSource.range*0.8f;
        //agent.SetDestination(player.position);
    }

    void FixedUpdate()
    {
        Debug.Log("Get length by nav mesh" + GetPathLength(agent.path));
        Debug.Log("Get length by distance" + Vector3.Distance(transform.position, player.position));
        
    }

       
    public float GetPathLength( NavMeshPath path )
    {
        float lng = 0.0f;
       
        if (( path.status != NavMeshPathStatus.PathInvalid ) && ( path.corners.Length > 1 ))
        {
            for ( int i = 1; i < path.corners.Length; ++i )
            {
                lng += Vector3.Distance( path.corners[i-1], path.corners[i] );
            }
        }
       
        return lng;
    }
}