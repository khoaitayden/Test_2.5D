using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform feetPos;

    [Header("Surface Sounds")]
    [SerializeField] private SoundDefinition stepGrass;
    [SerializeField] private SoundDefinition stepWood; // Tree branches
    [SerializeField] private SoundDefinition stepStone; // House/Concrete
    [SerializeField] private SoundDefinition jumpSound;
    [SerializeField] private SoundDefinition landSound;

    [Header("Settings")]
    [SerializeField] private float stepStrideWalk = 0.5f;
    [SerializeField] private float stepStrideSprint = 0.9f;

    private float distanceTraveled;

    void Update()
    {
        HandleFootsteps();
    }

    // Call this from PlayerController.HandleJumpTrigger
    public void PlayJump() => SoundManager.Instance.PlaySound(jumpSound, transform.position);

    // Call this from PlayerController Update (Landing logic)
    public void PlayLand(float intensity) => SoundManager.Instance.PlaySound(landSound, transform.position, 1f + (intensity/10f));

    private void HandleFootsteps()
    {
        // 1. Check if moving and grounded
        if (player.IsDead || player.IsClimbing || player.IsInteractionLocked) return;
        
        // Use CharacterController velocity (assuming PlayerController has GetComponent<CharacterController> public or check velocity)
        // Since PlayerController calculation is private, we estimate via input or expose Velocity. 
        // Let's assume we can get magnitude:
        float speed = player.WorldSpaceMoveDirection.magnitude > 0.1f ? (player.IsSprinting ? 6f : 3f) : 0f;

        if (speed < 0.1f) { distanceTraveled = 0; return; }

        float stride = player.IsSprinting ? stepStrideSprint : stepStrideWalk;
        distanceTraveled += speed * Time.deltaTime;

        if (distanceTraveled >= stride)
        {
            PlayStep();
            distanceTraveled = 0;
        }
    }

    private void PlayStep()
    {
        // 2. Raycast to find surface
        RaycastHit hit;
        SurfaceType surface = SurfaceType.Grass; // Default

        if (Physics.Raycast(feetPos.position + Vector3.up, Vector3.down, out hit, 2f))
        {
            // A. Check for Component (Tree Branch, House Floor)
            SurfaceIdentifier id = hit.collider.GetComponent<SurfaceIdentifier>();
            if (id != null)
            {
                surface = id.type;
            }
            // B. Check Tag (For Terrain or objects without script)
            else if (hit.collider.CompareTag("Concrete")) surface = SurfaceType.Stone;
            else if (hit.collider.CompareTag("Wood")) surface = SurfaceType.Wood;
        }

        // 3. Choose Definition
        SoundDefinition defToPlay = stepGrass;
        switch (surface)
        {
            case SurfaceType.Wood: defToPlay = stepWood; break;
            case SurfaceType.Stone: defToPlay = stepStone; break;
        }

        // 4. Modify Audio based on Speed (The Request)
        float pitchMult = 1f;
        float volMult = 1f;

        if (player.IsSprinting)
        {
            pitchMult = 1.1f; // Higher pitch when running
            volMult = 1.2f;   // Louder
        }
        else if (player.IsSlowWalking)
        {
            pitchMult = 0.8f; // Lower pitch sneaking
            volMult = 0.5f;   // Quieter
        }

        SoundManager.Instance.PlaySound(defToPlay, feetPos.position, volMult, pitchMult);
    }
}