/*using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnScript : MonoBehaviour
{
    public Transform[] SpawnPoints;
    private int _playerCount;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.position = SpawnPoints[_playerCount].transform.position;
        if(_playerCount == 0)
        {
            playerInput.GetComponent<ThirdPersonController>().SwitchOnCamera();
        }
        else if(_playerCount == 1)
        {
            playerInput.gameObject.GetComponentInChildren<AudioListener>().enabled = true;
        }
        else
        {
            playerInput.gameObject.GetComponentInChildren<AudioListener>().enabled = false;
        }

         _playerCount++;
    }

}*/
