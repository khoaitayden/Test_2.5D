using UnityEngine;

public class EyeMonster : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Find the camera once
        if (Camera.main != null) 
            mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        // Make the Eye face the camera
        transform.LookAt(mainCameraTransform);
        
        // Optional: If your sprite is flipped, uncomment the line below.
        // transform.Rotate(0, 180, 0); 
    }
}