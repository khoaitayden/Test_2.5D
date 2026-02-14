using System.Collections;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIManager uIManager;
    [SerializeField] private CharacterController controller;
    [SerializeField] private TransformAnchorSO respawnPointAnchor;

    [SerializeField] private VoidAngelSequence voidAngelSceneObject;

    [Header("State")]
    [SerializeField] private bool isDead = false;

    [Header("Settings")]
    [SerializeField] private float deathDelayOnEmptyEnergy;
    [SerializeField] private float voidDeathYThreshold;

    [Header("Events")]
    [SerializeField] private GameEventSO onDeathEvent; 

    public bool IsDead => isDead;

    void Update()
    {
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
            Die();
        }
    }

    public void OnEmptyEnergy()
    {
        if (isDead) return;

        StartCoroutine(OnEmptyEnergyRoutine());
    }

    public IEnumerator OnEmptyEnergyRoutine()
    {
        yield return new WaitForSeconds(deathDelayOnEmptyEnergy);
        if (isDead) yield break; 
        Die(); 
    }

    public void GotAttack()
    {
        if (isDead) return;
        Die(); 
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

    void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.CompareTag("Monster"))
        {
            GotAttack();
        }
    }
}