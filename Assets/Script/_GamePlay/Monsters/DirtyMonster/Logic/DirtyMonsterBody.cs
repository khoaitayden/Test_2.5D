using UnityEngine;

public class DirtyMonsterBody : MonoBehaviour
{
    [Header("Kill Settings")]
    [Tooltip("How much higher the player needs to be to kill the monster")]
    [SerializeField] private float killHeightOffset;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem deathVFX;
    // [SerializeField] private SoundDefinition deathSFX;

    private Collider _myCollider;

    void Awake()
    {
        _myCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CheckSquish(other.transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CheckSquish(collision.transform);
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
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }
}