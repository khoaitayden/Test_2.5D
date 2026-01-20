using UnityEngine;

public class BeaconFrameDisplay : MonoBehaviour
{
    [Header("Motion Settings")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 30f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
}