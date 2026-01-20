using UnityEngine;

public class WispAnimationController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FloatVariableSO currentEnergy;
    [SerializeField] private FloatVariableSO maxEnergy;
    
    [SerializeField] private IntVariableSO monstersWatchingCount;

    [Header("References")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private Animator fireAnimator; 
    [SerializeField] private SpriteRenderer faceRenderer;

    [Header("Visual Settings")]
    [SerializeField] private Sprite faceNormal;
    [SerializeField] private Sprite faceObjectiveFound;
    [SerializeField] private Light innerGlowLight;
    [SerializeField] private bool lockYAxis = true;

    private float _initInnerIntensity;
    
    private int _animLookHash; 

    void Start()
    {
        if (mainCameraTransform == null && Camera.main != null) 
            mainCameraTransform = Camera.main.transform;

        if (innerGlowLight) 
            _initInnerIntensity = innerGlowLight.intensity;

        _animLookHash = Animator.StringToHash("IsBeingLook");
    }

    void Update()
    {
        HandleInnerGlow();
        HandleFireState();
    }

    void LateUpdate()
    {
        HandleBillboarding();
    }

    public void SetFaceExpression(bool isLookingAtObjective)
    {
        if (faceRenderer == null) return;
        Sprite targetSprite = isLookingAtObjective ? faceObjectiveFound : faceNormal;
        if (faceRenderer.sprite != targetSprite) faceRenderer.sprite = targetSprite;
    }

    private void HandleFireState()
    {
        if (fireAnimator == null || monstersWatchingCount == null) return;

        bool isBeingLookedAt = monstersWatchingCount.Value > 0;
        
        fireAnimator.SetBool(_animLookHash, isBeingLookedAt);
    }

    private void HandleInnerGlow()
    {
        if (innerGlowLight)
        {
            if (currentEnergy != null && currentEnergy.Value > 0)
            {
                innerGlowLight.enabled = true;
                float energyFactor = (maxEnergy != null && maxEnergy.Value > 0) ? currentEnergy.Value / maxEnergy.Value : 1f;
                float pulse = Mathf.Lerp(0.8f, 1.2f, Mathf.PerlinNoise(Time.time * 3f, 0f));
                innerGlowLight.intensity = Mathf.Lerp(0f, _initInnerIntensity, energyFactor) * pulse;
            }
            else
            {
                innerGlowLight.enabled = false;
            }
        }
    }

    private void HandleBillboarding()
    {
        if (mainCameraTransform != null)
        {
            Vector3 lookDir = mainCameraTransform.forward;
            if (lockYAxis) lookDir.y = 0;
            if (lookDir.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}