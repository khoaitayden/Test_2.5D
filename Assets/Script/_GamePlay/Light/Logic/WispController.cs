using UnityEngine;
using System.Collections.Generic;

public class WispController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    [SerializeField] private TransformAnchorSO playerAnchor; 
    [SerializeField] private TransformAnchorSO beaconAnchor;   
    [SerializeField] private BoolVariableSO isCarryingItem; 
    [SerializeField] private TransformSetSO activeObjectivesSet;

    [Header("References")]
    [SerializeField] private WispAnimationController animationController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform mainCameraTransform;
    
    [Header("Guidance Settings")]
    [Range(0f, 1f)] [SerializeField] private float lookThreshold = 0.85f;

    [Header("Area Light")]
    [SerializeField] private Light areaMapLight; 

    [Header("Orbit & Movement Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitHeight = 1.5f;
    [SerializeField] private float orbitSpeed = 40f;
    [SerializeField] private float followLag = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float minDistanceFromCamera = 1.0f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer; 
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private float avoidanceStrength = 5f;

    [Header("Soul Collection")]
    [SerializeField] private LayerMask detectionLayer;   
    [SerializeField] private LayerMask obstructionLayer; 
    [SerializeField] private float energyThreshold = 0.8f;

    // --- State ---
    private HashSet<ILitObject> _currentlyLitObjects = new HashSet<ILitObject>();
    private TombstoneController _currentTargetTombstone;
    
    private float _initAreaIntensity;
    private float _initAreaRange;
    private float energyFactor;

    // Movement State
    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;
    private Collider[] hitColliders = new Collider[5]; // Fixed array for non-alloc physics
    private PlayerMovement cachedPlayerMovement;
    private Transform cachedPlayerTransform;

    void Start()
    {
        InitializeReferences();
        orbitAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        if (currentEnergy.Value <= 0)
        {
            HandleDeath();
            return;
        }

        // 1. Calculate Global State
        energyFactor = currentEnergy.Value / maxEnergy.Value;
        
        // 2. Update Visuals & Logic
        UpdateAreaLight();
        UpdateInteractions();
        CheckObjectiveGuidance();
    }

    void LateUpdate()
    {
        if (currentEnergy.Value <= 0) return;
        
        // 1. Get Valid Player
        Transform currentPlayer = GetPlayerTransform();
        if (currentPlayer == null) return;

        // 2. Calculate Desired Position Pipeline
        UpdateOrbitAngle();
        Vector3 basePos = CalculateOrbitPosition(currentPlayer);
        Vector3 lagOffset = CalculateLagOffset(currentPlayer);
        Vector3 bobOffset = Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        Vector3 targetPos = basePos + lagOffset + bobOffset;

        // 3. Apply Physical Constraints
        Vector3 avoidance = CalculateObstacleAvoidance(transform.position);
        Vector3 finalPos = targetPos + avoidance;
        
        finalPos = ApplyCameraClipping(finalPos);

        // 4. Move
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, 0.2f);
    }

    private Transform GetPlayerTransform()
    {
        if (playerAnchor == null || playerAnchor.Value == null || mainCameraTransform == null) return null;

        Transform currentPlayer = playerAnchor.Value;

        // Refresh cache if player object changed (respawn)
        if (currentPlayer != cachedPlayerTransform)
        {
            cachedPlayerTransform = currentPlayer;
            cachedPlayerMovement = currentPlayer.GetComponent<PlayerMovement>();
            if(cachedPlayerMovement == null) cachedPlayerMovement = playerMovement; 
        }
        return currentPlayer;
    }

    private void UpdateOrbitAngle()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;
    }

    private Vector3 CalculateOrbitPosition(Transform player)
    {
        return player.position + new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );
    }

    private Vector3 CalculateLagOffset(Transform player)
    {
        if (cachedPlayerMovement != null && cachedPlayerMovement.IsMoving)
        {
            return -player.forward * followLag;
        }
        return Vector3.zero;
    }

    private Vector3 CalculateObstacleAvoidance(Vector3 currentPos)
    {
        // FIX: Replaced 'null' with 'hitColliders' array to prevent garbage allocation
        int numHits = Physics.OverlapSphereNonAlloc(currentPos, collisionRadius, hitColliders, obstacleLayer);
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < numHits; i++)
        {
            if (hitColliders[i] == null) continue;
            Vector3 pushDir = currentPos - hitColliders[i].ClosestPoint(currentPos);
            
            if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up; // Prevent divide by zero inside collider
            
            avoidance += pushDir.normalized * (1f - Mathf.Clamp01(pushDir.magnitude / collisionRadius)) * avoidanceStrength;
        }
        return avoidance;
    }

    private Vector3 ApplyCameraClipping(Vector3 targetPos)
    {
        Vector3 toWisp = targetPos - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            return mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }
        return targetPos;
    }


    private void InitializeReferences()
    {
        if (mainCameraTransform == null && Camera.main != null) 
            mainCameraTransform = Camera.main.transform;

        if (areaMapLight)
        {
            _initAreaIntensity = areaMapLight.intensity;
            _initAreaRange = areaMapLight.range;
        }
        
        if (animationController == null) animationController = GetComponent<WispAnimationController>();
    }

    private void CheckObjectiveGuidance()
    {
        if (animationController == null || mainCameraTransform == null) return;

        bool isLookingAtInterestingThing = false;

        // MODE 1: RETURN TO BEACON (If carrying Item)
        if (isCarryingItem != null && isCarryingItem.Value)
        {
            if (beaconAnchor != null && beaconAnchor.Value != null)
            {
                isLookingAtInterestingThing = IsLookingAt(beaconAnchor.Value.position);
            }
        }
        // MODE 2: FIND ANY ACTIVE AREA (If empty handed)
        else
        {
            if (activeObjectivesSet != null)
            {
                // Iterate through the Runtime Set
                List<Transform> objectives = activeObjectivesSet.GetItems();
                
                foreach (Transform target in objectives)
                {
                    if (target == null) continue;
                    
                    // If we match ANY of the active objectives, get excited
                    if (IsLookingAt(target.position))
                    {
                        isLookingAtInterestingThing = true;
                        break; 
                    }
                }
            }
        }

        animationController.SetFaceExpression(isLookingAtInterestingThing);
    }

    private bool IsLookingAt(Vector3 targetPos)
    {
        Vector3 camPos = mainCameraTransform.position;
        Vector3 dirToTarget = (targetPos - camPos).normalized;
        Vector3 camForward = mainCameraTransform.forward;
        return Vector3.Dot(camForward, dirToTarget) >= lookThreshold;
    }

    void UpdateInteractions()
    {
        float detectionRange = areaMapLight != null ? areaMapLight.range : 5f;
        
        // 1. Scan Area
        HashSet<ILitObject> visibleObjects = ScanForLitObjects(detectionRange);
        
        // 2. Determine Priority Target (Tombstones)
        TombstoneController bestTombstone = null;
        if (energyFactor < energyThreshold)
        {
            bestTombstone = FindBestTombstone(visibleObjects);
        }

        // 3. Apply States
        ApplyLitStates(visibleObjects, bestTombstone);
        
        _currentTargetTombstone = bestTombstone;
    }

    private HashSet<ILitObject> ScanForLitObjects(float range)
    {
        HashSet<ILitObject> foundObjects = new HashSet<ILitObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, range, detectionLayer);

        foreach (var col in hits)
        {
            if (col == null) continue;
            Vector3 dir = col.bounds.center - transform.position;
            
            // Line of Sight check
            if (!Physics.Raycast(transform.position, dir, dir.magnitude, obstructionLayer))
            {
                ILitObject litObj = col.GetComponent<ILitObject>();
                if (litObj != null) foundObjects.Add(litObj);
            }
        }
        return foundObjects;
    }

    private TombstoneController FindBestTombstone(HashSet<ILitObject> objects)
    {
        TombstoneController best = null;
        float minDst = float.MaxValue;

        foreach (var obj in objects)
        {
            if (obj is TombstoneController tomb && tomb.CurrentEnergy > 0)
            {
                float d = Vector3.Distance(transform.position, tomb.transform.position);
                if (d < minDst) { minDst = d; best = tomb; }
            }
        }
        return best;
    }

    private void ApplyLitStates(HashSet<ILitObject> visibleObjects, TombstoneController priorityTarget)
    {
        // A. Handle objects currently in view
        foreach (var obj in visibleObjects)
        {
            // Special handling for Tombstones (only light 1 at a time if prioritized)
            if (obj is TombstoneController tomb)
            {
                bool isPriority = (tomb == priorityTarget);
                
                if (isPriority && !_currentlyLitObjects.Contains(tomb))
                    tomb.OnLit(LightSourceType.Wisp);
                else if (!isPriority && _currentlyLitObjects.Contains(tomb))
                    tomb.OnUnlit(LightSourceType.Wisp);
            }
            // Standard objects
            else if (!_currentlyLitObjects.Contains(obj))
            {
                obj.OnLit(LightSourceType.Wisp);
            }
        }

        // B. Handle objects that LEFT the view
        foreach (var oldObj in _currentlyLitObjects)
        {
            if (!visibleObjects.Contains(oldObj)) oldObj.OnUnlit(LightSourceType.Wisp);
        }

        // C. Update Cache
        _currentlyLitObjects.Clear();
        foreach (var obj in visibleObjects)
        {
            // Only add tombstones if they are the priority one
            if (obj is TombstoneController tomb)
            {
                if (tomb == priorityTarget) _currentlyLitObjects.Add(obj);
            }
            else
            {
                _currentlyLitObjects.Add(obj);
            }
        }
    }

    void UpdateAreaLight()
    {
        if (areaMapLight)
        {
            areaMapLight.enabled = true;
            areaMapLight.intensity = Mathf.Lerp(0f, _initAreaIntensity, energyFactor);
            areaMapLight.range = Mathf.Lerp(5f, _initAreaRange, energyFactor);
        }
    }

    void HandleDeath()
    {
        if (areaMapLight) areaMapLight.enabled = false;
        foreach (var obj in _currentlyLitObjects) obj.OnUnlit(LightSourceType.Wisp);
        _currentlyLitObjects.Clear();
        _currentTargetTombstone = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}