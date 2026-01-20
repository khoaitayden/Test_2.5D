using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private Button restartButton;

    [Header("Events")]
    [SerializeField] private GameEventSO respawnEvent;

    void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonPressed);
        deathScreen.SetActive(false); // Ensure hidden at start
    }

    public void ToggleDeathScreen()
    {
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
        ToggleDeathScreen();

        if (respawnEvent != null) 
            respawnEvent.Raise();
    }
}