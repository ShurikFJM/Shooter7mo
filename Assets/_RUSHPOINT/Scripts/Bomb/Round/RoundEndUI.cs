using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoundEndUI : MonoBehaviour
{
    [Header("Round End")]
    [SerializeField] private GameObject _roundEndRoot;
    [SerializeField] private TMP_Text _winnerText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _nextRoundCountdownText;

    [Header("Match End")]
    [SerializeField] private GameObject _matchEndRoot;
    [SerializeField] private TMP_Text _matchWinnerText;

    [Header("Team Colors")]
    [SerializeField]
    private Color _redColor =
        new Color(0.85f, 0.15f, 0.15f);

    [SerializeField]
    private Color _blueColor =
        new Color(0.15f, 0.4f, 0.85f);

    private void Start()
    {
        HideAll();
    }

    private void Update()
    {
        if (RoundManager.Instance == null ||
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsClient)
        {
            HideAll();
            return;
        }

        RoundPhase currentPhase =
            RoundManager.Instance.CurrentPhase.Value;

        switch (currentPhase)
        {
            case RoundPhase.RoundEnd:
                ShowRoundEnd();
                break;

            case RoundPhase.MatchEnd:
                ShowMatchEnd();
                break;

            default:
                HideAll();
                break;
        }
    }

    private void ShowRoundEnd()
    {
        if (_matchEndRoot != null)
        {
            _matchEndRoot.SetActive(false);
        }

        if (_roundEndRoot != null)
        {
            _roundEndRoot.SetActive(true);
        }

        RoundManager roundManager =
            RoundManager.Instance;

        Team winner =
            roundManager.LastRoundWinner.Value;

        if (_winnerText != null)
        {
            if (winner == Team.Red)
            {
                _winnerText.text =
                    "¡TERRORISTAS GANAN LA RONDA!";

                _winnerText.color =
                    _redColor;
            }
            else if (winner == Team.Blue)
            {
                _winnerText.text =
                    "¡CONTRATERRORISTAS GANAN LA RONDA!";

                _winnerText.color =
                    _blueColor;
            }
            else
            {
                _winnerText.text =
                    "RONDA TERMINADA";
            }
        }

        if (_scoreText != null)
        {
            _scoreText.text =
                $"{roundManager.RedScore.Value} - " +
                $"{roundManager.BlueScore.Value}";
        }

        if (_nextRoundCountdownText != null)
        {
            double elapsedTime =
                NetworkManager.Singleton.ServerTime.Time -
                roundManager.PhaseStartServerTime.Value;

            float remainingTime =
                Mathf.Max(
                    roundManager.RoundEndDisplayDuration -
                    (float)elapsedTime,
                    0f
                );

            _nextRoundCountdownText.text =
                $"Siguiente ronda en " +
                $"{Mathf.CeilToInt(remainingTime)}...";
        }
    }

    private void ShowMatchEnd()
    {
        if (_roundEndRoot != null)
        {
            _roundEndRoot.SetActive(false);
        }

        if (_matchEndRoot != null)
        {
            _matchEndRoot.SetActive(true);
        }

        RoundManager roundManager =
            RoundManager.Instance;

        bool redWon =
            roundManager.RedScore.Value >
            roundManager.BlueScore.Value;

        if (_matchWinnerText != null)
        {
            if (redWon)
            {
                _matchWinnerText.text =
                    "¡TERRORISTAS GANAN LA PARTIDA!";

                _matchWinnerText.color =
                    _redColor;
            }
            else
            {
                _matchWinnerText.text =
                    "¡CONTRATERRORISTAS GANAN LA PARTIDA!";

                _matchWinnerText.color =
                    _blueColor;
            }
        }
    }

    private void HideAll()
    {
        if (_roundEndRoot != null)
        {
            _roundEndRoot.SetActive(false);
        }

        if (_matchEndRoot != null)
        {
            _matchEndRoot.SetActive(false);
        }
    }
}