// NEW SCRIPT: MonsterTouchSensor.cs
// Put this script on your Monster prefab.
using UnityEngine;

public class MonsterTouchSensor : MonoBehaviour
{
    // Public property that other scripts (like our action) can read.
    public bool IsTouchingPlayer { get; private set; }
    [SerializeField] private UIManager uiManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsTouchingPlayer = true;
            uiManager.ToggleDeathScreen();
            Debug.Log("Monster started touching Player!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsTouchingPlayer = false;
            Debug.Log("Monster stopped touching Player!");
        }
    }
}