using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [SerializeField] private GameObject dirtTrail;
    [SerializeField] private ParticleSystem landEffect;

    [Header("Landing Effect Settings")]
    [Tooltip("The downward velocity at which the minimum effect will be used.")]
    [SerializeField] private float minFallIntensity = 5f;
    [Tooltip("The downward velocity at which the maximum effect will be used.")]
    [SerializeField] private float maxFallIntensity = 20f;
    
    [Header("Count")]
    [SerializeField] private int minParticleCount = 8;
    [SerializeField] private int maxParticleCount = 40;

    [Header("Force (Start Speed)")]
    [SerializeField] private float minStartSpeed = 2f;
    [SerializeField] private float maxStartSpeed = 10f;

    [Header("Bounce")]
    [SerializeField] private float minBounce = 0.2f;
    [SerializeField] private float maxBounce = 0.5f;


    public void PlayLandEffect(float fallIntensity)
    {
        if (landEffect == null) return;

        // --- 1. Calculate the normalized intensity (0 to 1) ---
        // This single value will drive all our other calculations.
        float intensityT = Mathf.InverseLerp(minFallIntensity, maxFallIntensity, fallIntensity);

        // --- 2. Calculate new values using the intensity ---
        int newParticleCount = Mathf.RoundToInt(Mathf.Lerp(minParticleCount, maxParticleCount, intensityT));
        float newStartSpeed = Mathf.Lerp(minStartSpeed, maxStartSpeed, intensityT);
        float newBounce = Mathf.Lerp(minBounce, maxBounce, intensityT);

        // --- 3. Apply the new values to the Particle System modules ---

        // Get the modules. Modifying these structs applies the changes.
        var main = landEffect.main;
        var emission = landEffect.emission;
        var collision = landEffect.collision;

        // Apply the new Start Speed (Force)
        main.startSpeed = newStartSpeed;

        // Apply the new Bounce value
        collision.bounce = newBounce;
        
        // Apply the new Burst Count
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);
        if (bursts.Length > 0)
        {
            bursts[0].count = newParticleCount;
            emission.SetBursts(bursts);
        }

        // --- 4. Play the effect ---
        landEffect.Play();
    }

    public void ToggleDirtTrail(bool isOn)
    {
        if (dirtTrail != null)
        {
            dirtTrail.SetActive(isOn);
        }
    }
}