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
    private bool _wasVisible, _isRoaring;
    private AudioSource _breathSource;

    void Start()
    {
        if (!brain) brain = GetComponent<MonsterBrain>();
        
        // Setup Breathing Loop
        if (SoundManager.Instance && sfx_Breath)
        {
            _breathSource = SoundManager.Instance.CreateLoop(sfx_Breath, head ? head : transform);
            _breathSource.volume = 0f;
        }
        ResetTimer();
    }

    void Update()
    {
        bool visible = brain.IsPlayerVisible;
        Vector3 pos = head ? head.position : transform.position;

        // 1. Handle Breathing (Volume Ducking)
        // If visible and NOT roaring, target is max volume. Otherwise (lost player or roaring), target is 0.
        float targetVol = (visible && !_isRoaring) ? sfx_Breath.volume : 0f;
        if (_breathSource) 
            _breathSource.volume = Mathf.MoveTowards(_breathSource.volume, targetVol, Time.deltaTime * breathFadeSpeed);

        // 2. Handle Roaring
        if (visible && !_isRoaring)
        {
            // Roar if: Just spotted player OR Timer is up
            if (!_wasVisible || Time.time >= _nextRoar) 
                StartCoroutine(RoarRoutine(pos));
        }
        else if (!visible && _wasVisible)
        {
            _nextRoar = Time.time + lostSightDelay; // Delay if we just lost them
        }

        _wasVisible = visible;
    }

    private IEnumerator RoarRoutine(Vector3 pos)
    {
        _isRoaring = true;
        float waitTime = 2.0f; // Fallback

        if (SoundManager.Instance && sfx_Roar)
        {
            var source = SoundManager.Instance.PlaySound(sfx_Roar, pos);
            if (source && source.clip)
            {
                // Calculate duration: (Length / Pitch) - Overlap
                waitTime = (source.clip.length / Mathf.Abs(source.pitch)) - breathOverlap;
            }
        }

        yield return new WaitForSeconds(waitTime);
        
        ResetTimer();
        _isRoaring = false;
    }

    private void ResetTimer() => _nextRoar = Time.time + Random.Range(minRoarTime, maxRoarTime);
}