using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    public List<PlayerInput> players = new List<PlayerInput>();

    [SerializeField] private List<Transform> startingPoints;
    [SerializeField] private PlayerInputManager playerInputManager;
    public StackManager stackManager;
    public BackgroundMusicManager backgroundMusicManager;

    [Header("Join UI Settings")]
    public static int JoinedPlayerCount = 0;
    public static List<InputDevice> JoinedDevices = new List<InputDevice>();
    [SerializeField] private GameObject joinCanvas;
    [SerializeField] private TextMeshProUGUI p1Text;
    [SerializeField] private TextMeshProUGUI p2Text;
    [SerializeField] private TextMeshProUGUI statusPrompt;

    [Header("UI Visuals")]
    [SerializeField] private Color joinedColorP1 = Color.green;
    [SerializeField] private Color joinedColorP2 = Color.green;
    [SerializeField] private Color waitingColor = Color.gray;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    private PlayerControls playerControls;
    private bool gameStarted = false;
    private Vector3 originalPromptScale;

    public bool fromMain;

    private void Awake()
    {
        playerControls = new PlayerControls();
        if (JoinedPlayerCount > 0)
        {
            StartCoroutine(RespawnPlayers());
            return;
        }

        Time.timeScale = 0f;

        originalPromptScale = statusPrompt.transform.localScale;

        p1Text.text = "PLAYER 1: WAITING...";
        p1Text.color = waitingColor;
        p2Text.text = "PLAYER 2: WAITING...";
        p2Text.color = waitingColor;
        statusPrompt.text = "PRESS ANY BUTTON TO JOIN";

        
        StartCoroutine(PulsePromptRoutine());
    }


    private void OnEnable()
    {
        playerInputManager.onPlayerJoined += AddPlayer;
        playerControls.Player.Join.performed += HandleStartInput;
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerInputManager.onPlayerJoined -= AddPlayer;
        playerControls.Player.Join.performed -= HandleStartInput;
        playerControls.Player.Disable();
    }

    IEnumerator RespawnPlayers()
    {
        if (joinCanvas) joinCanvas.SetActive(false);

        yield return null;

        for (int i = 0; i < JoinedDevices.Count; i++)
        {
            playerInputManager.JoinPlayer(
                i,
                -1,
                null,
                JoinedDevices[i]
            );
        }

        yield return null;
    }

    public void AddPlayer(PlayerInput player)
    {
        players.Add(player);

        if (!JoinedDevices.Contains(player.devices[0]))
            JoinedDevices.Add(player.devices[0]);
        player.DeactivateInput();

        if (players.Count == 1)
        {
            p1Text.text = "PLAYER 1: JOINED!";
            p1Text.color = joinedColorP1;
            statusPrompt.text = "WAITING FOR PLAYER 2...";
        }
        else if (players.Count == 2)
        {
            p2Text.text = "PLAYER 2: JOINED!";
            p2Text.color = joinedColorP2;
            statusPrompt.text = "BOTH READY! PRESS START";
            playerInputManager.DisableJoining();
        }

        if (startingPoints == null || startingPoints.Count == 0)
        {
            Debug.LogError("No starting points assigned in PlayerManager!");
        }
        else
        {
            int spawnIndex = player.playerIndex % startingPoints.Count;
            Transform spawnPoint = startingPoints[spawnIndex];

            if (players.Count <= 2) // Only for first 2 players
                StartCoroutine(PlacePlayerNextFrame(player, spawnPoint));
        }


        SetupPlayerSystems(player);
    }

    private void HandleStartInput(InputAction.CallbackContext context)
    {
        if (players.Count == 2 && !gameStarted)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        Time.timeScale = 1f; // Unpause the game

        foreach (var player in players)
        {
            player.ActivateInput();
        }

        if (joinCanvas) joinCanvas.SetActive(false);
        backgroundMusicManager.PlayNextTrack();
        StopAllCoroutines();
    }

    private IEnumerator PulsePromptRoutine()
    {
        while (!gameStarted)
        {
            // Use unscaledTime to pulse even when timeScale is 0
            float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            statusPrompt.transform.localScale = originalPromptScale * scale;
            yield return null;
        }
    }

    private void SetupPlayerSystems(PlayerInput player)
    {
        int channel = player.playerIndex;
        var playerTransform = player.transform;
        CinemachineCamera cineCam = playerTransform.GetComponentInChildren<CinemachineCamera>();
        if (cineCam != null) cineCam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << channel);

        Camera playerCamera = playerTransform.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            CinemachineBrain brain = playerCamera.GetComponent<CinemachineBrain>();
            if (brain != null) brain.ChannelMask = (Unity.Cinemachine.OutputChannels)(1 << channel);
        }

        AudioListener listener = playerTransform.GetComponentInChildren<AudioListener>();
        if (listener != null)
        {
            int index = player.playerIndex;
            listener.enabled = (index == 0 || index == 2);
        }

        if (stackManager != null) stackManager.RegisterPlayer(player.gameObject, player.playerIndex);
    }

    private IEnumerator PlacePlayerNextFrame(PlayerInput player, Transform spawnPoint)
    {
        yield return null; // wait 1 frame

        Transform t = player.transform;

        // Disable CharacterController so it doesn't override the spawn position
        var cc = t.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        t.position = spawnPoint.position;
        t.rotation = spawnPoint.rotation;

        if (cc != null) cc.enabled = true;
    }
}
