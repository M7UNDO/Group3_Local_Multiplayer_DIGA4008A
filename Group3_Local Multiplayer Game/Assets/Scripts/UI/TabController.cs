using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View
{
    public class TabController : MonoBehaviour
    {
        [Header("Tabs & Pages")]
        [Space(5)]
        public Image[] tabButtons;
        public GameObject[] pages;

        public Color selectedTabColour;
        public Color deselectedTabColour;

        public Color selectedIconColour;
        public Color deselectedIconColour;

        [Header("Tab Settings")]
        [Space(5)]
        public bool hasTextComponent;
        public bool hasTextIcon;
        public bool buttonFill;
        public bool startWithActiveTab;

        void Start()
        {
            if (startWithActiveTab)
            {
                ActivateTab(0);
            }
        }

        public void ActivateTab(int tabNo)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(false);
                tabButtons[i].color = deselectedTabColour;
                if (buttonFill)
                    tabButtons[i].fillCenter = false;
                if (hasTextComponent)
                    tabButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                        deselectedIconColour;

                if (hasTextIcon)
                    tabButtons[i].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                        deselectedIconColour;
            }

            pages[tabNo].SetActive(true);
            tabButtons[tabNo].color = selectedTabColour;
            if (buttonFill)
                tabButtons[tabNo].fillCenter = true;
            if (hasTextComponent)
                tabButtons[tabNo].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                    selectedIconColour;
            if (hasTextIcon)
                tabButtons[tabNo].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                    selectedIconColour;
        }
    }
}