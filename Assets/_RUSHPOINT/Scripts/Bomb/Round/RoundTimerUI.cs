using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Cronómetro de ronda: cuenta regresiva del warmup y del tiempo límite para
/// plantar. Se oculta automáticamente en cuanto la bomba queda plantada (ahí
/// toma el relevo visual BombTimerUI) y durante fin de ronda / fin de partida.
/// Colocar en el mismo Canvas de pantalla que BombPromptUI y BombTimerUI.
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject _timerRoot;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _phaseLabelText;
    [SerializeField] private string _warmupLabel = "La ronda empieza en";
    [SerializeField] private string _inProgressLabel = "Tiempo restante";

    private Bomb _bomb;

    private void Update()
    {
        if (RoundManager.Instance == null || NetworkManager.Singleton == null)
        {
            Hide();
            return;
        }

        if (_bomb == null)
        {
            _bomb = FindFirstObjectByType<Bomb>();
        }

        RoundPhase phase = RoundManager.Instance.CurrentPhase.Value;

        if (phase == RoundPhase.Warmup)
        {
            ShowCountdown(_warmupLabel, RoundManager.Instance.WarmupDuration);
        }
        else if (phase == RoundPhase.InProgress && (_bomb == null || _bomb.State.Value != BombState.Planted))
        {
            ShowCountdown(_inProgressLabel, RoundManager.Instance.RoundTimeLimit);
        }
        else
        {
            Hide();
        }
    }

    private void ShowCountdown(string label, float phaseDuration)
    {
        double elapsed = NetworkManager.Singleton.ServerTime.Time - RoundManager.Instance.PhaseStartServerTime.Value;
        float remaining = Mathf.Max(0f, phaseDuration - (float)elapsed);

        if (_timerRoot != null) _timerRoot.SetActive(true);
        if (_phaseLabelText != null) _phaseLabelText.text = label;

        if (_timerText != null)
        {
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void Hide()
    {
        if (_timerRoot != null) _timerRoot.SetActive(false);
    }
}
