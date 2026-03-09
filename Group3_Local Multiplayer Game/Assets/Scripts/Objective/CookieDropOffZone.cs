using UnityEngine;

public class CookieDropZone : MonoBehaviour
{
    private bool dropZoneActive = false;

    public void ActivateDropZone()
    {
        dropZoneActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!dropZoneActive) return;

        ObjectGrabbable cookie = other.GetComponent<ObjectGrabbable>();

        if (cookie != null)
        {
            ObjectiveManager.Instance.DeliverCookie();
            Destroy(cookie.gameObject);
        }
    }
}