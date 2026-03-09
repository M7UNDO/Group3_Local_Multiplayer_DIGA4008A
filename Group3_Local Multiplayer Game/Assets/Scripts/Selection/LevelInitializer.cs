using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class LevelInitializer : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private StackManager stackManager;

    void Start()
    {
        var playerConfigs = PlayerConfigurationManager.Instance.GetPlayerConfigs();

        for (int i = 0; i < playerConfigs.Count; i++)
        {
            var config = playerConfigs[i];

            var player = Instantiate(
                playerPrefab,
                spawnPoints[i].position,
                spawnPoints[i].rotation
            );

            var pi = config.Input;

            pi.transform.SetParent(player.transform);

            SetupCamera(player, pi.playerIndex);

            stackManager.RegisterPlayer(player, pi.playerIndex);
        }
    }

    void SetupCamera(GameObject player, int index)
    {
        CinemachineCamera cineCam = player.GetComponentInChildren<CinemachineCamera>();

        if (cineCam != null)
            cineCam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << index);

        Camera cam = player.GetComponentInChildren<Camera>();

        if (cam != null)
        {
            var brain = cam.GetComponent<CinemachineBrain>();

            if (brain != null)
                brain.ChannelMask = (Unity.Cinemachine.OutputChannels)(1 << index);
        }
    }
}