using UnityEngine;
using UnityEngine.UI;

public class PingWheelUI : MonoBehaviour
{
    public static PingWheelUI Instance { get; private set; }

    [SerializeField] private GameObject _radialMenu;
    [SerializeField] private Image _dangerOption;
    [SerializeField] private Image _groupOption;

    public bool IsOpen => _radialMenu != null && _radialMenu.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_radialMenu != null)
        {
            _radialMenu.SetActive(false);
        }
    }

    public void Open()
    {
        if (_radialMenu != null)
        {
            _radialMenu.SetActive(true);
        }

        UpdateVisuals(PlayerPingSystem.PingType.Normal);
    }

    public void Close()
    {
        if (_radialMenu != null)
        {
            _radialMenu.SetActive(false);
        }
    }

    public void UpdateVisuals(PlayerPingSystem.PingType selectedType)
    {
        if (_dangerOption != null)
        {
            _dangerOption.color = (selectedType == PlayerPingSystem.PingType.Danger) ? Color.red : new Color(1f, 1f, 1f, 0.35f);
        }

        if (_groupOption != null)
        {
            _groupOption.color = (selectedType == PlayerPingSystem.PingType.Group) ? Color.cyan : new Color(1f, 1f, 1f, 0.35f);
        }
    }
}