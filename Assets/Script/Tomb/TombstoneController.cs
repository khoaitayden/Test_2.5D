// TombstoneController.cs
using UnityEngine;

public class TombstoneController : MonoBehaviour, ILitObject
{
    [Header("Energy Settings")]
    public float maxEnergy = 1f;
    public float transferRate = 0.5f;      // Energy per second transferred to player
    public float rechargeDelay = 2f;
    public float regainRate = 0.2f;

    [Header("Visuals")]
    public Light energyIndicatorLight;
    public ParticleSystem wispSoul;

    private float currentEnergy = 1f;
    private float lastDrainTime = 0f;
    private bool isLit = false;

    void Start()
    {
        currentEnergy = maxEnergy;
        if (wispSoul != null) wispSoul.Stop();
        UpdateIndicatorLight();
    }

    void Update()
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
            currentEnergy = 0f; // Ensure it's exactly 0
            StopTransferParticles();
            UpdateIndicatorLight(); // Update light to fully off
            return;
        }

        float drainAmount = transferRate * deltaTime;
        currentEnergy -= drainAmount;

        if (currentEnergy < 0f) currentEnergy = 0f;

        var manager = FindObjectOfType<LightEnergyManager>();
        if (manager != null && drainAmount > 0f)
        {
            manager.RestoreEnergy(drainAmount);
        }

        UpdateIndicatorLight();

        if (currentEnergy <= 0f)
        {
            StopTransferParticles();
        }
        else
        {
            PlayTransferParticles();
        }

        lastDrainTime = Time.time;
    }

    void PlayTransferParticles()
    {
        if (wispSoul != null && !wispSoul.isPlaying)
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

        // Always show light proportional to energy — even at low values
        float intensity = Mathf.Lerp(0f, 0.1f, currentEnergy);
        float range = Mathf.Lerp(0f, 2f, currentEnergy);

        energyIndicatorLight.intensity = intensity;
        energyIndicatorLight.range = range;
        // ✅ NEVER disable the light — just let intensity/range go to 0
        energyIndicatorLight.enabled = true; // Always on (visually fades out)
    }
}