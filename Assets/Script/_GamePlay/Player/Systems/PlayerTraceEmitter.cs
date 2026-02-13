using UnityEngine;

public class PlayerTraceEmitter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Intervals (Seconds)")]
    public Vector2 runInterval = new Vector2(5f, 10f);
    public Vector2 walkInterval = new Vector2(15f, 20f);

    private float timer;
    private float currentThreshold;
    private Vector3 lastPos;

    void Awake()
    {
        // Try to find it if not assigned
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Start()
    {
        PickNewThreshold(false);
        lastPos = transform.position;
    }

    void Update()
    {
        bool isSprinting = playerMovement.IsSprinting;
        
        bool isSneaking = InputManager.Instance.IsSlowWalking;

        if (playerMovement.CurrentHorizontalSpeed <= playerMovement.sneakSpeed && !isSneaking) 
        {
             isSneaking = true; 
        }

        if (isSneaking)
        {
            timer = 0f;
            return;
        }

        if (Vector3.Distance(transform.position, lastPos) < (Time.deltaTime * 0.1f))
        {
            return; 
        }
        lastPos = transform.position;

        timer += Time.deltaTime;

        if (timer >= currentThreshold)
        {
            TraceType type = isSprinting ? TraceType.Footstep_Run : TraceType.Footstep_Walk;
            
            if(traceChannel != null) 
                traceChannel.RaiseEvent(transform.position, type);

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