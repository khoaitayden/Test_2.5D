using UnityEngine;
using UnityEngine.Events;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Global Events")]
    public UnityEvent<GameObject, string> OnObjectInteracted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ReportInteraction(GameObject subject, string actionCode)
    {
        // Example Usage:
        // if (actionCode == "DoorOpened" && subject.name == "BossDoor") 
        //     PlayBossMusic();
        
        Debug.Log($"Interaction Event: {subject.name} -> {actionCode}");
        OnObjectInteracted?.Invoke(subject, actionCode);
    }
}