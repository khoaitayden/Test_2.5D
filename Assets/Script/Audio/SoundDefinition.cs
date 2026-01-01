using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup; // Drag your 'Environment', 'Player', or 'Monster' group here

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    
    [Header("Randomness")]
    public float volumeVariance = 0.1f;
    public float pitchVariance = 0.1f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 1 = 3D sound
    public float minDistance = 1f;
    public float maxDistance = 30f; // Set to 100 for Monster Roar, 10 for footsteps
}