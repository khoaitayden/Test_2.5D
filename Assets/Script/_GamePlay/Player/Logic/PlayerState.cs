using System.Collections;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIManager uIManager;
    [SerializeField] private CharacterController controller;
    [SerializeField] private TransformAnchorSO respawnPointAnchor;

    // NEW: Reference the SCENE OBJECT, not a prefab
    [SerializeField] private VoidAngelSequence voidAngelSceneObject;

    [Header("State")]
    [SerializeField] private bool isDead = false;

    [Header("Settings")]
    [SerializeField] private float deathDelayOnEmptyEnergy = 5.0f;
    [SerializeField] private float voidDeathYThreshold = -40f;

    [Header("Events")]
    [SerializeField] private GameEventSO onDeathEvent; 

    public bool IsDead => isDead;

    void Update()
    {
        // Void Check
        if (transform.position.y < voidDeathYThreshold && !isDead)
        {
            TriggerVoidDeath();
        }
    }

    private void TriggerVoidDeath()
    {
        Debug.Log("Fallen into Void...");
        isDead = true;


        if (voidAngelSceneObject != null)
        {
            voidAngelSceneObject.StartSequence(this.transform, Camera.main);
        }
        else
        {
            // Fallback if you forgot to assign it
            Die();
        }
    }

    public void GotAttack()
    {
        if (isDead) return;
        Die(); // Normal death (Monster hit)
    }

    private void Die()
    {
        Debug.Log("Player Died (Normal)!");
        isDead = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (uIManager != null) uIManager.ToggleDeathScreen();
        if (onDeathEvent != null) onDeathEvent.Raise();
    }

    public IEnumerator OnEmptyEnergyRoutine()
    {
        yield return new WaitForSeconds(deathDelayOnEmptyEnergy);
        Die(); // Energy death uses normal screen
    }

    public void RevivePlayer()
    {
        Debug.Log("Reviving Player...");
        isDead = false;

        if (respawnPointAnchor != null && respawnPointAnchor.Value != null)
        {
            controller.enabled = false; 
            transform.position = respawnPointAnchor.Value.position;
            controller.enabled = true;
        }
    }

    public void OnEmptyEnergy()
    {
        StartCoroutine(OnEmptyEnergyRoutine());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.CompareTag("Monster"))
        {
            GotAttack();
        }
    }
}