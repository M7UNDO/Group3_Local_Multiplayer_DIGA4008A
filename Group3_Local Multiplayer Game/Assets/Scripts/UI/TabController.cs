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
        public Color hoverTabColour;

        public Color selectedTextIcon;
        public Color deselectedTextIcon;
        public Color hoverTextIcon;

        [Header("Tab Settings")]
        [Space(5)]
        public bool hasTextComponent;
        public bool hasTextIcon;
        public bool buttonFill;
        public bool startWithActiveTab;

        int currentTab = -1;

        void Start()
        {
            if (startWithActiveTab)
            {
                ActivateTab(0);
            }
        }

        private void OnEnable()
        {
            ActivateTab(0);
        }

        public void ActivateTab(int tabNo)
        {
            currentTab = tabNo;

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(false);
                tabButtons[i].color = deselectedTabColour;

                if (buttonFill)
                    tabButtons[i].fillCenter = false;

                if (hasTextComponent)
                    tabButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                        deselectedTextIcon;

                if (hasTextIcon)
                    tabButtons[i].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                        deselectedTextIcon;
            }

            pages[tabNo].SetActive(true);
            tabButtons[tabNo].color = selectedTabColour;

            if (buttonFill)
                tabButtons[tabNo].fillCenter = true;

            if (hasTextComponent)
                tabButtons[tabNo].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                    selectedTextIcon;

            if (hasTextIcon)
                tabButtons[tabNo].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                    selectedTextIcon;
        }

        public void OnTabHover(int tabNo)
        {
            if (tabNo == currentTab) return;

            //tabButtons[tabNo].color = hoverTabColour;

            if (hasTextComponent)
                tabButtons[tabNo].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                    hoverTextIcon;

            if (hasTextIcon)
                tabButtons[tabNo].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                    hoverTextIcon;
        }

        public void OnTabExit(int tabNo)
        {
            if (tabNo == currentTab) return;

            //tabButtons[tabNo].color = deselectedTabColour;

            if (hasTextComponent)
                tabButtons[tabNo].gameObject.GetComponentInChildren<TextMeshProUGUI>().color =
                    deselectedTextIcon;

            if (hasTextIcon)
                tabButtons[tabNo].gameObject.transform.GetChild(0).GetComponent<Image>().color =
                    deselectedTextIcon;
        }
    }
}