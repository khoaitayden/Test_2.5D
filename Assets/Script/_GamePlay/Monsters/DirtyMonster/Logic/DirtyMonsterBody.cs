using UnityEngine;

public class DirtyMonsterBody : MonoBehaviour
{
    [Header("Kill Settings")]
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
        if (SoundManager.Instance != null && sfx_Death != null)
        {
            SoundManager.Instance.PlaySound(sfx_Death, transform.position);
        }
        gameObject.SetActive(false);
    }
}