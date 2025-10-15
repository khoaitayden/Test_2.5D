using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    // --- CHANGE THIS ---
    // We want a direct reference to the ParticleSystem component itself.
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private ParticleSystem landEffect;
    [SerializeField] private ParticleSystem jumpEffect;

    [Header("Landing Effect Settings")]
    [SerializeField] private float minFallIntensity = 5f;
    [SerializeField] private float maxFallIntensity = 20f;
    [SerializeField] private int minParticleCount = 8;
    [SerializeField] private int maxParticleCount = 40;

    public void PlayLandEffect(float fallIntensity)
    {
        if (landEffect == null) return;

        float intensityT = Mathf.InverseLerp(minFallIntensity, maxFallIntensity, fallIntensity);
        int newParticleCount = Mathf.RoundToInt(Mathf.Lerp(minParticleCount, maxParticleCount, intensityT));

        var emission = landEffect.emission;
        var burst = emission.GetBurst(0); // Get the first burst
        burst.count = newParticleCount;
        emission.SetBurst(0, burst); // Set the modified burst back

        landEffect.Play();
    }
    public void PlayJumpEffect()
    {
        if (jumpEffect == null) return;

        jumpEffect.Play();
        Debug.Log("Played jump effect");
    }

    // --- THIS IS THE NEW, ROBUST TOGGLE LOGIC ---
    public void ToggleTrail(bool isGrounded, bool isSlowWalking)
    {
        bool shouldPlay = isGrounded && !isSlowWalking;
        
        if (shouldPlay)
        {
            trailEffect.Play();
        }
        else
        {
            trailEffect.Stop();
        }
    }
}