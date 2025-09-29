using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [SerializeField] private GameObject dirtTrail;
    [SerializeField] private ParticleSystem landEffect;

    [Header("Landing Effect Settings")]
    [SerializeField] private float minFallIntensity = 5f;
    [SerializeField] private float maxFallIntensity = 20f;
    [SerializeField] private int minParticleCount = 8;
    [SerializeField] private int maxParticleCount = 40;

    public void PlayLandEffect(float fallIntensity)
    {
        if (landEffect == null) return;
        // 1. Calculate a normalized value (0 to 1) based on the fall intensity.
        float intensityT = Mathf.InverseLerp(minFallIntensity, maxFallIntensity, fallIntensity);
        // 2. Use that normalized value to find the desired particle count in our defined range.
        float particleCountFloat = Mathf.Lerp(minParticleCount, maxParticleCount, intensityT);
        // 3. Convert to an integer.
        int newParticleCount = Mathf.RoundToInt(particleCountFloat);

        Debug.Log($"Fall Intensity: {fallIntensity}, Particle Count: {newParticleCount}");
        var emission = landEffect.emission;
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);
        if (bursts.Length > 0)
        {
            bursts[0].count = newParticleCount;
            emission.SetBursts(bursts);
        }
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
