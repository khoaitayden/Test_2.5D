using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private Button restartButton;

    [Header("Events")]
    [SerializeField] private GameEventSO respawnEvent; // Drag "evt_PlayerRespawn"

    void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonPressed);
        deathScreen.SetActive(false); // Ensure hidden at start
    }

    public void ToggleDeathScreen()
    {
        // Simple toggle based on current state
        bool isActive = !deathScreen.activeSelf;
        deathScreen.SetActive(isActive);

        if (isActive)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OnRestartButtonPressed()
    {
        // 1. Hide Screen
        ToggleDeathScreen();

        // 2. Raise Respawn Event (Tells Player to move, Manager to reset item, etc.)
        if (respawnEvent != null) 
            respawnEvent.Raise();
    }
}