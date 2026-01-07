using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private Transform feetPosition;

    [Header("State Data")]
    [SerializeField] private BoolVariableSO isSprintingState;    // Drag "var_IsSprinting"
    [SerializeField] private BoolVariableSO isSlowWalkingState;  // Create/Drag "var_IsSlowWalking"

    [Header("Step Settings")]
    [Tooltip("Distance traveled before playing a step when walking.")]
    [SerializeField] private float strideWalk = 0.5f;
    [Tooltip("Distance traveled before playing a step when sprinting.")]
    [SerializeField] private float strideSprint = 0.8f;
    [SerializeField] private float velocityThreshold = 0.1f;

    [Header("Sound Definitions")]
    [SerializeField] private SoundDefinition sfx_GenericDirt;
    [SerializeField] private SoundDefinition sfx_Grass;
    [SerializeField] private SoundDefinition sfx_Stone;
    [SerializeField] private SoundDefinition sfx_Wood;
    [SerializeField] private SoundDefinition sfx_TreeBranch;
    [SerializeField] private SoundDefinition sfx_Log;
    
    [Space(10)]
    [SerializeField] private SoundDefinition sfx_Jump;
    [SerializeField] private SoundDefinition sfx_Land;

    private float _distanceTraveled;
    private bool _isMoving;

    private void Start()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (groundedChecker == null) groundedChecker = GetComponent<PlayerGroundedChecker>();

        // Subscribe to the specific high-fidelity landing event from GroundedChecker
        if (groundedChecker != null)
        {
            groundedChecker.OnLandWithFallIntensity += PlayLand;
        }
    }

    private void OnDestroy()
    {
        if (groundedChecker != null)
        {
            groundedChecker.OnLandWithFallIntensity -= PlayLand;
        }
    }

    private void Update()
    {
        // Safety check if player is dead or special state (can check PlayerHealth here if needed)
        // For now, assuming if velocity is zero, no steps.
        
        HandleStrideFootsteps();
    }

    // --- PUBLIC API (Linked via GameEventListener in Inspector) ---
    
    public void PlayJump() 
    {
        PlaySoundInternal(sfx_Jump);
    }
    
    // Called by C# Event from PlayerGroundedChecker
    public void PlayLand(float fallIntensity)
    {
        if (SoundManager.Instance != null && sfx_Land != null)
        {
            float volMod = Mathf.Clamp(fallIntensity / 5f, 0.8f, 1.5f);
            SoundManager.Instance.PlaySound(sfx_Land, transform.position, volMod);
        }
    }

    // --- Internal Logic ---

    private void OnTriggerEnter(Collider other)
    {
        // Immediate trigger logic for branches/logs
        SurfaceIdentifier surface = other.GetComponent<SurfaceIdentifier>();
        
        if (surface != null)
        {
            if (surface.type == SurfaceType.TreeBranch || surface.type == SurfaceType.Log)
            {
                SoundDefinition soundToPlay = GetSoundForSurfaceType(surface.type);
                PlaySoundInternal(soundToPlay);
                _distanceTraveled = 0f;
            }
        }
    }

    private void HandleStrideFootsteps()
    {
        // Use CharacterController velocity (includes external forces) or PlayerMovement velocity
        Vector3 horizontalVel = characterController.velocity;
        horizontalVel.y = 0;
        float speed = horizontalVel.magnitude;

        // Don't play steps if not on ground
        if (groundedChecker != null && !groundedChecker.IsGrounded)
        {
            _distanceTraveled = 0f;
            return;
        }

        _isMoving = speed > velocityThreshold;

        if (!_isMoving) 
        {
            _distanceTraveled = 0f;
            return;
        }

        // Determine stride based on Data SO
        bool isSprinting = isSprintingState != null && isSprintingState.Value;
        float currentStride = isSprinting ? strideSprint : strideWalk;

        _distanceTraveled += speed * Time.deltaTime;

        if (_distanceTraveled >= currentStride)
        {
            PlayRaycastFootstep();
            _distanceTraveled = 0f;
        }
    }

    private void PlayRaycastFootstep()
    {
        SoundDefinition soundToPlay = sfx_GenericDirt; 

        RaycastHit hit;
        if (Physics.Raycast(feetPosition.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            SurfaceIdentifier surface = hit.collider.GetComponent<SurfaceIdentifier>();
            
            if (surface != null)
            {
                soundToPlay = GetSoundForSurfaceType(surface.type);
            }
            else if (hit.collider.GetComponent<Terrain>() != null)
            {
                TerrainDetector detector = hit.collider.GetComponent<TerrainDetector>();
                if (detector != null)
                {
                    int textureIndex = detector.GetDominantTextureIndex(hit.point);
                    if (textureIndex == 3) soundToPlay = sfx_Grass;
                    else soundToPlay = sfx_GenericDirt; 
                }
            }
        }

        PlaySoundInternal(soundToPlay);
    }

    private SoundDefinition GetSoundForSurfaceType(SurfaceType type)
    {
        switch (type)
        {
            case SurfaceType.Wood:       return sfx_Wood;
            case SurfaceType.TreeBranch: return sfx_TreeBranch;
            case SurfaceType.Stone:      return sfx_Stone;
            case SurfaceType.Log:        return sfx_Log;
            case SurfaceType.Grass:      return sfx_Grass;
            default:                     return sfx_GenericDirt;
        }
    }

    private void PlaySoundInternal(SoundDefinition soundDef)
    {
        if (soundDef == null) return;

        float volMultiplier = 1f;
        float pitchMultiplier = 1f;

        bool isSprinting = isSprintingState != null && isSprintingState.Value;
        bool isSlowWalking = isSlowWalkingState != null && isSlowWalkingState.Value;

        if (isSprinting)
        {
            volMultiplier = 1.2f;   
            pitchMultiplier = 1.1f; 
        }
        else if (isSlowWalking)
        {
            volMultiplier = 0.5f;   
            pitchMultiplier = 0.9f; 
        }

        SoundManager.Instance.PlaySound(soundDef, feetPosition.position, volMultiplier, pitchMultiplier);
    }
}