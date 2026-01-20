using UnityEngine;

public class TombstonePowerParticleController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy; 
    [Header("Detection Settings")]
    [SerializeField] private LayerMask triggerLayers;
    [SerializeField] private float checkRadius = 0.5f;
    
    [Header("Energy Settings")]
    [SerializeField] private float energyPerParticle = 0.05f; 
    
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private Collider[] hitBuffer = new Collider[4];
    
    private TombstoneController tombstoneController;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps != null)
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
        
        tombstoneController = GetComponent<TombstoneController>();
        if (tombstoneController == null) tombstoneController = GetComponentInParent<TombstoneController>();
    }

    void LateUpdate()
    {
        if (ps == null) return;

        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;
            int hits = Physics.OverlapSphereNonAlloc(pos, checkRadius, hitBuffer, triggerLayers);
            
            if (hits > 0)
            {
                if (currentEnergy != null && maxEnergy != null)
                {
                    float amountToAdd = maxEnergy.Value * energyPerParticle;
                    currentEnergy.ApplyChange(amountToAdd, 0f, maxEnergy.Value);
                }

                if (tombstoneController != null)
                {
                    tombstoneController.DrainEnergyByAmount(energyPerParticle);
                }

                particles[i].remainingLifetime = 0f;
            }
        }

        ps.SetParticles(particles, count);
    }
}