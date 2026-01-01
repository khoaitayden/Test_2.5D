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
        
        // Randomize + Multipliers (from player speed)
        source.volume = (def.volume + Random.Range(-def.volumeVariance, def.volumeVariance)) * volumeMult;
        source.pitch = (def.pitch + Random.Range(-def.pitchVariance, def.pitchVariance)) * pitchMult;

        source.spatialBlend = def.spatialBlend;
        source.minDistance = def.minDistance;
        source.maxDistance = def.maxDistance;
        source.Play();

        StartCoroutine(DisableSource(source, source.clip.length + 0.2f));
        return source;
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