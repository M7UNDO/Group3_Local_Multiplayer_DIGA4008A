using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainUI : MonoBehaviour
{
    private PlayerControls playerControls;
    [Header("UI Elements")]
    [Space(5)]
    public Animator animator;

    public GameObject controlUIPanel;
    public GameObject settingsPanel;
    public GameObject restartScreen;

    public bool hasMenuElements;
    public GameObject[] menuUIElements;

    [Header("UI Navigation")]
    public GameObject mainMenuFirstSelected;
    public GameObject controlsFirstSelected;
    public GameObject settingsFirstSelected;
    public GameObject restartFirstSelected;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    private System.Action<InputAction.CallbackContext> backAction;

    private void OnEnable()
    {
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        backAction = ctx => Back();
        playerControls.Player.Back.performed += backAction;
    }

    private void OnDisable()
    {
        playerControls.Player.Back.performed -= backAction;
        playerControls.Player.Disable();
    }

    private void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void SetSelected(GameObject obj)
    {
        StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(obj);
    }

    public void OpenSettingsPanel()
    {
        OpenPanel(settingsPanel, settingsFirstSelected);
    }

    public void OpenControlsPanel()
    {
        OpenPanel(controlUIPanel, controlsFirstSelected);
    }

    private void OpenPanel(GameObject panel, GameObject firstSelected)
    {
        if (panelHistory.Count == 0)
        {
            if (hasMenuElements)
            {
                foreach (GameObject elem in menuUIElements)
                {
                    elem.SetActive(false);
                }
            }

            animator.SetBool("Controls", true);
        }
        else
        {
            panelHistory.Peek().SetActive(false);
        }

        panel.SetActive(true);
        panelHistory.Push(panel);

        SetSelected(firstSelected);
    }

    public void RestartPanel()
    {
        if(restartScreen != null)
        {
            restartScreen.SetActive(true);
            SetSelected(restartFirstSelected);
        }  
    }

    public void Back()
    {
        if (panelHistory.Count == 0)
            return;

        GameObject current = panelHistory.Pop();
        current.SetActive(false);

        if (panelHistory.Count > 0)
        {
            GameObject previous = panelHistory.Peek();
            previous.SetActive(true);

            if (previous == settingsPanel)
                SetSelected(settingsFirstSelected);
            else if (previous == controlUIPanel)
                SetSelected(controlsFirstSelected);
        }
        else
        {
            animator.SetBool("Controls", false);

            if (hasMenuElements)
            {
                foreach (GameObject elem in menuUIElements)
                {
                    elem.SetActive(true);
                }
            }

            SetSelected(mainMenuFirstSelected);
        }
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadLevelNumber(int levelIndex)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(levelIndex);
    }
}