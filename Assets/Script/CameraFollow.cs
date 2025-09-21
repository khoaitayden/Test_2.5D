using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // player transform
    public Vector3 offset = new Vector3(0, 10, -10); // adjust for your 2.5D look
    public float smoothSpeed = 5f; // smoothing

    void LateUpdate()
    {
        if (target == null) return;

        // desired position
        Vector3 desiredPosition = target.position + offset;

        // smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // optional: keep camera looking at player
        // transform.LookAt(target);
    }
}
