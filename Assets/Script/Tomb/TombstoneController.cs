// TombstoneController.cs
using UnityEngine;

public class TombstoneController : MonoBehaviour, ILitObject
{
    [SerializeField] private ParticleSystem wispSoul;

    void Start()
    {
        if (wispSoul != null)
            wispSoul.Stop();

        // Optional: register itself automatically (not required if using physics query)
    }

    public void OnLit()
    {
        Debug.Log($"{name} is lit!");
        if (wispSoul != null && !wispSoul.isPlaying)
            wispSoul.Play();
    }

    public void OnUnlit()
    {
        Debug.Log($"{name} is unlit!");
        if (wispSoul != null && wispSoul.isPlaying)
            wispSoul.Stop();
    }

    // Keep your billboard rotation logic
    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}