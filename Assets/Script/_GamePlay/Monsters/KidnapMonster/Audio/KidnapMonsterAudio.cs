using UnityEngine;
using UnityEngine.AI;

public class KidnapMonsterAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform feetPosition;
    [SerializeField] private Transform bagPosition; 
    [SerializeField] private KidnapMonsterConfig config; 
    [Header("Audio Malipulate")]
    [SerializeField] private float footStepsVolumnMultiplier=0.5f;
    [Header("Footsteps")]
    [SerializeField] private float strideLength = 1.2f;
    [SerializeField] private float velocityThreshold = 0.5f;
    [SerializeField] private SoundDefinition sfx_Footstep_Dirt;
    [SerializeField] private SoundDefinition sfx_Footstep_Grass;
    [SerializeField] private SoundDefinition sfx_Footstep_Wood;
    [SerializeField] private SoundDefinition sfx_Footstep_Stone;

    [Header("Bag Dragging")]
    [SerializeField] private SoundDefinition sfx_BagDrag_Loop;
    [Header("Breath")]
    [SerializeField] private SoundDefinition sfx_Breath_Loop;
    [SerializeField] private float maxBreathPitchMultiplier = 1.5f;

    private float _distanceTraveled;
    private AudioSource _bagSource;
    private AudioSource _breathSource;
    private Transform _playerTransform;
    private float _baseBreathPitch;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (config == null) config = GetComponent<MonsterConfigBase>() as KidnapMonsterConfig;

        if (SoundManager.Instance != null)
        {
            if (sfx_BagDrag_Loop)
            {
                _bagSource = SoundManager.Instance.CreateLoop(sfx_BagDrag_Loop, bagPosition);
                _bagSource.volume = 0f; 
            }

            if (sfx_Breath_Loop)
            {
                _breathSource = SoundManager.Instance.CreateLoop(sfx_Breath_Loop, transform);
                _breathSource.volume = 0f; 
                _baseBreathPitch = sfx_Breath_Loop.pitch; 
            }
        }
    }

    void OnDisable()
    {
        if (_bagSource != null) _bagSource.volume = 0f;
        if (_breathSource != null) _breathSource.volume = 0f;
    }

    void Update()
    {
        UpdatePlayerReference();
        
        float speed = agent.velocity.magnitude;
        bool isMoving = speed > velocityThreshold;

        HandleFootsteps(speed, isMoving);
        HandleBagAudio(isMoving);
        HandleBreathing();
    }

    private void UpdatePlayerReference()
    {
        if (_playerTransform == null && config != null && config.playerAnchor != null)
        {
            _playerTransform = config.playerAnchor.Value;
        }
    }

    // --- 1. FOOTSTEPS ---
    private void HandleFootsteps(float speed, bool isMoving)
    {
        if (!isMoving) 
        {
            _distanceTraveled = 0f;
            return;
        }

        _distanceTraveled += speed * Time.deltaTime;

        if (_distanceTraveled >= strideLength)
        {
            PlayRaycastFootstep();
            _distanceTraveled = 0f;
        }
    }

private void PlayRaycastFootstep()
    {
        SoundDefinition soundToPlay = sfx_Footstep_Dirt; 

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
                    if (textureIndex == 3) soundToPlay = sfx_Footstep_Grass; 
                }
            }
        }

        if (SoundManager.Instance != null && soundToPlay != null)
        {
            // --- FIX: Apply 0.5f Volume Multiplier ---
            SoundManager.Instance.PlaySound(soundToPlay, feetPosition.position, 0.5f);
        }
    }

    private void HandleBagAudio(bool isMoving)
    {
        if (_bagSource == null) return;

        float targetVol = isMoving ? sfx_BagDrag_Loop.volume : 0f;
        _bagSource.volume = Mathf.MoveTowards(_bagSource.volume, targetVol, Time.deltaTime * 5f);

    }

    private void HandleBreathing()
    {
        if (_breathSource == null || _playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        float intensity = Mathf.InverseLerp(sfx_Breath_Loop.maxDistance, sfx_BagDrag_Loop.minDistance, dist);

        _breathSource.volume = intensity * sfx_Breath_Loop.volume;

        float currentMult = Mathf.Lerp(1.0f, maxBreathPitchMultiplier, intensity);
        _breathSource.pitch = _baseBreathPitch * currentMult;
    }

    private SoundDefinition GetSoundForSurfaceType(SurfaceType type)
    {
        switch (type)
        {
            case SurfaceType.Wood: return sfx_Footstep_Wood;
            case SurfaceType.Stone: return sfx_Footstep_Stone;
            case SurfaceType.Grass: return sfx_Footstep_Grass;
            default: return sfx_Footstep_Dirt;
        }
    }
}