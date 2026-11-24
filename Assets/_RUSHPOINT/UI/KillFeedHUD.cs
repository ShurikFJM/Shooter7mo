using Unity.Netcode;
using UnityEngine;

public class KillFeedHUD : MonoBehaviour
{
    public static KillFeedHUD Instance { get; private set; }
    public GameObject[] killImages;
    public int maxKillsDisplay = 5;
    public AudioSource audioSource;
    public AudioClip killSound;
    public AudioClip headshotKillSound;

    private ScoreboardManager _boundScoreboard;
    private RoundManager _boundRoundManager;

    private int _roundKills = 0;
    private int _lastKnownTotalKills = -1; 

    public int RoundKills => _roundKills;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ResetKills();
        TryBindToManagers();
    }

    private void OnDestroy()
    {
        UnbindFromManagers();
    }

    private void Update()
    {
        if (_boundScoreboard != ScoreboardManager.Instance || _boundRoundManager != RoundManager.Instance)
        {
            TryBindToManagers();
        }
    }

    private void TryBindToManagers()
    {
        ScoreboardManager scoreboard = ScoreboardManager.Instance;
        if (_boundScoreboard != scoreboard && scoreboard != null && scoreboard.IsSpawned && scoreboard.PlayerStats != null)
        {
            UnbindScoreboard();
            _boundScoreboard = scoreboard;
            _boundScoreboard.PlayerStats.OnListChanged += HandleScoreboardChanged;
            _boundScoreboard.OnStatsReady += SyncInitialKills;
            SyncInitialKills();
        }
        RoundManager roundManager = RoundManager.Instance;
        if (_boundRoundManager != roundManager && roundManager != null)
        {
            UnbindRoundManager();
            _boundRoundManager = roundManager;
            _boundRoundManager.CurrentPhase.OnValueChanged += HandlePhaseChanged;
        }
    }

    private void UnbindFromManagers()
    {
        UnbindScoreboard();
        UnbindRoundManager();
    }

    private void UnbindScoreboard()
    {
        if (_boundScoreboard != null)
        {
            if (_boundScoreboard.PlayerStats != null)
                _boundScoreboard.PlayerStats.OnListChanged -= HandleScoreboardChanged;
            _boundScoreboard.OnStatsReady -= SyncInitialKills;
        }
        _boundScoreboard = null;
    }

    private void UnbindRoundManager()
    {
        if (_boundRoundManager != null)
        {
            _boundRoundManager.CurrentPhase.OnValueChanged -= HandlePhaseChanged;
        }
        _boundRoundManager = null;
    }

    private void SyncInitialKills()
    {
        if (TryGetLocalPlayerKills(out int currentKills))
        {
            _lastKnownTotalKills = currentKills;
        }
    }

    private void HandleScoreboardChanged(NetworkListEvent<PlayerScoreStats> changeEvent)
    {
        if (!TryGetLocalPlayerKills(out int currentKills)) return;
        if (_lastKnownTotalKills < 0)
        {
            _lastKnownTotalKills = currentKills;
            return;
        }
        if (currentKills > _lastKnownTotalKills)
        {
            int killsGained = currentKills - _lastKnownTotalKills;
            _lastKnownTotalKills = currentKills;

            for (int i = 0; i < killsGained; i++)
            {
                TriggerKillNotification(false);
            }
        }
        else if (currentKills < _lastKnownTotalKills)
        {
            _lastKnownTotalKills = currentKills;
        }
    }

    private bool TryGetLocalPlayerKills(out int kills)
    {
        kills = 0;
        if (_boundScoreboard == null || _boundScoreboard.PlayerStats == null) return false;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return false;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < _boundScoreboard.PlayerStats.Count; i++)
        {
            PlayerScoreStats stats = _boundScoreboard.PlayerStats[i];
            if (stats.ClientId == localClientId)
            {
                kills = stats.Kills;
                return true;
            }
        }

        return false;
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase currentPhase)
    {
        if (currentPhase == RoundPhase.Warmup || currentPhase == RoundPhase.InProgress)
        {
            ResetKills();
        }
    }

    public void TriggerKillNotification(bool isHeadshot = false)
    {
        _roundKills = Mathf.Min(_roundKills + 1, maxKillsDisplay);

        UpdateKillImageDisplay();

        AudioClip clipToPlay = (isHeadshot && headshotKillSound != null) ? headshotKillSound : killSound;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void UpdateKillImageDisplay()
    {
        if (killImages == null || killImages.Length == 0) return;

        int targetIndex = Mathf.Clamp(_roundKills, 0, killImages.Length - 1);

        for (int i = 0; i < killImages.Length; i++)
        {
            if (killImages[i] != null)
                killImages[i].SetActive(false);
        }

        GameObject activeKillObject = killImages[targetIndex];
        if (activeKillObject != null)
        {
            activeKillObject.SetActive(true);
            RestartObjectAnimation(activeKillObject);
        }
    }

    private void RestartObjectAnimation(GameObject obj)
    {
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.Play(0, 0, 0f);
        }

        Animation legacyAnim = obj.GetComponent<Animation>();
        if (legacyAnim != null)
        {
            legacyAnim.Stop();
            legacyAnim.Rewind();
            legacyAnim.Play();
        }
    }

    public void ResetKills()
    {
        _roundKills = 0;
        UpdateKillImageDisplay();
    }
}