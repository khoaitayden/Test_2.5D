using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    
    public float volumeVariance = 0.1f;
    public float pitchVariance = 0.1f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1f;
    public float maxDistance = 50f;

    // --- ADD THIS ---
    [Header("Effects")]
    [Tooltip("Extra seconds to keep the object active after the sound finishes. INCREASE THIS for Echo/Reverb.")]
    public float tailSeconds = 0f; 
}