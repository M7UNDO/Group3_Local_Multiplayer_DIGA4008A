using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public List<PlayerInput> players = new List<PlayerInput>();
    [SerializeField] private List<Transform> startingPoints;
    [SerializeField] private PlayerInputManager playerInputManager;
    public StackManager stackManager;

    private void Awake()
    {
        playerInputManager = FindFirstObjectByType<PlayerInputManager>();
    }

    private void OnEnable()
    {
        //Debug.Log("Subscribing to onPlayerJoined...");
        playerInputManager.onPlayerJoined += AddPlayer;
    }

    private void OnDisable()
    {
        //Debug.Log("Unsubscribing from onPlayerJoined...");
        playerInputManager.onPlayerJoined -= AddPlayer;
    }

    public void AddPlayer(PlayerInput player)
    {
        players.Add(player);

        // SAFETY CHECK: Make sure we have enough starting points
        if (startingPoints == null || startingPoints.Count == 0)
        {
            Debug.LogError("No starting points assigned in PlayerManager!");
            return;
        }

        // Get the spawn point index (use modulo to cycle through points if needed)
        int spawnIndex = (players.Count - 1) % startingPoints.Count;
        Transform spawnPoint = startingPoints[spawnIndex];

        Transform playerTransform = player.transform;
        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;

        int channel = player.playerIndex;

        // Setup Cinemachine
        CinemachineCamera cineCam = playerTransform.GetComponentInChildren<CinemachineCamera>();
        if (cineCam != null)
            cineCam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << channel);

        // Setup CinemachineBrain
        Camera playerCamera = playerTransform.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            CinemachineBrain brain = playerCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
                brain.ChannelMask = (Unity.Cinemachine.OutputChannels)(1 << channel);
        }

        // Handle AudioListener
        AudioListener listener = playerTransform.GetComponentInChildren<AudioListener>();
        if (listener != null)
            listener.enabled = players.Count == 1; // Only first player has audio

        // Register player with StackManager
        if (stackManager != null)
        {
            stackManager.RegisterPlayer(player.gameObject, player.playerIndex);
        }

        Debug.Log($"Player {player.playerIndex} spawned at position {spawnIndex}");
    }

    public List<GameObject> GetAllActivePlayers()
    {
        List<GameObject> activePlayers = new List<GameObject>();
        foreach (var player in players)
        {
            if (player.gameObject.activeInHierarchy)
                activePlayers.Add(player.gameObject);
        }
        return activePlayers;
    }

    // Helper method to get player by index
    public GameObject GetPlayerByIndex(int index)
    {
        if (index >= 0 && index < players.Count)
            return players[index].gameObject;
        return null;
    }
}