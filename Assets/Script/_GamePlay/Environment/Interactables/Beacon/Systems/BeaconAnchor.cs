using UnityEngine;

public class BeaconAnchor : MonoBehaviour
{
    [SerializeField] private TransformAnchorSO beaconAnchor; // Create "anchor_Beacon"

    void OnEnable()
    {
        if (beaconAnchor != null) beaconAnchor.Provide(transform);
    }

    void OnDisable()
    {
        if (beaconAnchor != null) beaconAnchor.Unset();
    }
}