using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Banner de fin de ronda / fin de partida. No es un NetworkBehaviour: lee las
/// NetworkVariables de RoundManager. Colocar en el Canvas de pantalla.
/// </summary>
public class RoundEndUI : MonoBehaviour
{
    [SerializeField] private GameObject _roundEndRoot;
    [SerializeField] private TMP_Text _winnerText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _nextRoundCountdownText;

    [SerializeField] private GameObject _matchEndRoot;
    [SerializeField] private TMP_Text _matchWinnerText;

    [SerializeField] private Color _redColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color _blueColor = new Color(0.15f, 0.4f, 0.85f);

    private void Update()
    {
        if (RoundManager.Instance == null || NetworkManager.Singleton == null)
        {
            HideAll();
            return;
        }

        RoundPhase phase = RoundManager.Instance.CurrentPhase.Value;

        if (phase == RoundPhase.RoundEnd)
        {
            ShowRoundEnd();
        }
        else if (phase == RoundPhase.MatchEnd)
        {
            ShowMatchEnd();
        }
        else
        {
            HideAll();
        }
    }

    private void ShowRoundEnd()
    {
        if (_matchEndRoot != null) _matchEndRoot.SetActive(false);
        if (_roundEndRoot != null) _roundEndRoot.SetActive(true);

        Team winner = RoundManager.Instance.LastRoundWinner.Value;

        if (_winnerText != null)
        {
            _winnerText.text = winner == Team.Red ? "¡Terroristas ganan la ronda!" : "¡Contraterroristas ganan la ronda!";
            _winnerText.color = winner == Team.Red ? _redColor : _blueColor;
        }

        if (_scoreText != null)
        {
            _scoreText.text = $"{RoundManager.Instance.RedScore.Value} - {RoundManager.Instance.BlueScore.Value}";
        }

        if (_nextRoundCountdownText != null)
        {
            double elapsed = NetworkManager.Singleton.ServerTime.Time - RoundManager.Instance.PhaseStartServerTime.Value;
            float remaining = Mathf.Max(0f, RoundManager.Instance.RoundEndDisplayDuration - (float)elapsed);
            _nextRoundCountdownText.text = $"Siguiente ronda en {Mathf.CeilToInt(remaining)}...";
        }
    }

    private void ShowMatchEnd()
    {
        if (_roundEndRoot != null) _roundEndRoot.SetActive(false);
        if (_matchEndRoot != null) _matchEndRoot.SetActive(true);

        bool redWon = RoundManager.Instance.RedScore.Value > RoundManager.Instance.BlueScore.Value;

        if (_matchWinnerText != null)
        {
            _matchWinnerText.text = redWon ? "¡Equipo Terrorista gana la partida!" : "¡Equipo Contraterrorista gana la partida!";
            _matchWinnerText.color = redWon ? _redColor : _blueColor;
        }
    }

    private void HideAll()
    {
        if (_roundEndRoot != null) _roundEndRoot.SetActive(false);
        if (_matchEndRoot != null) _matchEndRoot.SetActive(false);
    }
}
