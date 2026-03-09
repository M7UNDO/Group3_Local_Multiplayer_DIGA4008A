using UnityEngine.InputSystem;

public class PlayerConfiguration
{
    public PlayerConfiguration(PlayerInput input)
    {
        PlayerIndex = input.playerIndex;
        Input = input;
    }

    public PlayerInput Input { get; private set; }

    public int PlayerIndex { get; private set; }

    public bool IsReady { get; set; }
}