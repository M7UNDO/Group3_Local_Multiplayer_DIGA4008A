using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrabbable : MonoBehaviour
{
    //[Header("Settings")]
    //public float rotationSpeed = 100f;

    [Header("UI")]
    public GameObject pickupIcon; // Reference to a UI icon that floats above the item

    private void Start()
    {
        // Hide the icon at start
        if (pickupIcon != null)
            pickupIcon.SetActive(false);
    }

    private void Update()
    {
        // Make the item float/rotate slightly to look attractive
        //transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void ShowIcon()
    {
        if (pickupIcon != null)
            pickupIcon.SetActive(true);
    }

    public void HideIcon()
    {
        if (pickupIcon != null)
            pickupIcon.SetActive(false);
    }

    public void DestroySelf()
    {
        // Play a pickup sound/effect here if desired
        Debug.Log($"Item {gameObject.name} picked up!");

        // Hide icon before destroying
        HideIcon();

        // Destroy the item
        Destroy(gameObject);
    }

    // Optional: Called when player leaves range
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HideIcon();
        }
    }
}