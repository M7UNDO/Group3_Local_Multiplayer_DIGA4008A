using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManageUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Space(5)]
    public Animator animator;

    [Header("Panels")]
    public GameObject controlUIPanel;
    public GameObject audioUIPanel;
    public bool hasMenuElements;
    public GameObject[] menuUIElements;
    public HighlightText highlightText;
    public TextMeshProUGUI[] buttonTxt;

    /*
    [Header("Saving states")]
    [Space(5)]
    public TextMeshProUGUI gameStateTxt;
    public Color savedColour;
    public Color resetColour;

    [Header("Levels Index")]
    [Space(5)]

    public int level; // Current level index
    public int tutorialLevel = 1;
    public int mainLevel = 2;*/

    [Header("Toggles")]
    private bool toggle;
    public Toggle loadToggle;


    public void ControlPanel()
    {
        toggle = !toggle;

        if (!toggle)
        {
            animator.SetBool("Controls", false);
            controlUIPanel.SetActive(false);

            foreach (TextMeshProUGUI txt in buttonTxt)
            {
                txt.color = highlightText.originalColor;
            }

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

    public void AudioPanel()
    {
        toggle = !toggle;

        if (!toggle)
        {
            animator.SetBool("Controls", false);
            audioUIPanel.SetActive(false);

            foreach (TextMeshProUGUI txt in buttonTxt)
            {
                txt.color = highlightText.originalColor;
            }

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
            audioUIPanel.SetActive(true);
            animator.SetBool("Controls", true);
            foreach (GameObject elem in menuUIElements)
            {
                elem.SetActive(false);
            }
        }
    }
    /*
    public void GameSaved()
    {
        gameStateTxt.gameObject.SetActive(true);
        gameStateTxt.text = "Game Saved!";
        gameStateTxt.color = savedColour;
        StartCoroutine(DeactivateAfterDelay(gameStateTxt.gameObject, 1.5f));
    }

    private IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    public void GameReset()
    {
        gameStateTxt.gameObject.SetActive(true);
        gameStateTxt.text = "Game Reset!";
        gameStateTxt.color = resetColour;
        StartCoroutine(DeactivateAfterDelay(gameStateTxt.gameObject, 1.5f));
    }*/

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadLevelNumber(int levelIndex)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(levelIndex);
    }

    /*
    public void TutorialLevelToggle()
    {
        if (loadToggle.isOn)
        {
            level = tutorialLevel;
        }
        else
        {
            level = mainLevel;
        }
    }
    */
}