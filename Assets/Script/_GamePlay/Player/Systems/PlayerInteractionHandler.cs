using UnityEngine;

public class PlayerInteractionHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform mainCameraTransform;

    private IInteractable currentInteractable;

    void Start()
    {
        if (mainCameraTransform == null) mainCameraTransform = Camera.main.transform;

        // Subscribe to Input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteractTriggered += TryInteract;
        }
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteractTriggered -= TryInteract;
        }
    }

    void Update()
    {
        CheckForInteractable();
    }

    // Logic to update UI or Highlight objects
    private void CheckForInteractable()
    {
        Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            // Get the component from the hit object
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            // If component is on a parent (common for complex meshes)
            if (interactable == null) interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                // TODO: Send prompt to UI Manager -> interactable.GetInteractionPrompt();
                return;
            }
        }

        currentInteractable = null;
    }

    // Called when 'E' is pressed
    private void TryInteract()
    {
        // Check references and states
        if (currentInteractable == null) return;
        
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            // Don't interact if dead or locked in animation
            if (pc.IsDead || pc.IsInteractionLocked) return;
        }

        currentInteractable.Interact(this.gameObject);
    }
        
    // Debug Visual
    private void OnDrawGizmos()
    {
        if (mainCameraTransform)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mainCameraTransform.position, mainCameraTransform.forward * interactionRange);
        }
    }
}