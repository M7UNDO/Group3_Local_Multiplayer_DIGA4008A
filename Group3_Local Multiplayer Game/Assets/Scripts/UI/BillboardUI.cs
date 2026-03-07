using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.current == null)
        {
            print("No camera");
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.current.transform.position
        );
    }
}