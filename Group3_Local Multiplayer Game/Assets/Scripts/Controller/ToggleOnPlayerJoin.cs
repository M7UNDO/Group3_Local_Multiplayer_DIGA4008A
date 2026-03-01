using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleOnPlayerJoin : MonoBehaviour
{
    [SerializeField] private PlayerInputManager playerInputManager;
    public GameObject[] temporaryObjects;

    private void Awake()
    {
        playerInputManager = FindFirstObjectByType<PlayerInputManager>();
        if(playerInputManager != null)
        {
            print("Player Input manager Found");
        }
    }

    private void OnEnable()
    {
        playerInputManager.onPlayerJoined += ToggleThis;
    }

    private void OnDisable()
    {
        playerInputManager.onPlayerJoined -= ToggleThis;
    }

    public void ToggleThis(PlayerInput player)
    {
        foreach (var obj in temporaryObjects)
        {
            obj.SetActive(false);
        }
    }
}