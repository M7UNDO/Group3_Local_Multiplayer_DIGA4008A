using UnityEngine;

public class Candyspawner : MonoBehaviour
{
    [Header("Spawn locations")]
    [SerializeField] private GameObject SP1; //SP = Spawn Point
    [SerializeField] private GameObject SP2;
    [SerializeField] private GameObject SP3;
    [SerializeField] private GameObject SP4;
    [SerializeField] private GameObject SP5;
    [SerializeField] private GameObject SP6;
    [SerializeField] private GameObject SP7;

    [SerializeField] private float SPnum1;
    [SerializeField] private float SPnum2;
    [SerializeField] private float SPnum3;
    [SerializeField] private float SPnum4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SPnum1 = Mathf.Round(Random.Range(0f, 6f)) ; //Assigning random numbers for spawn point
        SPnum2 = Mathf.Round(Random.Range(0f, 6f));
        SPnum3 = Mathf.Round(Random.Range(0f, 6f));
        SPnum4 = Mathf.Round(Random.Range(0f, 6f));

        SP1.SetActive(false); //Setting every Spawm point to inactive/false
        SP2.SetActive(false);
        SP3.SetActive(false);
        SP4.SetActive(false);
        SP5.SetActive(false);
        SP6.SetActive(false);
        SP7.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (SPnum1 == 0 || SPnum2 == 0 || SPnum3 == 0 || SPnum4 == 0)  //Assigning the Spawn points
        {
            SP1.SetActive(true);
        }

        if (SPnum1 == 1 || SPnum2 == 1 || SPnum3 == 1 || SPnum4 == 1)
        { 
            SP2.SetActive(true);
        }

        if(SPnum1 == 2 || SPnum2 == 2 || SPnum3 == 2 || SPnum4 == 2)  
        {
            SP3.SetActive(true);
        }

        if (SPnum1 == 3 || SPnum2 == 3 || SPnum3 == 3 || SPnum4 == 3)
        {
            SP4.SetActive(true);
        }

        if(SPnum1 == 4 || SPnum2 == 4 || SPnum3 == 4 || SPnum4 == 4)  
        {
            SP5.SetActive(true);
        }

        if (SPnum1 == 5 || SPnum2 == 5 || SPnum3 == 5 || SPnum4 == 5)
        {
            SP6.SetActive(true);
        }

        if (SPnum1 == 6 || SPnum2 == 6 || SPnum3 == 6 || SPnum4 == 6)
        {
            SP7.SetActive(true);
        }
    }
}
