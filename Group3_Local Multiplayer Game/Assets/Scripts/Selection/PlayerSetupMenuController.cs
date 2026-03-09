using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSetupMenuController : MonoBehaviour
{
    private int playerIndex;
    private Canvas canvas;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI titleText;

    private void Awake()
    {
        // Get the canvas on the root
        canvas = GetComponent<Canvas>();
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
        titleText.text = "Player " + (index + 1);

        // Set canvas sort order to ensure proper layering
        if (canvas != null)
        {
            canvas.sortingOrder = index; // Player 1 = 0, Player 2 = 1
        }

        // Position the panel based on player index
        if (panelRect != null)
        {
            if (index == 0)
            {
                // Left half
                panelRect.anchorMin = new Vector2(0, 0);
                panelRect.anchorMax = new Vector2(0.5f, 1);
            }
            else if (index == 1)
            {
                // Right half
                panelRect.anchorMin = new Vector2(0.5f, 0);
                panelRect.anchorMax = new Vector2(1f, 1);
            }

            // Reset offsets
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        Debug.Log($"Player {index} menu setup complete - Panel should be visible");
    }

    public void ReadyPlayer()
    {
        Debug.Log($"Player {playerIndex} pressed ready button");
        PlayerConfigurationManager.Instance.ReadyPlayer(playerIndex);
    }
}