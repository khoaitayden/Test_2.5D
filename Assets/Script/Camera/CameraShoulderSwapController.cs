using UnityEngine;
using Unity.Cinemachine;

public class CameraShoulderSwapController : MonoBehaviour
{
    [Header("Cinemachine Target")]
    [Tooltip("The Cinemachine Virtual Camera to control.")]
    public CinemachineCamera virtualCamera;

    [Header("Shoulder Offset Settings")]
    public float rightShoulderX;
    public float leftShoulderX;
    
    [Header("Snappiness")]
    public float swapSpeed;

    private CinemachineRotationComposer composer;
    private float targetScreenX;
    private float screenXVelocity; 

    void Start()
    {
        if (virtualCamera != null)
        {
            var aimComponent = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim);
            if (aimComponent is CinemachineRotationComposer)
            {
                composer = aimComponent as CinemachineRotationComposer;
            }
        }
        targetScreenX = rightShoulderX;
    }

    void FixedUpdate()
    {
        if (composer == null) return;


        float horizontalInput = Input.GetAxis("Horizontal");
        targetScreenX = (horizontalInput < -0.1f) ? leftShoulderX : rightShoulderX;

        // 2. Because 'Composition' is a struct, we must copy it to modify it.
        var composition = composer.Composition;
        
        // 3. Get the current X value from our copy.
        float currentX = composition.ScreenPosition.x;
        
        // 4. Calculate the new, smoothed X value.
        float newX = Mathf.SmoothDamp(
            currentX, 
            targetScreenX, 
            ref screenXVelocity, 
            swapSpeed
        );

        // 5. Apply the new X value to our copy.
        composition.ScreenPosition.x = newX;

        // 6. Assign the modified copy back to the composer. This is the crucial final step.
        composer.Composition = composition;
    }
}