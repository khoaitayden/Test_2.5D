using UnityEngine;

public class TransformAnchorProvider : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Which Anchor does this object represent?")]
    [SerializeField] private TransformAnchorSO anchorToProvide;

    private void OnEnable()
    {
        if (anchorToProvide != null) 
            anchorToProvide.Provide(this.transform);
    }

    private void OnDisable()
    {
        if (anchorToProvide != null) 
            anchorToProvide.Unset();
    }
}