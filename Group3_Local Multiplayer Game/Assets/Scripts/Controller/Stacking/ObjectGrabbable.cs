using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();

        // store original spawn
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void Grab(Transform grabPoint)
    {
        objectGrabPointTransform = grabPoint;
        objectRigidbody.useGravity = false;
    }

    public void Drop()
    {
        objectGrabPointTransform = null;
        objectRigidbody.useGravity = true;
    }

    public void ResetToSpawn()
    {

        transform.position = startPosition;
        transform.rotation = startRotation;

        objectRigidbody.useGravity = false;
        print("Reset");

    }

    private void FixedUpdate()
    {
        if (objectGrabPointTransform != null)
        {
            float lerpSpeed = 10f;

            Vector3 newPosition = Vector3.Lerp(
                transform.position,
                objectGrabPointTransform.position,
                Time.deltaTime * lerpSpeed
            );

            objectRigidbody.MovePosition(newPosition);
        }
    }
}