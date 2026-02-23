using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public List<PlayerInput> players = new List<PlayerInput>();
    [SerializeField] private List<Transform> startingPoints;
    [SerializeField] private PlayerInputManager playerInputManager;

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
        //Debug.Log("AddPlayer fired for playerIndex: " + player.playerIndex);

        players.Add(player);

        Transform playerTransform = player.transform;
        Transform spawnPoint = startingPoints[players.Count - 1];

        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;


        int channel = player.playerIndex;

        CinemachineCamera cineCam =
            playerTransform.GetComponentInChildren<CinemachineCamera>();

        if (cineCam != null)
            cineCam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << channel);

        // Assign Channel Mask to this player's CinemachineBrain
        CinemachineBrain brain =
            playerTransform.GetComponentInChildren<Camera>()
                           .GetComponent<CinemachineBrain>();

        if (brain != null)
            brain.ChannelMask = (Unity.Cinemachine.OutputChannels)(1 << channel);

        // Enable only one audio listener
        playerTransform.GetComponentInChildren<AudioListener>().enabled = players.Count == 1;
    }
}