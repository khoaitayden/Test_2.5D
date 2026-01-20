using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SubSceneGateway : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string subSceneName; 
    [SerializeField] private AreaDefinitionSO associatedArea;
    [SerializeField] private Transform returnPoint;

    [Header("Dependencies")]
    [SerializeField] private TransformAnchorSO playerAnchor;
    [SerializeField] private TransformAnchorSO subSceneSpawnAnchor; 
    [SerializeField] private ObjectiveEventChannelSO objectiveEvents;
    
    [SerializeField] private TransformSetSO activeObjectivesSet; 

    [Header("Visuals")]
    [SerializeField] private GameObject doorVisuals; 

    private bool isOpen = true;
    private bool isPlayerInside = false;

    void Start()
    {

        if (isOpen && activeObjectivesSet != null)
        {
            activeObjectivesSet.Add(this.transform);
        }
    }

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
        
        // Safety: Always remove self when destroyed/disabled
        if (activeObjectivesSet != null) activeObjectivesSet.Remove(this.transform);
    }

    // --- INTERACTION ---
    public bool Interact(GameObject interactor)
    {
        if (!isOpen || isPlayerInside) return false;

        // 1. Remove Door from Wisp Targets (We are going inside now)
        if (activeObjectivesSet != null) activeObjectivesSet.Remove(this.transform);

        StartCoroutine(EnterSubSceneRoutine());
        return true;
    }

    public string GetInteractionPrompt()
    {
        return isOpen ? $"Enter {associatedArea.areaName}" : "Sealed";
    }

    // --- ROUTINES ---
    private IEnumerator EnterSubSceneRoutine()
    {
        isPlayerInside = true;
        
        if (!SceneManager.GetSceneByName(subSceneName).isLoaded)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(subSceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;
        }

        yield return null; 

        if (subSceneSpawnAnchor != null && subSceneSpawnAnchor.Value != null)
        {
            TeleportPlayer(subSceneSpawnAnchor.Value.position);
        }
    }

    private void HandleItemPickedUp(AreaDefinitionSO area)
    {
        if (area == associatedArea && isPlayerInside)
        {
            StartCoroutine(ExitSubSceneRoutine());
        }
    }

    private IEnumerator ExitSubSceneRoutine()
    {
        TeleportPlayer(returnPoint.position);

        if (SceneManager.GetSceneByName(subSceneName).isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(subSceneName);
            while (!asyncUnload.isDone) yield return null;
        }

        isPlayerInside = false;
        isOpen = false;
        UpdateVisuals();
        
    }

    private void HandleAreaReset(AreaDefinitionSO area)
    {
        if (area == associatedArea)
        {
            isOpen = true;
            isPlayerInside = false;
            
            if (SceneManager.GetSceneByName(subSceneName).isLoaded)
                SceneManager.UnloadSceneAsync(subSceneName);
            
            UpdateVisuals();

            if (activeObjectivesSet != null) activeObjectivesSet.Add(this.transform);
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