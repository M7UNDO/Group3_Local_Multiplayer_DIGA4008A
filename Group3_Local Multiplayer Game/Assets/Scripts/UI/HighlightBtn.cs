using UnityEngine;
using UnityEngine.UI;

public class HighlightBtn : MonoBehaviour
{
    [Header("Colour Settings")]
    public Color originalColour;
    public Color highlightColour;

    [Header("Button & Icon")]
    public Image button;
    public Image icon;

    public bool canFill;

    private void Start()
    {
        button = GetComponent<Image>();
        icon = transform.GetChild(0).GetComponent<Image>();

        if (canFill)
        {
            button.fillCenter = false;
            icon.color = originalColour;
        }
        else
        {
            button.color = originalColour;
            icon.color = highlightColour;
        }

    }

    public void ChangeColour()
    {
        if (canFill)
        {
            button.fillCenter = true;
            icon.color = highlightColour;
        }
        else
        {
            button.color = highlightColour;
            icon.color = originalColour;
        }

    }

    public void ResetColour()
    {
        if (canFill)
        {
            button.fillCenter = false;
            icon.color = originalColour;
        }
        else
        {
            button.color = originalColour;
            icon.color = highlightColour;
        }
    }


}