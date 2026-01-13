using UnityEngine;

public class EyeMonster : MonoBehaviour, ILitObject
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite burnSprite;
    
    // ---------------- NEW FIELD ----------------
    [SerializeField] private GameObject xRayOutlineObject; 
    // -------------------------------------------

    [Header("Mechanics")]
    [SerializeField] private float timeToExpose = 3.0f;
    [SerializeField] private float exposureDuration = 5.0f;
    [Header("Burn")]
    [SerializeField] private float timeToVanish = 2.0f;
    [SerializeField] private EyeMonsterManager manager;

    private Transform mainCameraTransform;
    private float currentHeat = 0f;
    private float exposeTimer = 0f;
    private float activeExposureTimer = 0f;
    
    private bool isLitByFlashlight = false;
    private bool isAlarmActive = false;

    void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        if (manager == null) manager = FindFirstObjectByType<EyeMonsterManager>();
        
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        OnEnable();
    }

    void OnEnable()
    {
        currentHeat = 0f;
        exposeTimer = 0f;
        activeExposureTimer = 0f;
        isLitByFlashlight = false;
        isAlarmActive = false;
        
        if (manager != null) manager.SetExposureState(false);
        if (spriteRenderer != null) spriteRenderer.sprite = normalSprite;

        // ---------------- RESET OUTLINE ----------------
        // Ensure outline is hidden when the monster first spawns/respawns
        if (xRayOutlineObject != null) xRayOutlineObject.SetActive(false);
        // -----------------------------------------------
    }

    void OnDisable()
    {
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
        // Stop alarm immediately if burned
        if (isAlarmActive)
        {
            isAlarmActive = false;
            if (manager != null) manager.SetExposureState(false);
            
            // ---------------- HIDE OUTLINE ----------------
            // If we burn it, stop the xray effect
            if (xRayOutlineObject != null) xRayOutlineObject.SetActive(false);
            // ----------------------------------------------
        }

        if (spriteRenderer != null && spriteRenderer.sprite != burnSprite)
            spriteRenderer.sprite = burnSprite;

        exposeTimer = Mathf.Max(0, exposeTimer - Time.deltaTime * 2f);
        currentHeat += Time.deltaTime;
        
        if (currentHeat >= timeToVanish) Vanish();
    }

    private void HandleStaring()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != normalSprite)
            spriteRenderer.sprite = normalSprite;

        if (currentHeat > 0) currentHeat = Mathf.Max(0, currentHeat - Time.deltaTime);

        // --- ALARM LOGIC ---
        if (isAlarmActive)
        {
            // The eye has already exposed the player. It keeps the alarm on for X seconds.
            activeExposureTimer += Time.deltaTime;
            
            if (activeExposureTimer >= exposureDuration)
            {
                Vanish();
            }
            return; 
        }

        // --- NORMAL STARE LOGIC ---
        if (CanSeePlayer())
        {
            exposeTimer += Time.deltaTime;
            
            // Trigger Alarm?
            if (exposeTimer >= timeToExpose)
            {
                isAlarmActive = true;
                activeExposureTimer = 0f; 
                if (manager != null) manager.SetExposureState(true);

                // ---------------- SHOW OUTLINE ----------------
                // The player is now exposed, turn on the XRay object
                if (xRayOutlineObject != null) xRayOutlineObject.SetActive(true);
                // ----------------------------------------------
            }
        }
        else
        {
            exposeTimer = Mathf.Max(0, exposeTimer - Time.deltaTime);
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