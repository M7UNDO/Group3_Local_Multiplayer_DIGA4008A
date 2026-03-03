using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HighlightText : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Text Style Settings")]
    [Space(5)]

    public TMP_FontAsset originalFont;
    public TMP_FontAsset highlightFont;
    public bool changeFont;
    public bool isBoldOnHighlight;


    [Header("Image Settings")]
    [Space(5)]

    public bool imgFillsOnHover;
    public Image buttonImage;


    [Header("Text Settings")]
    [Space(5)]

    public TextMeshProUGUI buttonTxt;
    private float originalFontSize;
    public Color originalColor;
    public Color highlightColor;

    [SerializeField]
    private Button button;
    public bool textIsFirstChild = true;

    [Header("border Settings")]
    [Space(5)]

    public Image border;
    public bool hasBorder;



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
        if (hasBorder)
        {
            border.gameObject.SetActive(true);
        }

        if (imgFillsOnHover)
        {
            buttonImage.fillCenter = true;
        }


        buttonTxt.color = highlightColor;

        if (changeFont)
        {
            buttonTxt.font = highlightFont;
        }

        if (isBoldOnHighlight)
        {
            buttonTxt.fontStyle = FontStyles.Bold;
        }
    }

    public void ChangeColourBack()
    {

        if (hasBorder)
        {
            border.gameObject.SetActive(false);
        }

        if (imgFillsOnHover)
        {
            buttonImage.fillCenter = false;
        }

        buttonTxt.color = originalColor;

        if (changeFont)
        {
            buttonTxt.font = originalFont;
        }
    }


    public void OnSelect(BaseEventData eventData)
    {
        buttonTxt.color = highlightColor;
    }


    public void OnDeselect(BaseEventData eventData)
    {
        buttonTxt.color = originalColor;
    }
}
