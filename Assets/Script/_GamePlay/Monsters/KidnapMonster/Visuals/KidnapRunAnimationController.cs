using UnityEngine;
using UnityEngine.AI;

public class RunAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterBrain brain;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MonsterConfigBase config;

    [Header("Speed Settings")]
    [Tooltip("Smoothing speed for animation transitions")]
    [SerializeField] private float smoothTime = 0.1f;

    private int animSpeedHash;
    private float currentAnimSpeed;
    private float velocityRef;
    
    // Cached max speeds from config
    private float maxSpeed;

    void Awake()
    {
        animSpeedHash = Animator.StringToHash("MonsterSpeed");
    }

    void Start()
    {
        maxSpeed =config.chaseSpeed;
    }

    void Update()
    {
        if (agent == null) return;

        float currentRealSpeed = agent.velocity.magnitude;

        float targetNormalizedSpeed = currentRealSpeed / maxSpeed;

        targetNormalizedSpeed = Mathf.Clamp01(targetNormalizedSpeed);

        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetNormalizedSpeed, ref velocityRef, smoothTime);

        animator.SetFloat(animSpeedHash, currentAnimSpeed);
    }
}