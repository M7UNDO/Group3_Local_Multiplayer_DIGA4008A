using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseScript : MonoBehaviour
{
    private bool toggle;
    private PlayerControls playerControls;
    private int levelRestart = 1;
    private System.Action<InputAction.CallbackContext> pauseAction;

    [Header("Pause UI Elements")]
    [Space(5)]
    public GameObject pausePanel;
    [SerializeField] private ManageUI manageUI;

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

    public void Pause()
    {
        if (manageUI.controlUIPanel.activeSelf)
        {
            manageUI.ControlPanel();
            return;
        }
        else if (manageUI.audioUIPanel.activeSelf)
        {
            manageUI.AudioPanel();
            return;
        }


        toggle = !toggle;

        if (toggle)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

    }

    public void CompleteRestartGame()
    {
        SceneManager.LoadScene(levelRestart);
        Time.timeScale = 1f;
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }
}