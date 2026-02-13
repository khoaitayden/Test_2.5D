using UnityEngine;

public class DirtyMonsterBody : MonoBehaviour
{
    [Header("Kill Settings")]
    [Tooltip("How much higher the player needs to be to kill the monster")]
    [SerializeField] private float killHeightOffset;
    [SerializeField] private float slowDuration;
    [SerializeField] private float slowMultiplier;

    [Header("Effects")]
    [SerializeField] private SoundDefinition sfx_Death;

    private Collider _myCollider;

    void Awake()
    {
        _myCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement movement = other.GetComponent<PlayerMovement>();
            movement.ApplyEnvironmentalSlow(slowMultiplier, slowDuration);
            CheckSquish(other.transform);
        }
    }
    private void CheckSquish(Transform player)
    {
        float myTop = transform.position.y + (_myCollider.bounds.size.y / 2f);
        
        if (player.position.y > (myTop - killHeightOffset))
        {
            Die();
        }

    }

    public void Die()
    {
        // 1. Play Visuals
        if (sfx_Death != null)
        {
            Instantiate(sfx_Death, transform.position, Quaternion.identity);
        }

        // 2. NEW: Play Sound
        if (SoundManager.Instance != null && sfx_Death != null)
        {
            SoundManager.Instance.PlaySound(sfx_Death, transform.position);
        }

        // 3. Return to Pool
        gameObject.SetActive(false);
    }
}