using UnityEngine;
using System.Collections;

public class TombstoneController : MonoBehaviour, ILitObject
{
    [Header("Energy Randomization")]
    public Vector2 energyRange = new Vector2(0.5f, 1.0f);
    
    [Header("Visual Variants")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] tombstoneSprites;
    
    [Header("Energy Settings")]
    [HideInInspector] public float maxEnergy;
    public float rechargeDelay = 2f;
    public float regainRate = 0.2f;
    
    [Header("Visuals")]
    public Light energyIndicatorLight;
    public ParticleSystem wispSoul;
    
    private float currentEnergy;
    private float lastDrainTime = 0f;
    private bool isLit = false;

    void Start()
    {
        maxEnergy = Random.Range(energyRange.x, energyRange.y);
        currentEnergy = maxEnergy;
        
        if (spriteRenderer != null && tombstoneSprites != null && tombstoneSprites.Length > 0)
        {
            spriteRenderer.sprite = tombstoneSprites[Random.Range(0, tombstoneSprites.Length)];
        }
        
        if (wispSoul != null) wispSoul.Stop();
        UpdateIndicatorLight();
    }

    void FixedUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = cam.transform.position - transform.position;
            dir.y = 0;
            if (dir.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        
        if (!isLit && Time.time - lastDrainTime > rechargeDelay)
        {
            currentEnergy = Mathf.MoveTowards(currentEnergy, maxEnergy, regainRate * Time.deltaTime);
            UpdateIndicatorLight();
        }
    }

    public void OnLit()
    {
        isLit = true;
        // --- MODIFIED: We DO NOT play particles here anymore. ---
        // PlayTransferParticles(); 
    }

    public void OnUnlit()
    {
        isLit = false;
        StopTransferParticles();
    }

    // This method is now ONLY called when the player actively holds 'E'
    public void DrainEnergy(float deltaTime)
    {
        if (!isLit || currentEnergy <= 0f)
        {
            currentEnergy = 0f;
            StopTransferParticles();
            UpdateIndicatorLight();
            return;
        }
        
        var manager = LightEnergyManager.Instance;
        if (manager != null && manager.CurrentEnergy >= 0.99f)
        {
            StopTransferParticles(); // Stop emitting if player's energy is full
            return;
        }
        
        // --- NEW: This is now the only place particles are started ---
        PlayTransferParticles();
    }
    
    public void DrainEnergyByAmount(float amount)
    {
        if (currentEnergy <= 0f) return;
        TraceEventBus.Emit(transform.position, TraceType.Soul_Collection);
        currentEnergy -= amount;
        if (currentEnergy < 0f) currentEnergy = 0f;
        
        UpdateIndicatorLight();
        lastDrainTime = Time.time;
        
        if (currentEnergy <= 0f) StopTransferParticles();
    }

    void PlayTransferParticles()
    {
        if (wispSoul != null && !wispSoul.isPlaying && currentEnergy > 0f)
            wispSoul.Play();
    }

    void StopTransferParticles()
    {
        if (wispSoul != null && wispSoul.isPlaying)
            wispSoul.Stop();
    }

    void UpdateIndicatorLight()
    {
        if (energyIndicatorLight == null) return;
        
        float intensity = Mathf.Lerp(0f, 0.08f, currentEnergy / maxEnergy);
        float range = Mathf.Lerp(0f, 2f, currentEnergy / maxEnergy);
        
        energyIndicatorLight.intensity = intensity;
        energyIndicatorLight.range = range;
        energyIndicatorLight.enabled = true;
    }
    
    public float CurrentEnergy => currentEnergy;
    public float EnergyPercentage => maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
}