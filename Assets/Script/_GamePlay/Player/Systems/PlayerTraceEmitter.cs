using UnityEngine;

public class PlayerTraceEmitter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel;
    [Header("Intervals (Seconds)")]
    [Tooltip("Random time between traces while running (e.g. 5 to 10s)")]
    public Vector2 runInterval = new Vector2(5f, 10f);
    
    [Tooltip("Random time between traces while walking (e.g. 15 to 20s)")]
    public Vector2 walkInterval = new Vector2(15f, 20f);

    private float timer;
    private float currentThreshold;
    
    // Position tracking to ensure we actually moved
    private Vector3 lastPos;

    void Start()
    {
        PickNewThreshold(false);
        lastPos = transform.position;
    }

    void Update()
    {
        // 1. Check Input
        bool isSprinting = InputManager.Instance.IsSprinting;
        bool isSneaking = InputManager.Instance.IsSlowWalking;

        // 2. Sneaking leaves NO trace
        if (isSneaking)
        {
            // Reset timer slightly so we don't drop a trace immediately upon standing up
            timer = 0f;
            return;
        }

        // 3. Verify Movement (Don't drop traces if standing still)
        // Check if moved more than 0.1m this frame
        if (Vector3.Distance(transform.position, lastPos) < (Time.deltaTime * 0.1f))
        {
            return; 
        }
        lastPos = transform.position;

        // 4. Timer Logic
        timer += Time.deltaTime;

        if (timer >= currentThreshold)
        {
            // FIRE EVENT
            TraceType type = isSprinting ? TraceType.Footstep_Run : TraceType.Footstep_Walk;
            
            if(traceChannel != null) 
                traceChannel.RaiseEvent(transform.position, type);

            // Reset
            timer = 0f;
            PickNewThreshold(isSprinting);
        }
    }

    void PickNewThreshold(bool isRunning)
    {
        if (isRunning)
            currentThreshold = Random.Range(runInterval.x, runInterval.y);
        else
            currentThreshold = Random.Range(walkInterval.x, walkInterval.y);
    }
}