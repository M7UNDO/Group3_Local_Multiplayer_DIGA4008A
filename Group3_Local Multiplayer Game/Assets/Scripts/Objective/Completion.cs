using UnityEngine;

public class Completion : MonoBehaviour
{
    public int Candies = 0;
    public GameObject Endscreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Endscreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Candies >= 2)
        {
            Endscreen.SetActive(true);
        }
    }
}
