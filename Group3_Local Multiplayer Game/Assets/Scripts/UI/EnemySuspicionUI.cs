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

        if (enemy.IsChasing())
        {
            suspicionFillBackground.SetActive(false);
            alertIcon.SetActive(false);
        }
        else if (percent >= 1f)
        {
            suspicionFillBackground.SetActive(false);
            alertIcon.SetActive(true);
        }
        else if (percent > 0f)
        {
            suspicionFillBackground.SetActive(true);
            alertIcon.SetActive(false);
        }
        else
        {
            suspicionFillBackground.SetActive(false);
            alertIcon.SetActive(false);
        }
    }
}