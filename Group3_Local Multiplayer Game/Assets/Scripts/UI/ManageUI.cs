using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManageUI : MonoBehaviour
{
    public bool toggle;

    [Header("UI Elements")]
    [Space(5)]
    public Animator animator;
    public GameObject controlUIPanel;
    public GameObject settingsPanel;
    public bool hasMenuElements;
    public GameObject[] menuUIElements;
    /*
    [Header("Saving states")]
    [Space(5)]
    public TextMeshProUGUI gameStateTxt;
    public Color savedColour;
    public Color resetColour;

    [Header("Main Menu Buttons")]
    [Space(5)]
    public bool isMainMenu;
    public Button continueButton;
    public Button newGameButton;

    [Header("Confirmation UI")]
    [Space(5)]
    public GameObject confirmResetPanel;
    public Button yesButton;
    public Button noButton;*/

    private void Awake()
    {

       animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void Start()
    {


        /*if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            CheckForSave();
        }*/
    }

    /*private void CheckForSave()
    {
        bool hasSave = SaveSystem.HasSave();

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            var text = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.alpha = hasSave ? 1f : 0.4f;
            }
        }

        if (newGameButton != null)
            newGameButton.interactable = true;

        Debug.Log(
            hasSave ? "Save file found: Continue enabled." : "No save file:  Continue disabled."
        );
    }

    public void ContinueGame()
    {
        if (SaveSystem.HasSave())
        {
            Debug.Log("Continuing saved game...");
            SceneManager.LoadScene("Level_Scene");
        }
        else
        {
            Debug.Log("No save found: starting a new game instead.");
        }
    }

    public void AskForResetConfirmation()
    {
        if (SaveSystem.HasSave())
        {
            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(true);

            Time.timeScale = 0f;
        }
        else
        {
            Debug.Log("No save file found, starting a new game immediately.");
            NewGame();
        }
    }

    public void NewGame()
    {
        Debug.Log("Starting new game...");
        SaveSystem.ResetSave();
        SceneManager.LoadScene("Tutorial Level");
    }

    public void ConfirmResetYes()
    {
        Time.timeScale = 1f;
        confirmResetPanel.SetActive(false);
        SaveSystem.ResetSave();
        SceneManager.LoadScene("Tutorial Level");
    }

    public void ConfirmResetNo()
    {
        Time.timeScale = 1f;
        confirmResetPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            attackUIPanel.SetActive(!attackUIPanel.activeSelf);
        }
    }*/

    public void SettingsPanel()
    {
        toggle = !toggle;

        if (!toggle)
        {
            settingsPanel.SetActive(false);
            animator.SetBool("Controls", false);

            if (hasMenuElements)
            {
                foreach (GameObject elem in menuUIElements)
                {
                    elem.SetActive(true);
                }
            }
        }

        if (toggle)
        {
            settingsPanel.SetActive(true);
            animator.SetBool("Controls", true);
            foreach (GameObject elem in menuUIElements)
            {
                elem.SetActive(false);
            }
        }
    }

    public void ControlPanel()
    {
        toggle = !toggle;

        if (!toggle)
        {
            animator.SetBool("Controls", false);
            controlUIPanel.SetActive(false);

            if (hasMenuElements)
            {
                foreach (GameObject elem in menuUIElements)
                {
                    elem.SetActive(true);

                }
            }

        }

        if (toggle)
        {
            controlUIPanel.SetActive(true);
            animator.SetBool("Controls", true);

            foreach (GameObject elem in menuUIElements)
            {
                elem.SetActive(false);
            }

        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadLevelNumber(int levelIndex)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(levelIndex);
    }
}