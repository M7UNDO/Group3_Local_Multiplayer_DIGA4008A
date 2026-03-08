using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro; // Add if using TextMeshPro

public class PlayerSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playerSelectionPanel; // Parent panel for all player slots
    public List<PlayerSelectionSlot> selectionSlots; // Assign in inspector or find dynamically
    public TextMeshProUGUI readyCountText;
    public GameObject startGameButton;

    [Header("Settings")]
    public string gameSceneName = "GameLevel"; // Name of your main game scene
    public int minPlayersToStart = 1;
    public int maxPlayers = 4;

    [Header("Player Prefabs")]
    public List<GameObject> playerPrefabs; // Different character prefabs if you have multiple

    private List<PlayerInput> joinedPlayers = new List<PlayerInput>();
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();
    private bool gameStarted = false;

    private void Awake()
    {
        // Find all selection slots if not assigned
        if (selectionSlots == null || selectionSlots.Count == 0)
        {
            selectionSlots = new List<PlayerSelectionSlot>(
                FindObjectsByType<PlayerSelectionSlot>(FindObjectsSortMode.None)
            );
        }

        // Subscribe to player join events
        var playerInputManager = GetComponent<PlayerInputManager>();
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        var playerInputManager = GetComponent<PlayerInputManager>();
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        if (gameStarted) return;

        int playerIndex = player.playerIndex;

        // Check if we haven't exceeded max players
        if (joinedPlayers.Count >= maxPlayers)
        {
            Destroy(player.gameObject);
            return;
        }

        joinedPlayers.Add(player);
        playerReadyStatus[playerIndex] = false;

        // Position the player in the UI (optional - you might want to hide them)
        player.transform.position = Vector3.zero; // Or move to a hidden area

        // Update the UI slot for this player
        if (playerIndex < selectionSlots.Count)
        {
            selectionSlots[playerIndex].ActivateSlot(player);
        }

        // Setup player input for UI navigation
        SetupPlayerForUI(player);

        UpdateUI();
        Debug.Log($"Player {playerIndex} joined selection screen");
    }

    private void OnPlayerLeft(PlayerInput player)
    {
        if (gameStarted) return;

        int playerIndex = player.playerIndex;
        joinedPlayers.Remove(player);
        playerReadyStatus.Remove(playerIndex);

        // Update UI slot
        if (playerIndex < selectionSlots.Count)
        {
            selectionSlots[playerIndex].DeactivateSlot();
        }

        UpdateUI();
        Debug.Log($"Player {playerIndex} left selection screen");
    }

    private void SetupPlayerForUI(PlayerInput player)
    {
        // Disable player control scripts for the selection screen
        var playerController = player.GetComponent<YourPlayerController>(); // Replace with your controller type
        if (playerController != null)
            playerController.enabled = false;

        // Add a UI input handler if needed
        var uiHandler = player.GetComponent<PlayerUIInputHandler>();
        if (uiHandler == null)
            uiHandler = player.gameObject.AddComponent<PlayerUIInputHandler>();

        uiHandler.Initialize(player, this);
    }

    public void TogglePlayerReady(int playerIndex)
    {
        if (gameStarted) return;
        if (!playerReadyStatus.ContainsKey(playerIndex)) return;

        playerReadyStatus[playerIndex] = !playerReadyStatus[playerIndex];

        // Update UI
        if (playerIndex < selectionSlots.Count)
        {
            selectionSlots[playerIndex].SetReady(playerReadyStatus[playerIndex]);
        }

        UpdateUI();
    }

    public void ChangePlayerSelection(int playerIndex, int direction)
    {
        if (gameStarted) return;
        if (playerReadyStatus.ContainsKey(playerIndex) && playerReadyStatus[playerIndex])
            return; // Can't change if ready

        // Handle character selection cycling
        if (playerIndex < selectionSlots.Count)
        {
            selectionSlots[playerIndex].CycleSelection(direction);
        }
    }

    private void UpdateUI()
    {
        if (readyCountText != null)
        {
            int readyCount = 0;
            foreach (var ready in playerReadyStatus.Values)
            {
                if (ready) readyCount++;
            }
            readyCountText.text = $"Ready: {readyCount}/{joinedPlayers.Count}";
        }

        if (startGameButton != null)
        {
            startGameButton.SetActive(CanStartGame());
        }
    }

    private bool CanStartGame()
    {
        if (joinedPlayers.Count < minPlayersToStart) return false;

        // Check if all joined players are ready
        foreach (var player in joinedPlayers)
        {
            if (!playerReadyStatus.ContainsKey(player.playerIndex) ||
                !playerReadyStatus[player.playerIndex])
                return false;
        }
        return true;
    }

    public void StartGame()
    {
        if (!CanStartGame() || gameStarted) return;

        gameStarted = true;
        Debug.Log("Starting game with " + joinedPlayers.Count + " players");

        // Create a DontDestroyOnLoad object to carry player data
        GameObject playerDataObject = new GameObject("PlayerDataCarrier");
        DontDestroyOnLoad(playerDataObject);

        var playerDataCarrier = playerDataObject.AddComponent<PlayerDataCarrier>();

        // Store player information
        foreach (var player in joinedPlayers)
        {
            int selectedCharacter = selectionSlots[player.playerIndex].GetSelectedCharacterIndex();
            playerDataCarrier.AddPlayerData(player.playerIndex, selectedCharacter);
        }

        // Load the game scene
        SceneManager.LoadScene(gameSceneName);
    }
}