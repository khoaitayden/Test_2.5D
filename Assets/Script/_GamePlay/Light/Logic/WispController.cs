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
    [SerializeField] private TransformSetSO activeChestsSet;

    [Header("References")]
    [SerializeField] private WispAnimationController animationController;
    [SerializeField] private PlayerMovement playerMovement; // Moved Here
    [SerializeField] private Transform mainCameraTransform; // Moved Here
    
    [Header("Guidance Settings")]
    [Tooltip("How directly must the player look at the objective? (1.0 = Perfect, 0.7 = 45 degrees)")]
    [Range(0f, 1f)]
    [SerializeField] private float lookThreshold = 0.85f;
    [Header("Area Light (Interaction Range)")]
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
    [Tooltip("Stops collecting if Energy % is higher than this (0.0 to 1.0)")]
    [SerializeField] private float energyThreshold = 0.8f;

    // --- State ---
    private TombstoneController _currentTargetTombstone;
    private HashSet<ILitObject> _currentlyLitObjects = new HashSet<ILitObject>();
    
    private float _initAreaIntensity;
    private float _initAreaRange;
    private float energyFactor;

    // Movement State
    private Vector3 currentVelocity = Vector3.zero;
    private float orbitAngle;
    private Collider[] hitColliders = new Collider[5]; 
    private PlayerMovement cachedPlayerMovement;
    private Transform cachedPlayerTransform;

    void Start()
    {
        if (mainCameraTransform == null && Camera.main != null) 
            mainCameraTransform = Camera.main.transform;

        if (areaMapLight)
        {
            _initAreaIntensity = areaMapLight.intensity;
            _initAreaRange = areaMapLight.range;
        }
        
        // Auto-find animation controller if on same object
        if (animationController == null) animationController = GetComponent<WispAnimationController>();

        orbitAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        if (currentEnergy.Value <= 0)
        {
            HandleDeath();
            return;
        }

        energyFactor = currentEnergy.Value / maxEnergy.Value;
        
        UpdateAreaLight();
        UpdateInteractions();
        
        // --- NEW LOGIC HERE ---
        CheckObjectiveGuidance();
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
        // MODE 2: FIND CHESTS (If empty handed)
        else
        {
            if (activeChestsSet != null)
            {
                // Check ALL chests. If we are looking at ANY of them, make the face excited.
                List<Transform> chests = activeChestsSet.GetItems();
                foreach (Transform chest in chests)
                {
                    if (chest == null) continue;
                    
                    if (IsLookingAt(chest.position))
                    {
                        isLookingAtInterestingThing = true;
                        break; // Found one, no need to check the rest
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

        float dot = Vector3.Dot(camForward, dirToTarget);
        return dot >= lookThreshold;
    }

    // --- MOVEMENT LOGIC (Moved from AnimationController) ---
    void LateUpdate()
    {
        if (currentEnergy.Value <= 0) return; // Stop moving if dead
        
        // Safety Check
        if (playerAnchor == null || playerAnchor.Value == null || mainCameraTransform == null) return;

        Transform currentPlayer = playerAnchor.Value;

        // Dynamic Cache: If the player transform changed (respawn), get the new Movement component
        if (currentPlayer != cachedPlayerTransform)
        {
            cachedPlayerTransform = currentPlayer;
            cachedPlayerMovement = currentPlayer.GetComponent<PlayerMovement>();
            // Fallback if not on the exact object (try parent/child logic if needed)
            if(cachedPlayerMovement == null) cachedPlayerMovement = playerMovement; 
        }

        // 1. Calculate Orbit
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius
        );

        // 2. Calculate Lag
        Vector3 lagOffset = Vector3.zero;
        if (cachedPlayerMovement != null && cachedPlayerMovement.IsMoving)
        {
            lagOffset = -currentPlayer.forward * followLag;
        }

        Vector3 targetPos = currentPlayer.position + orbitOffset + lagOffset + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        // 3. Obstacle Avoidance
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, collisionRadius, hitColliders, obstacleLayer);
        Vector3 avoidance = Vector3.zero;
        if (numHits > 0)
        {
            for (int i = 0; i < numHits; i++)
            {
                if (hitColliders[i] == null) continue;
                Vector3 pushDir = transform.position - hitColliders[i].ClosestPoint(transform.position);
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up;
                avoidance += pushDir.normalized * (1f - Mathf.Clamp01(pushDir.magnitude / collisionRadius)) * avoidanceStrength;
            }
        }

        // 4. Final Position & Camera Clip
        Vector3 finalPos = targetPos + avoidance;
        Vector3 toWisp = finalPos - mainCameraTransform.position;
        if (toWisp.magnitude < minDistanceFromCamera)
        {
            finalPos = mainCameraTransform.position + toWisp.normalized * minDistanceFromCamera;
        }

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, 0.2f);
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

    void UpdateInteractions()
    {
        float detectionRange = areaMapLight != null ? areaMapLight.range : 5f;
        
        HashSet<ILitObject> visibleObjects = new HashSet<ILitObject>();
        HashSet<TombstoneController> visibleTombstones = new HashSet<TombstoneController>();

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, detectionLayer);

        foreach (var col in hits)
        {
            if (col == null) continue;

            Vector3 targetCenter = col.bounds.center;
            Vector3 dir = targetCenter - transform.position;
            if (!Physics.Raycast(transform.position, dir, dir.magnitude, obstructionLayer))
            {
                ILitObject litObj = col.GetComponent<ILitObject>();
                if (litObj != null)
                {
                    visibleObjects.Add(litObj);
                    if (litObj is TombstoneController tomb && tomb.CurrentEnergy > 0)
                    {
                        visibleTombstones.Add(tomb);
                    }
                }
            }
        }

        TombstoneController bestTombstone = null;
        if (energyFactor < energyThreshold)
        {
            float minDst = float.MaxValue;
            foreach (var t in visibleTombstones)
            {
                float d = Vector3.Distance(transform.position, t.transform.position);
                if (d < minDst)
                {
                    minDst = d;
                    bestTombstone = t;
                }
            }
        }

        foreach (var obj in visibleObjects)
        {
            if (obj is TombstoneController tomb)
            {
                if (tomb == bestTombstone)
                {
                    if (!_currentlyLitObjects.Contains(tomb)) tomb.OnLit(LightSourceType.Wisp);
                }
                else
                {
                    if (_currentlyLitObjects.Contains(tomb)) tomb.OnUnlit(LightSourceType.Wisp);
                }
            }
            else
            {
                if (!_currentlyLitObjects.Contains(obj)) obj.OnLit(LightSourceType.Wisp);
            }
        }

        foreach (var oldObj in _currentlyLitObjects)
        {
            if (!visibleObjects.Contains(oldObj))
            {
                oldObj.OnUnlit(LightSourceType.Wisp);
            }
        }

        _currentlyLitObjects.Clear();
        foreach (var obj in visibleObjects)
        {
            if (obj is TombstoneController tomb)
            {
                if (tomb == bestTombstone) _currentlyLitObjects.Add(obj);
            }
            else
            {
                _currentlyLitObjects.Add(obj);
            }
        }
        
        _currentTargetTombstone = bestTombstone;
    }

    void HandleDeath()
    {
        if (areaMapLight) areaMapLight.enabled = false;

        foreach (var obj in _currentlyLitObjects)
        {
            obj.OnUnlit(LightSourceType.Wisp);
        }
        _currentlyLitObjects.Clear();
        _currentTargetTombstone = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}