using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerGrabItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handIKTarget;       // IK target for the hand
    [SerializeField] private Transform handHoldPoint;      // Where items rest in the hand
    private Animator animator;
    public Rig rig;
    public float targetWeight;

    [Header("Settings")]
    public float reachSpeed = 6f;
    public float arcPullDuration = 0.35f;
    public float grabRange = 1.7f;

    private ItemGrabbable targetItem = null;
    private Transform heldItem = null;

    private bool isReaching = false;
    private bool isHolding = false;

    private StackedController stackedController;

    void Start()
    {
        animator = GetComponent<Animator>();
        stackedController = GetComponent<StackedController>();
    }

    void Update()
    {
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * 10f);
        DetectItem();

        if (stackedController.GetGrabInput() && targetItem != null && !isHolding && !isReaching)
        {
            StartGrab();
        }
    }

    void StartGrab()
    {
        isReaching = true;
        heldItem = targetItem.transform;

        animator.SetTrigger("GrabItem");
        StartCoroutine(ReachForItemRoutine());
    }

    IEnumerator ReachForItemRoutine()
    {
        while (Vector3.Distance(handIKTarget.position, heldItem.position) > 0.12f)
        {
            handIKTarget.position = Vector3.Lerp(
                handIKTarget.position,
                heldItem.position,
                Time.deltaTime * reachSpeed
            );

            yield return null;
        }

        // Begin arc pull into the hand
        StartCoroutine(ArcPullRoutine(heldItem, heldItem.position, handHoldPoint.position));
    }

    IEnumerator ArcPullRoutine(Transform item, Vector3 startPos, Vector3 endPos)
    {
        float time = 0f;

        Vector3 arcMidPoint = ArcPullUtility.CalculateArcMidPoint(startPos, endPos, 0.5f);

        while (time < arcPullDuration)
        {
            float t = time / arcPullDuration;

            Vector3 arcPos = ArcPullUtility.EvaluateBezierPoint(startPos, arcMidPoint, endPos, t);

            item.position = arcPos;

            time += Time.deltaTime;
            yield return null;
        }

        AttachItemToHand();
    }

    void AttachItemToHand()
    {
        isReaching = false;
        isHolding = true;

        // Parent item to hand
        heldItem.SetParent(handHoldPoint);
        heldItem.localPosition = Vector3.zero;
        heldItem.localRotation = Quaternion.identity;

        targetItem.HideIcon();
    }

    public void DropItem()
    {
        if (!isHolding) return;

        heldItem.SetParent(null);
        heldItem = null;
        isHolding = false;

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }

    void DetectItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRange);

        ItemGrabbable closest = null;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out ItemGrabbable item))
            {
                closest = item;
                break;
            }
        }

        // update target
        if (closest != targetItem)
        {
            if (targetItem != null)
                targetItem.HideIcon();
            targetWeight = 0f;

            if (closest != null)
                closest.ShowIcon();
            targetWeight = 1f;

            targetItem = closest;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        if (isReaching)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKPosition(AvatarIKGoal.RightHand, handIKTarget.position);
        }

        if (isHolding)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKPosition(AvatarIKGoal.RightHand, handHoldPoint.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, handHoldPoint.rotation);
        }
    }
}