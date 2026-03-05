using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HighlightText : MonoBehaviour
{
    [Header("Image Settings")]
    [Space(5)]

    public bool imgFillsOnHover;
    private Image buttonImage;


    [Header("Text Settings")]
    [Space(5)]

    private TextMeshProUGUI buttonTxt;
    public Color originalColor;
    public Color highlightColor;

    private Button button;
    public bool textIsFirstChild = true;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (imgFillsOnHover)
        {
            buttonImage = GetComponent<Image>();
        }


        if (textIsFirstChild)
        {
            buttonTxt = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        }
    }
    public void ChangeColour()
    {

        if (imgFillsOnHover)
        {
            buttonImage.fillCenter = true;
        }


        buttonTxt.color = highlightColor;
    }

    public void ChangeColourBack()
    {

        if (imgFillsOnHover)
        {
            buttonImage.fillCenter = false;
        }

        buttonTxt.color = originalColor;
    }
}
