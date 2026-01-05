using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform feetPosition;

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
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }

    // --- NEW: IMMEDIATE TRIGGER LOGIC ---
    // This fires the moment the player's collider touches the Branch/Log Trigger
    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the object has a Surface Identifier
        SurfaceIdentifier surface = other.GetComponent<SurfaceIdentifier>();
        
        if (surface != null)
        {
            // 2. Only force play for "Single Event" items (Branches and Logs)
            // We don't want this for big floors (Wood/Stone) or we'd hear a step just by standing near a wall
            if (surface.type == SurfaceType.TreeBranch || surface.type == SurfaceType.Log)
            {
                SoundDefinition soundToPlay = GetSoundForSurfaceType(surface.type);
                PlaySoundInternal(soundToPlay);

                // 3. Reset distance so we don't play a "Stride" step immediately after this
                _distanceTraveled = 0f;
            }
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        if (playerController.IsDead || playerController.IsClimbing || !playerController.IsGrounded) 
        {
            _distanceTraveled = 0f;
            return;
        }

        HandleStrideFootsteps();
    }

    // --- Public API ---
    public void PlayJump() => PlaySoundInternal(sfx_Jump);
    
    public void PlayLand(float fallIntensity)
    {
        if (SoundManager.Instance != null && sfx_Land != null)
        {
            float volMod = Mathf.Clamp(fallIntensity / 5f, 0.8f, 1.5f);
            SoundManager.Instance.PlaySound(sfx_Land, transform.position, volMod);
        }
    }

    // --- Internal Logic ---

    private void HandleStrideFootsteps()
    {
        Vector3 horizontalVel = characterController.velocity;
        horizontalVel.y = 0;
        float speed = horizontalVel.magnitude;

        _isMoving = speed > velocityThreshold;

        if (!_isMoving) 
        {
            _distanceTraveled = 0f;
            return;
        }

        float currentStride = playerController.IsSprinting ? strideSprint : strideWalk;
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
        // Note: QueryTriggerInteraction.Collide ensures we still hear steps if walking ALONG a long log
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

    // Helper to keep the switch statement in one place
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

    // The actual audio player that applies volume/pitch based on speed
    private void PlaySoundInternal(SoundDefinition soundDef)
    {
        if (soundDef == null) return;

        float volMultiplier = 1f;
        float pitchMultiplier = 1f;

        if (playerController.IsSprinting)
        {
            volMultiplier = 1.2f;   
            pitchMultiplier = 1.1f; 
        }
        else if (playerController.IsSlowWalking)
        {
            volMultiplier = 0.5f;   
            pitchMultiplier = 0.9f; 
        }

        SoundManager.Instance.PlaySound(soundDef, feetPosition.position, volMultiplier, pitchMultiplier);
    }
}