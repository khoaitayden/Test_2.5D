using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSortingOrder : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    public int sortingOrderBase = 1;  // Big enough base
    public float offset = 0f;            // Manual tweak per object if needed
    public float precision = 1000f;       // Controls granularity

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Convert world position into camera space
        Vector3 cameraSpacePos = mainCamera.WorldToScreenPoint(transform.position);

        // Use the depth value from camera space (z) instead of world z
        // Higher depth → further away → lower order
        float distance = cameraSpacePos.z * precision;

        spriteRenderer.sortingOrder = sortingOrderBase - Mathf.RoundToInt(distance + offset);
    }
}
