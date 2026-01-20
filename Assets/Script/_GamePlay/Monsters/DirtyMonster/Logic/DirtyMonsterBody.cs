using UnityEngine;

public class DirtyMonsterBody : MonoBehaviour
{
    [Header("Kill Settings")]
    [Tooltip("How much higher the player needs to be to kill the monster")]
    [SerializeField] private float killHeightOffset;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem deathVFX;
    // [SerializeField] private SoundDefinition deathSFX; // Optional if you use your sound system

    private Collider _myCollider;

    void Awake()
    {
        _myCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for Player Tag
        if (other.CompareTag("Player"))
        {
            CheckSquish(other.transform);
        }
    }

    // Also check CollisionEnter in case player uses non-trigger collider
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CheckSquish(collision.transform);
        }
    }

    private void CheckSquish(Transform player)
    {
        // 1. Height Check
        // Is the player's feet (Pivot) higher than my center + offset?
        float myTop = transform.position.y + (_myCollider.bounds.size.y / 2f);
        
        if (player.position.y > (myTop - killHeightOffset))
        {
            Die();
        }
        else
        {
            // Optional: If player touches from side/bottom, damage player?
            // PlayerHealth.TakeDamage();
        }
    }

    public void Die()
    {
        // 1. Play Effects (spawn separate object so it doesn't vanish with monster)
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        // 2. Return to Pool
        // The Manager will handle restarting the graph next time it enables
        gameObject.SetActive(false);
    }
}