using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class SpawnPlayerSetupMenu : MonoBehaviour
{
    public GameObject playerSetupMenuPrefab;
    public PlayerInput input;

    private GameObject rootMenu;

    private void Start()
    {
        Debug.Log("SpawnPlayerSetupMenu.Start() called for player index: " + input?.playerIndex);

        // Check if input is assigned
        if (input == null)
        {
            Debug.LogError("input is NULL on SpawnPlayerSetupMenu!");
            return;
        }

        // Find MainLayout
        rootMenu = GameObject.Find("MainLayout");
        if (rootMenu == null)
        {
            Debug.LogError("MainLayout NOT FOUND in scene! Make sure it exists and is named exactly 'MainLayout'");
            return;
        }
        Debug.Log("MainLayout found successfully");

        // Check if prefab is assigned
        if (playerSetupMenuPrefab == null)
        {
            Debug.LogError("playerSetupMenuPrefab is NOT assigned in the Inspector!");
            return;
        }
        Debug.Log("playerSetupMenuPrefab is assigned");

        // Instantiate the menu
        GameObject menu;
        try
        {
            menu = Instantiate(playerSetupMenuPrefab, rootMenu.transform);
            Debug.Log("Menu instantiated successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to instantiate menu: " + e.Message);
            return;
        }

        // Get MultiplayerEventSystem
        var multiplayerEventSystem = menu.GetComponent<MultiplayerEventSystem>();
        if (multiplayerEventSystem == null)
        {
            Debug.LogError("MultiplayerEventSystem not found on instantiated menu!");
            return;
        }
        multiplayerEventSystem.playerRoot = menu;
        Debug.Log("MultiplayerEventSystem configured");

        // Get UI Input Module
        var uiInputModule = menu.GetComponent<InputSystemUIInputModule>();
        if (uiInputModule == null)
        {
            Debug.LogError("InputSystemUIInputModule not found on instantiated menu!");
            return;
        }

        // Assign to player input
        input.uiInputModule = uiInputModule;
        Debug.Log("UI Input Module assigned to player");

        // Set player index on controller
        var menuController = menu.GetComponent<PlayerSetupMenuController>();
        if (menuController == null)
        {
            Debug.LogError("PlayerSetupMenuController not found on instantiated menu!");
            return;
        }

        menuController.SetPlayerIndex(input.playerIndex);
        Debug.Log($"Player {input.playerIndex} setup complete");
    }
}