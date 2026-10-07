using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoundPlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text _roundTimerText;
    [SerializeField] private TMP_Text _terroristScoreText;
    [SerializeField] private TMP_Text _counterTerroristScoreText;
    [SerializeField] private TMP_Text _roundNumberText;
    [SerializeField] private GameObject _roundResultRoot;
    [SerializeField] private TMP_Text _roundWinnerText;
    [SerializeField] private GameObject _matchResultRoot;
    [SerializeField] private TMP_Text _matchWinnerText;
    [SerializeField] private Color _terroristColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color _counterTerroristColor = new Color(0.15f, 0.4f, 0.85f);

    private void Start()
    {
        HideResultUI();
    }

    private void Update()
    {
        if (RoundManager.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            return;
        }

        RoundManager roundManager = RoundManager.Instance;

        UpdateTimer(roundManager);
        UpdateScore(roundManager);
        UpdateRoundNumber(roundManager);
        UpdateResultUI(roundManager);
    }

    private void UpdateTimer(RoundManager roundManager)
    {
        if (_roundTimerText == null) return;

        int totalSeconds = Mathf.Max(roundManager.CurrentRoundTime.Value, 0);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _roundTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScore(RoundManager roundManager)
    {
        if (_terroristScoreText != null)
        {
            _terroristScoreText.text = roundManager.RedScore.Value.ToString();
        }

        if (_counterTerroristScoreText != null)
        {
            _counterTerroristScoreText.text = roundManager.BlueScore.Value.ToString();
        }
    }

    private void UpdateRoundNumber(RoundManager roundManager)
    {
        if (_roundNumberText == null) return;

        _roundNumberText.text = $"ROUND {roundManager.RoundNumber.Value}";
    }

    private void UpdateResultUI(RoundManager roundManager)
    {
        RoundPhase phase = roundManager.CurrentPhase.Value;

        if (phase == RoundPhase.RoundEnd)
        {
            ShowRoundResult(roundManager);
            return;
        }

        if (phase == RoundPhase.MatchEnd)
        {
            ShowMatchResult(roundManager);
            return;
        }

        HideResultUI();
    }

    private void ShowRoundResult(RoundManager roundManager)
    {
        if (_matchResultRoot != null)
        {
            _matchResultRoot.SetActive(false);
        }

        if (_roundResultRoot != null)
        {
            _roundResultRoot.SetActive(true);
        }

        Team winner = roundManager.LastRoundWinner.Value;

        if (_roundWinnerText == null) return;

        if (winner == Team.Red)
        {
            _roundWinnerText.text = "TERRORISTS WIN";
            _roundWinnerText.color = _terroristColor;
        }
        else if (winner == Team.Blue)
        {
            _roundWinnerText.text = "COUNTER-TERRORISTS WIN";
            _roundWinnerText.color = _counterTerroristColor;
        }
        else
        {
            _roundWinnerText.text = "ROUND OVER";
        }
    }

    private void ShowMatchResult(RoundManager roundManager)
    {
        if (_roundResultRoot != null)
        {
            _roundResultRoot.SetActive(false);
        }

        if (_matchResultRoot != null)
        {
            _matchResultRoot.SetActive(true);
        }

        if (_matchWinnerText == null) return;

        if (roundManager.RedScore.Value > roundManager.BlueScore.Value)
        {
            _matchWinnerText.text = "TERRORISTS WIN THE MATCH";
            _matchWinnerText.color = _terroristColor;
        }
        else
        {
            _matchWinnerText.text = "COUNTER-TERRORISTS WIN THE MATCH";
            _matchWinnerText.color = _counterTerroristColor;
        }
    }

    private void HideResultUI()
    {
        if (_roundResultRoot != null)
        {
            _roundResultRoot.SetActive(false);
        }

        if (_matchResultRoot != null)
        {
            _matchResultRoot.SetActive(false);
        }
    }
}