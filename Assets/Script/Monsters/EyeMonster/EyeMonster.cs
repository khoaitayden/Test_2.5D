using UnityEngine;

public class EyeMonster : MonoBehaviour, ILitObject
{
    [SerializeField] private float timeToExpose = 3.0f;
    [SerializeField] private float timeToVanish = 2.0f;
    [SerializeField] private EyeMonsterManager manager;

    private Transform mainCameraTransform;
    private float currentHeat = 0f;
    private float exposeTimer = 0f;
    private bool isLitByFlashlight = false;

    void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        if (manager == null) manager = FindFirstObjectByType<EyeMonsterManager>();
        OnEnable();
    }

    void OnEnable()
    {
        currentHeat = 0f;
        exposeTimer = 0f;
        isLitByFlashlight = false;
        // Reset flag immediately
        if (manager != null) manager.SetExposureState(false);
    }

    void OnDisable()
    {
        // Safety check: ensure flag is off when object turns off
        if (manager != null) manager.SetExposureState(false);
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null) return;
        transform.LookAt(mainCameraTransform);

        if (isLitByFlashlight)
        {
            HandleBurning();
        }
        else
        {
            HandleStaring();
        }
    }

    private void HandleBurning()
    {
        // Immediately cut the alarm
        if (manager != null) manager.SetExposureState(false);
        
        exposeTimer = Mathf.Max(0, exposeTimer - Time.deltaTime * 2f);
        currentHeat += Time.deltaTime;
        
        if (currentHeat >= timeToVanish) Vanish();
    }

    private void HandleStaring()
    {
        if (currentHeat > 0) currentHeat = Mathf.Max(0, currentHeat - Time.deltaTime);

        if (CanSeePlayer())
        {
            exposeTimer += Time.deltaTime;

            // --- TRIGGER ---
            if (exposeTimer >= timeToExpose)
            {
                if (manager != null) manager.SetExposureState(true);
            }
        }
        else
        {
            exposeTimer = Mathf.Max(0, exposeTimer - Time.deltaTime);
            // Stop alarm if line of sight broken
            if (manager != null) manager.SetExposureState(false);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 dir = (mainCameraTransform.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, mainCameraTransform.position);
        return !Physics.Raycast(transform.position, dir, dist, LayerMask.GetMask("Default", "Structure", "Terrain"));
    }

    private void Vanish()
    {
        if (manager != null) manager.DespawnEye();
        else gameObject.SetActive(false);
    }

    public void OnLit(LightSourceType type) { if (type == LightSourceType.Flashlight) isLitByFlashlight = true; }
    public void OnUnlit(LightSourceType type) { if (type == LightSourceType.Flashlight) isLitByFlashlight = false; }
}