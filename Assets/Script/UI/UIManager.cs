using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private Button restartButton;
    void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonPressed);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ToggleDeathScreen()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (deathScreen.activeSelf)
        {
            deathScreen.SetActive(false);
        }
        else
        {
            deathScreen.SetActive(true);
        }
    }
    public void OnRestartButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
