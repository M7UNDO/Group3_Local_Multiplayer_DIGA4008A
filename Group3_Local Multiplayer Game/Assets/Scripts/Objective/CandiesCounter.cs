using System.Security.Cryptography;
using UnityEngine;

public class CandiesCounter : MonoBehaviour
{
    public Completion Candy;

    public void OnTriggerEnter(Collider other)
    {
        if (other)
        {
            Candy.Candies++;
            Destroy(gameObject);
            Debug.Log("Candies collected "+Candy.Candies);

        }
    }
}
