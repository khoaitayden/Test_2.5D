using System.Collections.Generic;
using UnityEngine;
using System.Collections;
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
        
        float finalPitch = (def.pitch + Random.Range(-def.pitchVariance, def.pitchVariance)) * pitchMult;
        if (finalPitch < 0.1f) finalPitch = 0.1f; // Safety

        float finalVolume = (def.volume + Random.Range(-def.volumeVariance, def.volumeVariance)) * volumeMult;

        source.volume = finalVolume;
        source.pitch = finalPitch;
        source.spatialBlend = def.spatialBlend;
        source.minDistance = def.minDistance;
        source.maxDistance = def.maxDistance;
        
        source.Play();

        float clipDuration = source.clip.length / Mathf.Abs(finalPitch);

        float totalLifeTime = clipDuration + def.tailSeconds;
        
        StartCoroutine(DisableSourceDelayed(source, totalLifeTime));
        
        return source;
    }

    private IEnumerator DisableSourceDelayed(AudioSource source, float time)
    {
        yield return new WaitForSeconds(time);
        source.Stop();
        source.gameObject.SetActive(false);
    }
    private System.Collections.IEnumerator DisableSourceWithFade(AudioSource source, float duration, float startVolume)
    {
        float fadeDuration = 0.5f;
        float waitTime = duration - fadeDuration;

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

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
        return pool[0];
    }

    private System.Collections.IEnumerator DisableSource(AudioSource s, float t)
    {
        yield return new WaitForSeconds(t);
        s.gameObject.SetActive(false);
    }
}