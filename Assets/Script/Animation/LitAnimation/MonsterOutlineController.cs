using UnityEngine;

public class MonsterOutlineController : MonoBehaviour, ILitObject
{
    [Header("Visual Settings")]
    [Tooltip("Assign all SkinnedMeshRenderers (Head, Body) here.")]
    [SerializeField] private SkinnedMeshRenderer[] monsterMeshes;

    [Tooltip("The index of the Outline Material in the Renderer list (Element 1 in your screenshot).")]
    [SerializeField] private int materialIndex = 1;

    [Header("Outline Configuration")]
    [SerializeField] private float activeThickness = 0.02f; // Adjust this to match your desired thickness
    [SerializeField] private string propertyName = "_OutlineThickness"; // Must match Shader Graph Reference

    private MaterialPropertyBlock propBlock;
    private int thicknessID;
    private bool isLit = false;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        thicknessID = Shader.PropertyToID(propertyName);
    }

    void Start()
    {
        // Ensure outline is hidden at start
        SetOutlineThickness(0f);
    }

    // --- ILitObject Implementation ---

    public void OnLit()
    {
        if (isLit) return;
        isLit = true;
        Debug.Log("Monster lit");
        SetOutlineThickness(activeThickness);
    }

    public void OnUnlit()
    {
        if (!isLit) return;
        isLit = false;
        Debug.Log("Monster unlit");
        SetOutlineThickness(0f);
    }

    // --- Helper ---

    private void SetOutlineThickness(float thickness)
    {
        foreach (var mesh in monsterMeshes)
        {
            if (mesh == null) continue;

            // 1. Get the current block for the specific material index
            mesh.GetPropertyBlock(propBlock, materialIndex);

            // 2. Change the value
            propBlock.SetFloat(thicknessID, thickness);

            // 3. Apply it back
            mesh.SetPropertyBlock(propBlock, materialIndex);
        }
    }
}