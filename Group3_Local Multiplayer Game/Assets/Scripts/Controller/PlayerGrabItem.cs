using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrabItem : MonoBehaviour
{
    private ItemGrabbable itemGrabbable = null;
    private StackedController stackedController;


    [SerializeField] private Transform handIKTarget;
    private Animator animator;

    private void Start()
    {
        stackedController = GetComponent<StackedController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float grabRange = 1.5f;
        Collider[] colliderArray = Physics.OverlapSphere(transform.position, grabRange);

        foreach (Collider collider in colliderArray)
        {
            if (collider.gameObject.TryGetComponent<ItemGrabbable>(out ItemGrabbable grabbable))
            {
                itemGrabbable = grabbable;
                itemGrabbable.ShowIcon();
                break;
            }
        }

        if (stackedController != null && stackedController.GetGrabInput())
        {
            if (itemGrabbable != null)
            {
                handIKTarget.position = itemGrabbable.transform.position;
                animator.SetTrigger("GrabItem");
                //itemGrabbable.DestroySelf();
            }
        }
    }
}