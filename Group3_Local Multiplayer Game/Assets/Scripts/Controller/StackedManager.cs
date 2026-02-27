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
    private bool unstackInProgress = false;
    public PlayerInputManager playerInputManager;

    [Header("Debug")]
    public bool stackActive = false;

    private List<PlayerStackInfo> activePlayers = new List<PlayerStackInfo>();
    private GameObject currentStackedCharacter;
    private StackedController stackedController;
    private StackedInputHandler stackedInputHandler;


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

        // Determine who should be bottom and who should be top requesting player becomes the bottom
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

        // Checking player Distance here
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

        // Deactivate player Children GameObjects
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

        // Disable Components
        DisableComponents(bottomPlayer, topPlayer);

        // Create stacked character
        currentStackedCharacter = Instantiate(stackedCharacterPrefab, stackPosition, Quaternion.identity);
        stackedController = currentStackedCharacter.GetComponent<StackedController>();
        stackedInputHandler = currentStackedCharacter.GetComponent<StackedInputHandler>();

        stackedController.Initialize(bottomPlayer, topPlayer);

        stackActive = true;
        Debug.Log("Stack Formed successfully");
    }

    public void DisableComponents(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        playerInputManager.splitScreen = false;

       bottomPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       bottomPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;

       topPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       topPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;
    }

    public void EnableComponents(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        playerInputManager.splitScreen = true;

        bottomPlayer.playerObject.GetComponent<CharacterController>().enabled = true;
        bottomPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = true;

        topPlayer.playerObject.GetComponent<CharacterController>().enabled = true;
        topPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = true;
    }

    public void Unstack()
    {
        if (!stackActive || unstackInProgress)
            return;

        unstackInProgress = true;

        if (currentStackedCharacter == null)
        {
            stackActive = false;
            unstackInProgress = false;
            return;
        }

        // Safe check for destroyed object
        if (currentStackedCharacter == null)
        {
            stackActive = false;
            unstackInProgress = false;
            return;
        }

        // Base position of the stacked player
        Vector3 basePos = currentStackedCharacter.transform.position;

        float separation = 1.0f;

        // Bottom player to the left
        Vector3 bottomPos = basePos + Vector3.left * separation;

        // Top player to the right
        Vector3 topPos = basePos + Vector3.right * separation;

        foreach (var info in activePlayers)
        {
            if (info == null || info.playerObject == null)
                continue;

            if (info.isTop)
                info.playerObject.transform.position = topPos;
            else
                info.playerObject.transform.position = bottomPos;

            for (int i = 0; i < info.playerObject.transform.childCount; i++)
                info.playerObject.transform.GetChild(i).gameObject.SetActive(true);
        }

        EnableComponents(activePlayers[0], activePlayers[1]);

        if (currentStackedCharacter != null)
            Destroy(currentStackedCharacter);

        currentStackedCharacter = null;
        stackActive = false;

        unstackInProgress = false;

        Debug.Log("Unstacked");
    }
}