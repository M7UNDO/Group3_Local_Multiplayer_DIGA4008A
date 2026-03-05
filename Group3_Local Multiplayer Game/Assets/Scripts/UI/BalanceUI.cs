using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BalanceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image balanceMeter;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private GameObject warningIcon;
    [SerializeField] private TextMeshProUGUI warningTimerText;
    [SerializeField] private Image timerFill;

    [Header("Colors")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Controller Reference")]
    [SerializeField] private SpineBalanceController balanceController;

    private void Awake()
    {
        // Fallback in case not assigned in inspector
        if (balanceController == null)
            balanceController = FindFirstObjectByType<SpineBalanceController>();

        if (balanceController != null)
        {
            balanceController.OnBalanceChanged += UpdateUI;
            balanceController.OnEnterCriticalZone += HandleEnterCritical;
            balanceController.OnExitCriticalZone += HandleExitCritical;

            // ?? Force initial UI update immediately
            UpdateUI(balanceController.GetBalancePercentage());
        }

        if (warningIcon != null)
            warningIcon.SetActive(false);

        if (warningTimerText != null)
            warningTimerText.text = "";
    
    }

    private void Update()
    {
        if (balanceController == null) return;

        if (balanceController.IsInCriticalZone())
        {
            float timeLeft = balanceController.GetTimeUntilUnstack();
            float fillPercentage = balanceController.unstackDelay / timeLeft; 

            if (warningTimerText != null)
                warningTimerText.text = $"{timeLeft:F1}s";


            if(timerFill != null)
            {
                timerFill.enabled = true;
                timerFill.fillAmount = fillPercentage;
                //timerFill.color = Color.Lerp(safeColor, dangerColor, fillPercentage);
                //.color
            }
            
        }
        else
        {
            if (warningTimerText != null)
                warningTimerText.text = "";

            if (timerFill != null)
            {
                timerFill.enabled = false;
            }
        }
    }

    private void UpdateUI(float balancePercent)
    {
        balancePercent = Mathf.Clamp01(balancePercent);

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
            balanceText.text = $"{balancePercent * 100f:F0}%";
        }
    }

    private void HandleEnterCritical()
    {
        if (warningIcon != null)
            warningIcon.SetActive(true);
    }

    private void HandleExitCritical()
    {
        if (warningIcon != null)
            warningIcon.SetActive(false);
    }

    private void OnDestroy()
    {
        if (balanceController == null) return;

        balanceController.OnBalanceChanged -= UpdateUI;
        balanceController.OnEnterCriticalZone -= HandleEnterCritical;
        balanceController.OnExitCriticalZone -= HandleExitCritical;
    }
}