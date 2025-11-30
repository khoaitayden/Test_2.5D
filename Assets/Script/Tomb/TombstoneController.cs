// TombstoneController.cs
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
        // Randomize max energy
        maxEnergy = Random.Range(energyRange.x, energyRange.y);
        currentEnergy = maxEnergy;
        
        // Randomize sprite
        if (spriteRenderer != null && tombstoneSprites != null && tombstoneSprites.Length > 0)
        {
            Sprite randomSprite = tombstoneSprites[Random.Range(0, tombstoneSprites.Length)];
            spriteRenderer.sprite = randomSprite;
        }
        
        // Initialize visuals
        if (wispSoul != null) wispSoul.Stop();
        UpdateIndicatorLight();
    }

    void FixedUpdate()
    {
        // Billboard to camera
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
        
        // Recharge when not lit and delay passed
        if (!isLit && Time.time - lastDrainTime > rechargeDelay)
        {
            currentEnergy = Mathf.MoveTowards(currentEnergy, maxEnergy, regainRate * Time.deltaTime);
            UpdateIndicatorLight();
        }
    }

    public void OnLit()
    {
        isLit = true;
        lastDrainTime = Time.time;
        PlayTransferParticles();
    }

    public void OnUnlit()
    {
        isLit = false;
        StopTransferParticles();
    }

    public void DrainEnergy(float deltaTime)
    {
        if (!isLit || currentEnergy <= 0f)
        {
            currentEnergy = 0f;
            StopTransferParticles();
            UpdateIndicatorLight();
            return;
        }
        
        // Check if player is at max energy - stop emitting particles
        var manager = FindFirstObjectByType<LightEnergyManager>();
        if (manager != null && manager.CurrentEnergy >= 0.95f)
        {
            StopTransferParticles();
            return;
        }
        
        // Keep particles playing if there's energy
        if (currentEnergy > 0f)
        {
            PlayTransferParticles();
        }
        else
        {
            StopTransferParticles();
        }
    }
    
    // NEW METHOD: Called by particle controller when energy is actually absorbed
    public void DrainEnergyByAmount(float amount)
    {
        if (currentEnergy <= 0f) return;
        
        currentEnergy -= amount;
        if (currentEnergy < 0f) currentEnergy = 0f;
        
        UpdateIndicatorLight();
        lastDrainTime = Time.time;
        
        // Stop particles if depleted
        if (currentEnergy <= 0f)
        {
            StopTransferParticles();
        }
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
    
    // PUBLIC GETTER for current energy state
    public float CurrentEnergy => currentEnergy;
    public float EnergyPercentage => maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
}