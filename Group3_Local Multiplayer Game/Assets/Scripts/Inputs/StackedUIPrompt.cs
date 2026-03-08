using UnityEngine;
using UnityEngine.UI;

public class stackedUIPrompt : MonoBehaviour
{
    [SerializeField] private StackedDeviceDetector deviceDetector;

    [SerializeField] private Image promptImage;

    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite xboxSprite;
    [SerializeField] private Sprite playStationSprite;

    private void Start()
    {
        UpdateSprite(deviceDetector.CurrentDevice);
        deviceDetector.OnDeviceChanged += UpdateSprite;
    }

    private void OnDestroy()
    {
        deviceDetector.OnDeviceChanged -= UpdateSprite;
    }

    private void UpdateSprite(InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceType.KeyboardMouse:
                promptImage.sprite = keyboardSprite;
                break;

            case InputDeviceType.Xbox:
                promptImage.sprite = xboxSprite;
                break;

            case InputDeviceType.PlayStation:
                promptImage.sprite = playStationSprite;
                break;
        }
    }
}