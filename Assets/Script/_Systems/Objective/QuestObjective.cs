using UnityEngine;

public class QuestObjective : MonoBehaviour
{
    [SerializeField] private TransformAnchorSO objectiveAnchor; // Drag "anchor_CurrentObjective"

    void OnEnable()
    {
        // Register this object as the current objective
        if (objectiveAnchor != null) objectiveAnchor.Provide(this.transform);
    }

    void OnDisable()
    {
        // If this object turns off (e.g. puzzle solved), clear the objective
        if (objectiveAnchor != null && objectiveAnchor.Value == transform) 
            objectiveAnchor.Unset();
    }
}