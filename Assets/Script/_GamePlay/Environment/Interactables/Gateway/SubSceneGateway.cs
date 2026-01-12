using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SubSceneGateway : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string subSceneName; // Name in Build Settings
    [SerializeField] private AreaDefinitionSO associatedArea;
    [Tooltip("Where the player stands after returning to main map")]
    [SerializeField] private Transform returnPoint;

    [Header("Dependencies")]
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private TransformAnchorSO subSceneSpawnAnchor; // Drag "anchor_SubSceneSpawn"
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;

    [Header("Visuals")]
    [SerializeField] private GameObject doorVisuals; // Optional: To show open/closed state

    private bool isOpen = true;
    private bool isPlayerInside = false;

    void OnEnable()
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.OnAreaItemPickedUp += HandleItemPickedUp;
            objectiveEvents.OnAreaReset += HandleAreaReset;
        }
    }

    void OnDisable()
    {
        if (objectiveEvents != null)
        {
            objectiveEvents.OnAreaItemPickedUp -= HandleItemPickedUp;
            objectiveEvents.OnAreaReset -= HandleAreaReset;
        }
    }

    public bool Interact(GameObject interactor)
    {
        if (!isOpen || isPlayerInside) return false;

        StartCoroutine(EnterSubSceneRoutine());
        return true;
    }

    public string GetInteractionPrompt()
    {
        return isOpen ? $"Enter {associatedArea.areaName}" : "Sealed";
    }

    // --- LOGIC: ENTERING ---
    private IEnumerator EnterSubSceneRoutine()
    {
        isPlayerInside = true;
        
        // 1. Load Scene Additively
        if (!SceneManager.GetSceneByName(subSceneName).isLoaded)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(subSceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;
        }

        // 2. Wait a frame for the SubScene's AnchorProvider to register itself
        yield return null; 

        // 3. Teleport Player IN
        if (subSceneSpawnAnchor != null && subSceneSpawnAnchor.Value != null)
        {
            TeleportPlayer(subSceneSpawnAnchor.Value.position);
        }
        else
        {
            Debug.LogError($"SubScene {subSceneName} loaded, but no Spawn Anchor found!");
        }
    }

    // --- LOGIC: EXITING (Triggered by Pickup) ---
    private void HandleItemPickedUp(AreaDefinitionSO area)
    {
        // Only react if it's MY area's item
        if (area == associatedArea && isPlayerInside)
        {
            StartCoroutine(ExitSubSceneRoutine());
        }
    }

    private IEnumerator ExitSubSceneRoutine()
    {
        // 1. Teleport Player OUT (Back to Main Map)
        TeleportPlayer(returnPoint.position);

        // 2. Unload Scene
        if (SceneManager.GetSceneByName(subSceneName).isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(subSceneName);
            while (!asyncUnload.isDone) yield return null;
        }

        // 3. Lock Door
        isPlayerInside = false;
        isOpen = false;
        UpdateVisuals();
    }

    // --- LOGIC: RESET (Triggered by Death) ---
    private void HandleAreaReset(AreaDefinitionSO area)
    {
        if (area == associatedArea)
        {
            isOpen = true;
            isPlayerInside = false;
            
            // Ensure scene is unloaded just in case
            if (SceneManager.GetSceneByName(subSceneName).isLoaded)
            {
                SceneManager.UnloadSceneAsync(subSceneName);
            }
            
            UpdateVisuals();
        }
    }

    private void TeleportPlayer(Vector3 targetPos)
    {
        if (playerAnchor == null || playerAnchor.Value == null) return;

        CharacterController controller = playerAnchor.Value.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        
        playerAnchor.Value.position = targetPos;
        
        if (controller != null) controller.enabled = true;
    }

    private void UpdateVisuals()
    {
        if (doorVisuals != null) doorVisuals.SetActive(isOpen);
    }
}