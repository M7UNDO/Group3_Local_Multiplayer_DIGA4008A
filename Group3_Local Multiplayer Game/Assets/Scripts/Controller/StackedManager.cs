using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class StackManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerStackInfo
    {
        public GameObject playerObject;
        public PlayerInput playerInput;
        public ThirdPersonController controller;
        public PlayerInputHandler inputHandler;
        public int playerIndex;
        public bool isTop;
    }

    [Header("Stack Settings")]
    public GameObject stackedCharacterPrefab;
    public float stackHeightOffset = 1.5f;
    public KeyCode stackTestKey = KeyCode.F; // For testing, remove in final build
    public PlayerInputManager playerInputManager;

    [Header("Debug")]
    public bool stackActive = false;

    private List<PlayerStackInfo> activePlayers = new List<PlayerStackInfo>();
    private GameObject currentStackedCharacter;
    private StackedController stackedController;
    private StackedInputHandler stackedInputHandler;

    // Singleton for easy access
    public static StackManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterPlayer(GameObject player, int playerIndex)
    {
        PlayerStackInfo info = new PlayerStackInfo
        {
            playerObject = player,
            playerInput = player.GetComponent<PlayerInput>(),
            controller = player.GetComponent<ThirdPersonController>(),
            inputHandler = player.GetComponent<PlayerInputHandler>(),
            playerIndex = playerIndex,
            isTop = false
        };

        activePlayers.Add(info);
        Debug.Log($"Player {playerIndex} registered for stacking");
    }

    public void AttemptStack(GameObject requestingPlayer, GameObject otherPlayer)
    {
        if (stackActive)
        {
            Debug.Log("Already stacked!");
            return;
        }

        PlayerStackInfo bottomPlayer = null;
        PlayerStackInfo topPlayer = null;

        // Determine who should be bottom and who should be top
        // The requesting player becomes the bottom (controls movement)
        foreach (var info in activePlayers)
        {
            if (info.playerObject == requestingPlayer)
                bottomPlayer = info;
            else if (info.playerObject == otherPlayer)
                topPlayer = info;
        }

        if (bottomPlayer == null || topPlayer == null)
        {
            Debug.LogError("Could not find both players for stacking");
            return;
        }

        // Check if players are close enough to stack
        float distance = Vector3.Distance(bottomPlayer.playerObject.transform.position,
                                         topPlayer.playerObject.transform.position);
        if (distance > 3f)
        {
            Debug.Log("Players are too far apart to stack");
            return;
        }

        PerformStack(bottomPlayer, topPlayer);
    }

    private void PerformStack(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        // Store position for stacked character
        Vector3 stackPosition = bottomPlayer.playerObject.transform.position;
        stackPosition.y += stackHeightOffset;

        // Deactivate individual players
        for(int i = 0; i < bottomPlayer.playerObject.transform.childCount; i++)
        {
            Transform child = bottomPlayer.playerObject.transform.GetChild(i);
            child.gameObject.SetActive(false);
        }

        for(int i = 0; i < topPlayer.playerObject.transform.childCount; i++)
        {
            Transform child = topPlayer.playerObject.transform.GetChild(i);
            child.gameObject.SetActive(false);
        }

        DisableComponents(bottomPlayer, topPlayer);
        //bottomPlayer.playerObject.SetActive(false);
        //topPlayer.playerObject.SetActive(false);

        // Create stacked character
        currentStackedCharacter = Instantiate(stackedCharacterPrefab, stackPosition, Quaternion.identity);
        stackedController = currentStackedCharacter.GetComponent<StackedController>();
        stackedInputHandler = currentStackedCharacter.GetComponent<StackedInputHandler>();

        // Configure the stacked controller with player info
        stackedController.Initialize(bottomPlayer, topPlayer);

        stackActive = true;
        Debug.Log("Stack formed successfully!");
    }

    public void DisableComponents(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        playerInputManager.splitScreen = false;

       bottomPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       bottomPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;
       //bottomPlayer.playerObject.GetComponent<PlayerInput>().enabled = false;

       topPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       topPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;
       //topPlayer.playerObject.GetComponent<PlayerInput>().enabled = false;
    }

    public void Unstack()
    {
        if (!stackActive || currentStackedCharacter == null)
            return;

        // Get positions for unstacking
        Vector3 bottomPos = currentStackedCharacter.transform.position;
        Vector3 topPos = bottomPos + Vector3.up * stackHeightOffset;

        // Reactivate individual players
        foreach (var info in activePlayers)
        {
            info.playerObject.SetActive(true);

            if (info.isTop)
                info.playerObject.transform.position = topPos;
            else
                info.playerObject.transform.position = bottomPos;
        }

        // Destroy stacked character
        Destroy(currentStackedCharacter);
        stackActive = false;
        Debug.Log("Stack broken");
    }
}