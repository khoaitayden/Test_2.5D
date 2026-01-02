using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private GameObject audioSourcePrefab; // Create an empty GameObject with an AudioSource, save as Prefab
    [SerializeField] private int poolSize = 20;

    private List<AudioSource> pool = new List<AudioSource>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(audioSourcePrefab, transform);
            obj.SetActive(false);
            pool.Add(obj.GetComponent<AudioSource>());
        }
    }

     public AudioSource PlaySound(SoundDefinition def, Vector3 position, float volumeMult = 1f, float pitchMult = 1f)
    {
        if (def == null || def.clips.Length == 0) return null;

        AudioSource source = GetFreeSource();
        source.transform.position = position;
        source.gameObject.SetActive(true);

        source.clip = def.clips[Random.Range(0, def.clips.Length)];
        source.outputAudioMixerGroup = def.mixerGroup;
        
        // Calculate final pitch and volume
        float finalPitch = (def.pitch + Random.Range(-def.pitchVariance, def.pitchVariance)) * pitchMult;
        // Clamp pitch to avoid divide-by-zero or negative time errors
        if (finalPitch < 0.1f) finalPitch = 0.1f; 

        float finalVolume = (def.volume + Random.Range(-def.volumeVariance, def.volumeVariance)) * volumeMult;

        source.volume = finalVolume;
        source.pitch = finalPitch;
        source.spatialBlend = def.spatialBlend;
        source.minDistance = def.minDistance;
        source.maxDistance = def.maxDistance;
        
        source.Play();

        // --- THE FIX ---
        // 1. Calculate actual duration based on pitch (Lower pitch = Longer time)
        float trueDuration = source.clip.length / Mathf.Abs(finalPitch);
        
        // 2. Start the Disable routine with the correct time
        StartCoroutine(DisableSourceWithFade(source, trueDuration, finalVolume));
        
        return source;
    }
    private System.Collections.IEnumerator DisableSourceWithFade(AudioSource source, float duration, float startVolume)
    {
        // Wait until near the end of the clip (minus 0.5 seconds for fade out)
        float fadeDuration = 0.5f;
        float waitTime = duration - fadeDuration;

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // --- FADE OUT LOGIC ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Smoothly lerp volume to 0
            source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        source.Stop();
        source.gameObject.SetActive(false);
    }
    public AudioSource CreateLoop(SoundDefinition def, Transform parent)
    {
        GameObject obj = new GameObject("Loop_" + def.name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        
        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = def.clips[0];
        source.outputAudioMixerGroup = def.mixerGroup;
        source.loop = true;
        source.spatialBlend = def.spatialBlend;
        source.volume = def.volume;
        source.maxDistance = def.maxDistance;
        source.Play();
        return source;
    }

    private AudioSource GetFreeSource()
    {
        foreach (var s in pool) if (!s.gameObject.activeInHierarchy) return s;
        return pool[0]; // Recycle oldest if full
    }

    private System.Collections.IEnumerator DisableSource(AudioSource s, float t)
    {
        yield return new WaitForSeconds(t);
        s.gameObject.SetActive(false);
    }
}