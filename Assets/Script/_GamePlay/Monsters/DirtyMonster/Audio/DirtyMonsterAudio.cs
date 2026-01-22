using System;
using UnityEngine;

public class DirtyMonsterAudio : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private SoundDefinition sfx_Land;
    [SerializeField] private SoundDefinition sfx_Attack;
    
    // We don't need Death here because DirtyMonsterBody handles death logic, 
    // but you could centralize it here if you wanted. 
    // For now, let's keep Land here to be called by the Behavior Graph.

    public void PlayLandSound()
    {
        if (SoundManager.Instance != null && sfx_Land != null)
        {
            SoundManager.Instance.PlaySound(sfx_Land, transform.position);
        }
    }

    public void PlayAttackSound()
    {
        if (SoundManager.Instance != null && sfx_Attack != null)
        {
            SoundManager.Instance.PlaySound(sfx_Attack, transform.position);
        }
    }
}