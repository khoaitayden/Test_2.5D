using UnityEngine;

public class TombstoneController : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("No main camera found! Tombstone rotation may not work.");
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        Vector3 direction = mainCamera.transform.position - transform.position;
        direction.y = 0; // Ignore vertical difference

        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        }
    }
}