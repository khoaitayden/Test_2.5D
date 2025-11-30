using UnityEngine;

public class TombstonePowerParticleController : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask triggerLayers;
    [SerializeField] private float checkRadius;
    
    [Header("Energy Settings")]
    [SerializeField] private float energyPerParticle; 
    
    // [Header("Optional: Visual Feedback")]
    // [SerializeField] private bool spawnAbsorbEffect = false;
    // [SerializeField] private GameObject absorbEffectPrefab;
    
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private Collider[] hitBuffer = new Collider[4];
    private LightEnergyManager energyManager;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        
        // Find the energy manager
        energyManager = FindFirstObjectByType<LightEnergyManager>();
        if (energyManager == null)
        {
            Debug.LogWarning("LightEnergyManager not found! Particles won't restore energy.");
        }
    }

    void LateUpdate()
    {
        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;
            int hits = Physics.OverlapSphereNonAlloc(pos, checkRadius, hitBuffer, triggerLayers);
            
            if (hits > 0)
            {
                // Restore energy when particle is absorbed
                if (energyManager != null && energyPerParticle > 0f)
                {
                    energyManager.RestoreEnergy(energyPerParticle);
                }
                
                // Optional: spawn absorption effect
                // if (spawnAbsorbEffect && absorbEffectPrefab != null)
                // {
                //     Instantiate(absorbEffectPrefab, pos, Quaternion.identity);
                // }
                
                // Optional: notify the absorber (Wisp)
                OnParticleAbsorbed(hitBuffer[0].gameObject, pos);
                
                // Kill particle
                particles[i].remainingLifetime = 0f;
            }
        }

        ps.SetParticles(particles, count);
    }
    
    void OnParticleAbsorbed(GameObject absorber, Vector3 position)
    {
        Debug.Log($"Particle absorbed by {absorber.name} - Energy restored: {energyManager.CurrentEnergy}");
    }
}