using UnityEngine;
using System.Collections;

public class TombstoneController : MonoBehaviour, ILitObject
{
    [Header("Dependencies")]
    [SerializeField] private TraceEventChannelSO traceChannel; 
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
    
    private bool isLitByWisp = false;

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
            if (dir.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }
        
        // Recharge if NOT lit by wisp
        if (!isLitByWisp && Time.time - lastDrainTime > rechargeDelay)
        {
            currentEnergy = Mathf.MoveTowards(currentEnergy, maxEnergy, regainRate * Time.deltaTime);
            UpdateIndicatorLight();
        }
    }

    // --- INTERFACE IMPLEMENTATION ---

    public void OnLit(LightSourceType sourceType)
    {
        if (sourceType == LightSourceType.Wisp)
        {
            isLitByWisp = true;
            lastDrainTime = Time.time;
            PlayTransferParticles();
        }
    }

    public void OnUnlit(LightSourceType sourceType)
    {
        if (sourceType == LightSourceType.Wisp)
        {
            isLitByWisp = false;
            StopTransferParticles();
        }
    }

    public void DrainEnergy(float deltaTime)
    {
        if (!isLitByWisp || currentEnergy <= 0f)
        {
            StopTransferParticles();
            UpdateIndicatorLight();
            return;
        }
        
        if (currentEnergy > 0f) PlayTransferParticles();
        else StopTransferParticles();
    }
    
    public void DrainEnergyByAmount(float amount)
    {
        if (currentEnergy <= 0f) return;
        
        traceChannel.RaiseEvent(transform.position, TraceType.Soul_Collection);
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
}