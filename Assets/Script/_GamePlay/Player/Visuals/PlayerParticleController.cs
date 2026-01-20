using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private ParticleSystem landEffect;

    [Header("Dependencies")]
    [SerializeField] private PlayerGroundedChecker groundedChecker;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Landing Effect Settings")]
    [SerializeField] private float minFallIntensity = 5f;
    [SerializeField] private float maxFallIntensity = 20f;
    [SerializeField] private int minParticleCount = 8;
    [SerializeField] private int maxParticleCount = 40;

    private void Start()
    {
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
        bool isSlowWalking = InputManager.Instance.IsSlowWalking;

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


    public void PlayJumpEffect()
    {

    }

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