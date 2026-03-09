using UnityEngine;

public class StoreEntranceTrigger : MonoBehaviour
{
    public CookieDropZone dropZone;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ObjectiveManager.Instance.EnterStore();

            dropZone.ActivateDropZone();
        }
    }
}