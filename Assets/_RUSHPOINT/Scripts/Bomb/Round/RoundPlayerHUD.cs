using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundPlayerHUD : MonoBehaviour
{
    private const string TIME_FORMAT = "{0:00}:{1:00}";
    private const string TERRORISTS_WIN_TEXT = "ATTACKERS WIN";
    private const string COUNTER_TERRORISTS_WIN_TEXT = "DEFENDERS WIN";
    private const string ROUND_OVER_TEXT = "ROUND OVER";
    private const string TERRORISTS_MATCH_WIN_TEXT = "ATTACKERS WIN THE MATCH";
    private const string COUNTER_TERRORISTS_MATCH_WIN_TEXT = "DEFENDERS WIN THE MATCH";

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

    [SerializeField] private Image[] _redDiamondImages;
    [SerializeField] private Image[] _blueDiamondImages;
    [SerializeField] private Color _activeDiamondColor = Color.white;
    [SerializeField] private Color _inactiveDiamondColor = new Color(1f, 1f, 1f, 0.2f);

    private RoundManager _cachedRoundManager;

    private void Start()
    {
        HideResultUI();
        LocateRoundManager();
    }

    private void Update()
    {
        LocateRoundManager();

        if (_cachedRoundManager == null)
        {
            return;
        }

        UpdateTimer(_cachedRoundManager);
        UpdateScore(_cachedRoundManager);
        UpdateRoundNumber(_cachedRoundManager);
        UpdateResultUI(_cachedRoundManager);
    }

    private void LocateRoundManager()
    {
        if (_cachedRoundManager != null) return;

        if (RoundManager.Instance != null)
        {
            _cachedRoundManager = RoundManager.Instance;
            return;
        }

        _cachedRoundManager = FindAnyObjectByType<RoundManager>();
    }

    private void UpdateTimer(RoundManager roundManager)
    {
        if (_roundTimerText == null) return;

        int totalSeconds = Mathf.Max(roundManager.SyncedRoundTime, 0);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _roundTimerText.text = string.Format(TIME_FORMAT, minutes, seconds);
    }

    private void UpdateScore(RoundManager roundManager)
    {
        int redScore = roundManager.SyncedRedScore;
        int blueScore = roundManager.SyncedBlueScore;

        if (_terroristScoreText != null)
        {
            _terroristScoreText.text = redScore.ToString();
        }

        if (_counterTerroristScoreText != null)
        {
            _counterTerroristScoreText.text = blueScore.ToString();
        }

        RefreshDiamonds(_redDiamondImages, redScore);
        RefreshDiamonds(_blueDiamondImages, blueScore);
    }

    private void UpdateRoundNumber(RoundManager roundManager)
    {
        if (_roundNumberText == null) return;
        _roundNumberText.text = roundManager.SyncedRoundNumber.ToString();
    }

    private void RefreshDiamonds(Image[] diamondArray, int filledCount)
    {
        if (diamondArray == null) return;

        for (int i = 0; i < diamondArray.Length; i++)
        {
            if (diamondArray[i] != null)
            {
                diamondArray[i].color = (i < filledCount) ? _activeDiamondColor : _inactiveDiamondColor;
            }
        }
    }

    private void UpdateResultUI(RoundManager roundManager)
    {
        RoundPhase phase = roundManager.SyncedPhase;

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
        if (_matchResultRoot != null) _matchResultRoot.SetActive(false);
        if (_roundResultRoot != null) _roundResultRoot.SetActive(true);

        Team winner = roundManager.SyncedLastWinner;
        if (_roundWinnerText == null) return;

        if (winner == Team.Red)
        {
            _roundWinnerText.text = TERRORISTS_WIN_TEXT;
            _roundWinnerText.color = _terroristColor;
        }
        else if (winner == Team.Blue)
        {
            _roundWinnerText.text = COUNTER_TERRORISTS_WIN_TEXT;
            _roundWinnerText.color = _counterTerroristColor;
        }
        else
        {
            _roundWinnerText.text = ROUND_OVER_TEXT;
        }
    }

    private void ShowMatchResult(RoundManager roundManager)
    {
        if (_roundResultRoot != null) _roundResultRoot.SetActive(false);
        if (_matchResultRoot != null) _matchResultRoot.SetActive(true);

        if (_matchWinnerText == null) return;

        if (roundManager.SyncedRedScore > roundManager.SyncedBlueScore)
        {
            _matchWinnerText.text = TERRORISTS_MATCH_WIN_TEXT;
            _matchWinnerText.color = _terroristColor;
        }
        else
        {
            _matchWinnerText.text = COUNTER_TERRORISTS_MATCH_WIN_TEXT;
            _matchWinnerText.color = _counterTerroristColor;
        }
    }

    private void HideResultUI()
    {
        if (_roundResultRoot != null) _roundResultRoot.SetActive(false);
        if (_matchResultRoot != null) _matchResultRoot.SetActive(false);
    }
}