using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public GameObject stackPrompt;

    public void StackPromptDisplay(bool isReadyToStack)
    {
        if(stackPrompt != null)
        {
            stackPrompt.SetActive(isReadyToStack);
        }    
    }
}
