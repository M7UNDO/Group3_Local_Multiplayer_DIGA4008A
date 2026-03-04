using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BalanceUI : MonoBehaviour
{
    public Image balanceMeter;
    public TextMeshProUGUI balanceText;
    public GameObject warningIcon;
    public TextMeshProUGUI warningTimerText;

    public Color safeColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [SerializeField] private SpineBalanceController _balanceController;

    private void Awake()
    {
        _balanceController = FindFirstObjectByType<SpineBalanceController>();

        if (_balanceController != null)
        {
            _balanceController.OnBalanceChanged += UpdateUI;
            _balanceController.OnEnterCriticalZone += () => warningIcon.SetActive(true);
            _balanceController.OnExitCriticalZone += () => warningIcon.SetActive(false);
        }

        if (warningIcon != null) warningIcon.SetActive(false);
    }

    private void Update()
    {
        if (_balanceController != null && _balanceController.IsInCriticalZone())
        {
            float timeLeft = _balanceController.GetTimeUntilUnstack();
            if (warningTimerText != null)
            {
                warningTimerText.text = $"{timeLeft:F1}s";
            }
        }
    }

    private void UpdateUI(float balancePercent)
    {
        if (balanceMeter != null)
        {
            balanceMeter.fillAmount = balancePercent;

            if (balancePercent > 0.7f)
                balanceMeter.color = safeColor;
            else if (balancePercent > 0.4f)
                balanceMeter.color = warningColor;
            else
                balanceMeter.color = dangerColor;
        }

        if (balanceText != null)
        {
            balanceText.text = $"{balancePercent * 100:F0}%";
        }
    }

    private void OnDestroy()
    {
        if (_balanceController != null)
        {
            _balanceController.OnBalanceChanged -= UpdateUI;
        }
    }
}