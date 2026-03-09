using System.Collections;
using TMPro;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("UI")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveText;

    [Header("Cookie Goal")]
    public int cookiesRequired = 3;
    private int cookiesDelivered = 0;

    private int currentObjective = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(ShowStartObjective());
    }

    IEnumerator ShowStartObjective()
    {
        objectivePanel.SetActive(true);
        objectiveText.text = "Find your way into the store";

        yield return new WaitForSeconds(5f);

        objectivePanel.SetActive(false);
    }

    public void EnterStore()
    {
        if (currentObjective != 0) return;

        currentObjective = 1;

        objectivePanel.SetActive(true);
        objectiveText.text = "Obtain magical cookies from the shop";
    }

    public void DeliverCookie()
    {
        if (currentObjective != 1) return;

        cookiesDelivered++;

        objectiveText.text = "Deliver Cookies: " + cookiesDelivered + " / " + cookiesRequired;

        if (cookiesDelivered >= cookiesRequired)
        {
            CompleteGame();
        }
    }

    void CompleteGame()
    {
        currentObjective = 2;

        objectiveText.text = "All cookies delivered! Escape successful!";
        Debug.Log("GAME COMPLETE");
    }
}