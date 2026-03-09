using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerConfigurationManager : MonoBehaviour
{
    private List<PlayerConfiguration> playerConfigs = new List<PlayerConfiguration>();

    [SerializeField] private int maxPlayers = 2;

    public static PlayerConfigurationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HandlePlayerJoin(PlayerInput pi)
    {
        Debug.Log($"Player joined - PlayerIndex: {pi.playerIndex}, Device: {pi.devices.FirstOrDefault()}");

        pi.transform.SetParent(transform);

        if (!playerConfigs.Any(p => p.PlayerIndex == pi.playerIndex))
        {
            playerConfigs.Add(new PlayerConfiguration(pi));
            Debug.Log($"Added player config. Total configs: {playerConfigs.Count}");
        }
    }

    public List<PlayerConfiguration> GetPlayerConfigs()
    {
        return playerConfigs;
    }

    public void ReadyPlayer(int playerIndex)
    {
        var playerConfig = playerConfigs.FirstOrDefault(p => p.PlayerIndex == playerIndex);

        if (playerConfig != null)
        {
            playerConfig.IsReady = true;
            Debug.Log($"Player {playerIndex} ready - Config at index {playerConfigs.IndexOf(playerConfig)}");

            if (playerConfigs.Count == maxPlayers &&
                playerConfigs.All(p => p.IsReady))
            {
                SceneManager.LoadScene("GameplayScene");
            }
        }
    }
}