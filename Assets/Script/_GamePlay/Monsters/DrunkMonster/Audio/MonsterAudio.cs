using UnityEngine;
using System.Collections;

public class MonsterAudio : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private MonsterBrain brain;
    [SerializeField] private Transform head; 

    [Header("Settings")]
    [SerializeField] private SoundDefinition sfx_Roar;
    [SerializeField] private SoundDefinition sfx_Breath;
    [SerializeField] private float minRoarTime = 3f, maxRoarTime = 6f;
    [SerializeField] private float lostSightDelay = 3f;
    [SerializeField] private float breathFadeSpeed = 2f;
    [SerializeField] private float breathOverlap = 0.25f;

    private float _nextRoar;
    private bool _wasVisible;
    private bool _isRoaring; // This was getting stuck
    private AudioSource _breathSource;

    void Awake()
    {
        if (!brain) brain = GetComponent<MonsterBrain>();
    }

    void Start()
    {
        // Create the AudioSource once per lifetime of the object
        if (SoundManager.Instance && sfx_Breath)
        {
            _breathSource = SoundManager.Instance.CreateLoop(sfx_Breath, head ? head : transform);
            _breathSource.volume = 0f;
        }
    }

    // --- FIX: FORCE RESET ON RESPAWN ---
    void OnEnable()
    {
        _isRoaring = false;
        _wasVisible = false;
        ResetTimer();

        // Resume breathing if it exists
        if (_breathSource != null)
        {
            _breathSource.volume = 0f; // Start silent and fade in
            if (!_breathSource.isPlaying) _breathSource.Play();
        }
    }

    void OnDisable()
    {
        // 1. Kill the Roar Coroutine immediately
        StopAllCoroutines();
        
        // 2. Reset Flags
        _isRoaring = false;
        _wasVisible = false;

        // 3. Silence Breathing
        if (_breathSource != null)
        {
            _breathSource.Stop();
        }
    }
    // -----------------------------------

    void Update()
    {
        bool visible = brain.IsPlayerVisible;
        Vector3 pos = head ? head.position : transform.position;

        // 1. Handle Breathing (Volume Ducking)
        if (_breathSource) 
        {
            // Target is 0 if roaring OR if disabled/lost player (optional preference)
            float targetVol = (visible && !_isRoaring) ? sfx_Breath.volume : 0f;
            _breathSource.volume = Mathf.MoveTowards(_breathSource.volume, targetVol, Time.deltaTime * breathFadeSpeed);
        }

        // 2. Handle Roaring
        if (visible && !_isRoaring)
        {
            // Roar if: Just spotted player OR Timer is up
            if (!_wasVisible || Time.time >= _nextRoar) 
                StartCoroutine(RoarRoutine(pos));
        }
        else if (!visible && _wasVisible)
        {
            _nextRoar = Time.time + lostSightDelay; 
        }

        _wasVisible = visible;
    }

    private IEnumerator RoarRoutine(Vector3 pos)
    {
        _isRoaring = true;
        float waitTime = 2.0f; 

        if (SoundManager.Instance && sfx_Roar)
        {
            var source = SoundManager.Instance.PlaySound(sfx_Roar, pos);
            if (source && source.clip)
            {
                waitTime = (source.clip.length / Mathf.Abs(source.pitch)) - breathOverlap;
            }
        }

        yield return new WaitForSeconds(waitTime);
        
        ResetTimer();
        _isRoaring = false;
    }

    private void ResetTimer()
    {
        _nextRoar = Time.time + Random.Range(minRoarTime, maxRoarTime);
    }
}