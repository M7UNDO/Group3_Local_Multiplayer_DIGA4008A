using UnityEngine;
using UnityEngine.UI;

public class EnemySuspicionUI : MonoBehaviour
{
    public WaypointEnemy enemy;
    public Image suspicionFill;
    public GameObject suspicionFillBackground;
    public GameObject alertIcon;

    void Update()
    {
        if (enemy == null) return;

        float percent = enemy.suspicion / enemy.suspicionThreshold;

        suspicionFill.fillAmount = percent;


        if (percent > 0 && percent < 1)
        {
            suspicionFillBackground.SetActive(true);
            alertIcon.SetActive(false);
        }

        if (percent >= 1)
        {
            suspicionFillBackground.gameObject.SetActive(false);
            alertIcon.SetActive(true);
        }

        if (percent <= 0)
        {
            suspicionFillBackground.gameObject.SetActive(false);
            alertIcon.SetActive(false);
        }

    
        if (enemy.IsChasing())
        {
            suspicionFillBackground.gameObject.SetActive(false);
            alertIcon.SetActive(false);
        }
    }
}