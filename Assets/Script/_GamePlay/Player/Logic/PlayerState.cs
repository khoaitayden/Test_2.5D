using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerState : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIManager uIManager; // Direct ref for now, can be event later

    [Header("State")]
    [SerializeField] private bool isDead = false;

    [Header("Settings")]
    [SerializeField] private float deathDelayOnEmptyEnergy = 5.0f;
    [Header("Events")]
    [SerializeField] private GameEventSO onDeathEvent; 

    // Public API
    public bool IsDead => isDead;

    public void GotAttack()
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

    public IEnumerator OnEmptyEnergyRoutine()
    {
        yield return new WaitForSeconds(deathDelayOnEmptyEnergy);
        Die();
        yield return null;
    }

    public void OnEmptyEnergy()
    {
        StartCoroutine(OnEmptyEnergyRoutine());
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead == false && other.CompareTag("Monster"))
        {
            GotAttack();
        }
    }

}