using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private Transform feetPosition;

    [Header("Step Settings")]
    [SerializeField] private float strideWalk = 0.5f;
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
        // Auto-find PlayerMovement if you forgot to drag it in
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        
        if (groundedChecker == null) groundedChecker = GetComponent<PlayerGroundedChecker>();

        if (groundedChecker != null)
        {
            groundedChecker.OnLandWithFallIntensity += PlayLand;
        }
        
        // Safety Check for Feet Position
        if (feetPosition == null)
        {
            Debug.LogError("PlayerAudio: 'Feet Position' is empty! Please assign the Feet transform in the Inspector.");
            this.enabled = false;
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
        HandleStrideFootsteps();
    }

    // --- PUBLIC API ---
    public void PlayJump() 
    {
        PlaySoundInternal(sfx_Jump);
    }
    
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
        // FIX: Don't use CharacterController.velocity. 
        // Use the calculated horizontal speed from our Logic script.
        float speed = 0f;
        if (playerMovement != null)
        {
            speed = playerMovement.CurrentHorizontalSpeed;
        }

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

        // Direct Input Access
        bool isSprinting = InputManager.Instance.IsSprinting;
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
        // Ensure feetPosition isn't null here
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
                    // Adjust this index (3) based on your specific terrain layer setup
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
        SoundManager.Instance.PlaySound(soundDef, feetPosition.position);
    }
}