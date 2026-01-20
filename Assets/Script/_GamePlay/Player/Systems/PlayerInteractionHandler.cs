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
    private void CheckForInteractable()
    {
        Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null) interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                return;
            }
        }

        currentInteractable = null;
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;
        
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            if (pc.IsDead || pc.IsInteractionLocked) return;
        }

        currentInteractable.Interact(this.gameObject);
    }
        
    private void OnDrawGizmos()
    {
        if (mainCameraTransform)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mainCameraTransform.position, mainCameraTransform.forward * interactionRange);
        }
    }
}