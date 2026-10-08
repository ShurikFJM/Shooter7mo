using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pon este componente en cada boton de ping (Normal / Danger / Group).
/// Al hacer click avisa al TutorialManager y el paso se da por completado.
/// </summary>
[RequireComponent(typeof(Button))]
public class TutorialPingButton : MonoBehaviour
{
    public enum PingKind
    {
        Normal = 0,
        Danger = 1,
        Group = 2
    }

    [SerializeField] private PingKind _pingKind = PingKind.Normal;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(NotifyTutorial);
    }

    private void NotifyTutorial()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnPingPlaced((int)_pingKind);
        }
    }
}
