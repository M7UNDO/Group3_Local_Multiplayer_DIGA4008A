using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseScript : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    private bool toggle;
    private PlayerControls playerControls;

    [Header("Pause UI Elements")]
    [Space(5)]
    public GameObject pausePanel;

    [SerializeField]
    private ManageUI manageUI;

    [Header("Pause Settings")]
    [Space(5)]
    private System.Action<InputAction.CallbackContext> pauseAction;
    public static bool IsGamePaused { get; private set; } = false;

    private void OnEnable()
    {
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        pauseAction = ctx => Pause();
        playerControls.Player.Pause.performed += pauseAction;
    }

    private void OnDisable()
    {
        playerControls.Player.Pause.performed -= pauseAction;
        playerControls.Player.Disable();
    }

    public static void SetPause(bool paused)
    {
        IsGamePaused = paused;
    }

    public void Pause()
    {
        if (manageUI.settingsPanel.activeSelf)
        {
            manageUI.SettingsPanel();
            return;
        }

        toggle = !toggle;

        if (toggle)
        {
            pausePanel.SetActive(true);
            IsGamePaused = true;
        }
        else
        {
            pausePanel.SetActive(false);
            IsGamePaused = false;
        }
    }

    public void LoadMainMenu()
    {
        IsGamePaused = false;
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }
}