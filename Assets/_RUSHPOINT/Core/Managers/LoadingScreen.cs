using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [SerializeField] private GameObject _loadingScreenRoot;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _mapNameText;
    [SerializeField] private TMP_Text _modeNameText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.8f;

    [SerializeField] private TMP_Text[] _attackerSlots;
    [SerializeField] private TMP_Text[] _defenderSlots;

    private struct PlayerRosterEntry
    {
        public string Name;
        public Team Team;
        public PlayerRoleType Role;
    }

    private readonly List<PlayerRosterEntry> _cachedRoster = new List<PlayerRosterEntry>();
    private bool _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(false);
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetRosterData(string[] names, byte[] teams, byte[] roles)
    {
        _cachedRoster.Clear();

        if (names == null || teams == null || roles == null) return;

        int count = Mathf.Min(names.Length, Mathf.Min(teams.Length, roles.Length));
        for (int i = 0; i < count; i++)
        {
            _cachedRoster.Add(new PlayerRosterEntry
            {
                Name = names[i],
                Team = (Team)teams[i],
                Role = (PlayerRoleType)roles[i]
            });
        }

        RenderRoster();
    }

    private void RenderRoster()
    {
        ClearRosterSlots();

        int attackerIdx = 0;
        int defenderIdx = 0;

        for (int i = 0; i < _cachedRoster.Count; i++)
        {
            PlayerRosterEntry entry = _cachedRoster[i];
            string slotText = $"{entry.Name}\n<color=#FFCC00>{entry.Role}</color>";

            if (entry.Team == Team.Red && _attackerSlots != null && attackerIdx < _attackerSlots.Length)
            {
                if (_attackerSlots[attackerIdx] != null)
                {
                    _attackerSlots[attackerIdx].text = slotText;
                    _attackerSlots[attackerIdx].gameObject.SetActive(true);
                }
                attackerIdx++;
            }
            else if (entry.Team == Team.Blue && _defenderSlots != null && defenderIdx < _defenderSlots.Length)
            {
                if (_defenderSlots[defenderIdx] != null)
                {
                    _defenderSlots[defenderIdx].text = slotText;
                    _defenderSlots[defenderIdx].gameObject.SetActive(true);
                }
                defenderIdx++;
            }
        }
    }

    private void ClearRosterSlots()
    {
        if (_attackerSlots != null)
        {
            for (int i = 0; i < _attackerSlots.Length; i++)
            {
                if (_attackerSlots[i] != null)
                {
                    _attackerSlots[i].text = string.Empty;
                    _attackerSlots[i].gameObject.SetActive(false);
                }
            }
        }

        if (_defenderSlots != null)
        {
            for (int i = 0; i < _defenderSlots.Length; i++)
            {
                if (_defenderSlots[i] != null)
                {
                    _defenderSlots[i].text = string.Empty;
                    _defenderSlots[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                ShowLoadingScreen();
                break;

            case SceneEventType.LoadEventCompleted:
                StartCoroutine(DismissLoadingScreenRoutine());

                if (NetworkManager.Singleton.IsServer && RoundManager.Instance != null)
                {
                    RoundManager.Instance.StartMatchServer();
                }
                break;

            case SceneEventType.SynchronizeComplete:
                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    StartCoroutine(DismissLoadingScreenRoutine());
                }
                break;
        }
    }

    private void ShowLoadingScreen()
    {
        _isLoading = true;

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(true);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_mapNameText != null) _mapNameText.text = "HEAVEN";
        if (_modeNameText != null) _modeNameText.text = "SEARCH & DESTROY";

        if (_progressBar != null) _progressBar.value = 0f;
        if (_progressText != null) _progressText.text = "0%";

        RenderRoster();

        StartCoroutine(SimulateProgressRoutine());
    }

    private IEnumerator SimulateProgressRoutine()
    {
        float progress = 0f;

        while (_isLoading && progress < 0.9f)
        {
            progress += Time.deltaTime * Random.Range(0.2f, 0.45f);
            UpdateProgressDisplay(progress);
            yield return null;
        }
    }

    private void UpdateProgressDisplay(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (_progressBar != null) _progressBar.value = clamped;
        if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }

    private IEnumerator DismissLoadingScreenRoutine()
    {
        _isLoading = false;
        UpdateProgressDisplay(1f);

        yield return new WaitForSeconds(0.5f);

        if (_canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }
        }

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(false);
        }
    }
}