using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Goap.MonsterGen.Capabilities
{
    public class CoverFinder : MonoBehaviour
    {
        [SerializeField] private MonsterConfig config;
        private readonly List<Vector3> _foundPoints = new List<Vector3>();
        private NavMeshPath _testPath;

        private void Awake()
        {
            if (config == null) config = GetComponent<MonsterConfig>();
            _testPath = new NavMeshPath();
        }

        public List<Vector3> GetCoverPointsAround(Vector3 centerOfSearch, Vector3 monsterPos)
        {
            _foundPoints.Clear();
            float radius = config.investigateRadius;
            int rayCount = 16;
            float behindOffset = 3.0f;
            
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0, i * (360f / rayCount), 0) * Vector3.forward;

                if (Physics.Raycast(centerOfSearch, dir, out RaycastHit hit, radius, config.obstacleLayerMask))
                {
                    Vector3 hidingSpot = hit.point + dir * behindOffset;

                    if (NavMesh.SamplePosition(hidingSpot, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                    {
                        if (NavMesh.CalculatePath(monsterPos, navHit.position, NavMesh.AllAreas, _testPath) && _testPath.status == NavMeshPathStatus.PathComplete)
                        {
                            _foundPoints.Add(navHit.position);
                        }
                    }
                }
            }

            if (_foundPoints.Count > config.investigationPoints)
            {
                _foundPoints.Sort((a, b) => Vector3.Distance(monsterPos, a).CompareTo(Vector3.Distance(monsterPos, b)));
                return _foundPoints.GetRange(0, config.investigationPoints);
            }

            return _foundPoints;
        }
    }
}