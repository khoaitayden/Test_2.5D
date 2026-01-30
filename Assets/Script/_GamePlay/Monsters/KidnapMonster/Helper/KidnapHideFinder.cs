using UnityEngine;
using UnityEngine.AI;

public class KidnapHideFinder : MonoBehaviour
{
    [SerializeField] private KidnapMonsterConfig config;
    [SerializeField] private LayerMask treeLayer;

    private Transform _playerTransform;

    void Awake()
    {
        if(config == null) config = GetComponent<KidnapMonsterConfig>();
    }

    public void SetPlayer(Transform player)
    {
        _playerTransform = player;
    }

    public Vector3? FindBestHideSpot()
    {
        if (_playerTransform == null) return null;

        Collider[] hits = Physics.OverlapSphere(_playerTransform.position, config.findHideRadius, treeLayer);
        
        Vector3 bestSpot = Vector3.zero;
        float bestScore = float.MinValue;
        bool found = false;

        foreach (var tree in hits)
        {
            Vector3 treePos = tree.transform.position;
            
            Vector3 dirFromPlayer = (treePos - _playerTransform.position).normalized;
            
            Vector3 hidePos = treePos + (dirFromPlayer * 3.0f);

            
            float distToPlayer = Vector3.Distance(hidePos, _playerTransform.position);
            float distToSelf = Vector3.Distance(hidePos, transform.position);
            
            float score = distToPlayer - (distToSelf * 0.5f);

            if (score > bestScore)
            {
                if (NavMesh.SamplePosition(hidePos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                {

                    RaycastHit coverHit;
                    if (Physics.Raycast(_playerTransform.position, (hit.position - _playerTransform.position).normalized, out coverHit, distToPlayer))
                    {
                        if (coverHit.collider == tree)
                        {
                            bestSpot = hit.position;
                            bestScore = score;
                            found = true;
                        }
                    }
                }
            }
        }

        return found ? bestSpot : null;
    }
}