using UnityEngine;
using UnityEngine.Events;

public class PlayerState : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIManager uIManager; // Direct ref for now, can be event later

    [Header("State")]
    [SerializeField] private bool isDead = false;

    [Header("Events")]
    [SerializeField] private GameEventSO onDeathEvent; // Create: "evt_PlayerDeath"

    // Public API
    public bool IsDead => isDead;

    public void TakeDamage(int amount = 1) // Or pass a damage source, etc.
    {
        if (isDead) return;

        Die();
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        isDead = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (uIManager != null) uIManager.ToggleDeathScreen();
        if (onDeathEvent != null) onDeathEvent.Raise();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead == false && other.CompareTag("Monster"))
        {
            TakeDamage();
        }
    }
}