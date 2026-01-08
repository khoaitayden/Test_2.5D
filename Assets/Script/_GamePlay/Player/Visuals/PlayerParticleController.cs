using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private ParticleSystem landEffect;

    [Header("Dependencies")]
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("State Data")]
    [SerializeField] private BoolVariableSO isSlowWalkingState; // Drag "var_IsSlowWalking"

    [Header("Landing Effect Settings")]
    [SerializeField] private float minFallIntensity = 5f;
    [SerializeField] private float maxFallIntensity = 20f;
    [SerializeField] private int minParticleCount = 8;
    [SerializeField] private int maxParticleCount = 40;

    private void Start()
    {
        // Subscribe to C# event for data-driven landing
        if (groundedChecker != null)
        {
            groundedChecker.OnLandWithFallIntensity += PlayLandEffect;
        }
    }

    private void OnDestroy()
    {
        if (groundedChecker != null)
        {
            groundedChecker.OnLandWithFallIntensity -= PlayLandEffect;
        }
    }

    private void Update()
    {
        UpdateTrail();
    }

    private void UpdateTrail()
    {
        if (trailEffect == null || groundedChecker == null || playerMovement == null) return;

        bool isGrounded = groundedChecker.IsGrounded;
        bool isMoving = playerMovement.IsMoving;
        bool isSlowWalking = isSlowWalkingState != null && isSlowWalkingState.Value;

        // Only show trail if grounded, moving, and NOT sneaking
        bool shouldPlay = isGrounded && isMoving && !isSlowWalking;

        if (shouldPlay && !trailEffect.isPlaying)
        {
            trailEffect.Play();
        }
        else if (!shouldPlay && trailEffect.isPlaying)
        {
            trailEffect.Stop();
        }
    }

    // --- PUBLIC API (Linked via GameEventListener in Inspector) ---

    public void PlayJumpEffect()
    {
        // Add jump specific particles here if you have them
        // For now, it's empty in your original code, but hooks are ready.
    }

    // Called by PlayerGroundedChecker event
    public void PlayLandEffect(float fallIntensity)
    {
        if (landEffect == null) return;

        float intensityT = Mathf.InverseLerp(minFallIntensity, maxFallIntensity, fallIntensity);
        int newParticleCount = Mathf.RoundToInt(Mathf.Lerp(minParticleCount, maxParticleCount, intensityT));

        var emission = landEffect.emission;
        var burst = emission.GetBurst(0); 
        burst.count = newParticleCount;
        emission.SetBurst(0, burst); 

        landEffect.Play();
    }
}