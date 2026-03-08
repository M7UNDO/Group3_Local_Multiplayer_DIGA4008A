using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public bool toggle;
    public string promptMessage;
    [SerializeField] private Animator animator;

    [Header("SFX")]

    public AudioSource doorOpenSFX;
    public AudioSource doorCloseSFX;

    void Start()
    {
        if (toggle)
        {
            promptMessage = "Close";
        }
        else
        {
            promptMessage = "Open";
        }

        print("Door Active");
    }


    public void Interact()
    {
        toggle = !toggle;
        if (toggle)
        {
            animator.ResetTrigger("close");
            if (doorOpenSFX != null)
            {
                doorOpenSFX.Play();
            }

            animator.SetTrigger("open");
            promptMessage = "Close";

            if (transform.gameObject.GetComponent<Outline>().enabled == true)
            {
                transform.gameObject.GetComponent<Outline>().enabled = false;
            }


        }
        else if (!toggle)
        {
            animator.ResetTrigger("open");
            if (doorCloseSFX != null)
            {
                doorCloseSFX.Play();
            }

            animator.SetTrigger("close");
            promptMessage = "Open";


        }

        Debug.Log(toggle ? "Door opens" : "Door closes");
    }
}
