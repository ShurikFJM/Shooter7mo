using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject _scoreboardRootPanel;
    [SerializeField] private ScoreboardSlotUI[] _attackerSlots;
    [SerializeField] private ScoreboardSlotUI[] _defenderSlots;
    [SerializeField] private TMP_Text _roundTimerText;
    [SerializeField] private TMP_Text _attackerScoreText;
    [SerializeField] private TMP_Text _defenderScoreText;
    [SerializeField] private TMP_Text _roundNumberText;
    [SerializeField] private bool _debugLogs = true;
    [SerializeField] private bool _debugOverlay = false;

    private struct ScoreRow
    {
        public ulong ClientId;
        public string Name;
        public Team Team;
        public int Kills;
        public int Assists;
        public int Deaths;
    }

    private ScoreboardManager _boundManager;
    private RoundManager _cachedRoundManager;
    private readonly List<ScoreRow> _rows = new List<ScoreRow>();
    private const string TIME_FORMAT = "{0:00}:{1:00}";

    private void Start()
    {
        if (_scoreboardRootPanel != null && transform.IsChildOf(_scoreboardRootPanel.transform))
        {
            Debug.LogError("[ScoreboardUI] Este script está dentro de _scoreboardRootPanel. Muévelo a un objeto que siempre esté activo (Canvas).", this);
        }

        if (_scoreboardRootPanel != null) _scoreboardRootPanel.SetActive(false);

        ValidateSlots();
        TryBindToManager();
        RefreshScoreboard();
    }

    private void OnDestroy()
    {
        UnbindFromManager();
    }

    private void Update()
    {
        if (_boundManager != ScoreboardManager.Instance)
        {
            UnbindFromManager();
            TryBindToManager();
        }

        if (_scoreboardRootPanel == null) return;

        bool isScoreboardRequested = false;

        if (InputManager.Instance != null)
        {
            isScoreboardRequested = InputManager.Instance.IsScoreboardPressed;
        }
        else
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.isPressed) isScoreboardRequested = true;
            if (Gamepad.current != null && Gamepad.current.selectButton.isPressed) isScoreboardRequested = true;
        }

        if (_scoreboardRootPanel.activeSelf != isScoreboardRequested)
        {
            _scoreboardRootPanel.SetActive(isScoreboardRequested);
            if (isScoreboardRequested) RefreshScoreboard();
        }

        if (isScoreboardRequested) UpdateRoundInfo();
    }

    private void TryBindToManager()
    {
        ScoreboardManager manager = ScoreboardManager.Instance;
        if (manager == null || !manager.IsSpawned || manager.PlayerStats == null) return;

        _boundManager = manager;
        manager.PlayerStats.OnListChanged += HandleScoreboardChanged;
        manager.OnStatsReady += RefreshScoreboard;

        if (_debugLogs) Debug.Log($"[ScoreboardUI] Enlazado al ScoreboardManager | IsSpawned={manager.IsSpawned} | jugadores={manager.PlayerStats.Count}");
        RefreshScoreboard();
    }

    private void UnbindFromManager()
    {
        if (_boundManager != null)
        {
            _boundManager.PlayerStats.OnListChanged -= HandleScoreboardChanged;
            _boundManager.OnStatsReady -= RefreshScoreboard;
        }
        _boundManager = null;
    }

    private void HandleScoreboardChanged(NetworkListEvent<PlayerScoreStats> changeEvent)
    {
        RefreshScoreboard();
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

    private void UpdateRoundInfo()
    {
        LocateRoundManager();

        if (_cachedRoundManager == null) return;

        if (_roundTimerText != null)
        {
            int totalSeconds = Mathf.Max(_cachedRoundManager.SyncedRoundTime, 0);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _roundTimerText.text = string.Format(TIME_FORMAT, minutes, seconds);
        }
        if (_attackerScoreText != null)
        {
            _attackerScoreText.text = _cachedRoundManager.SyncedRedScore.ToString();
        }
        if (_defenderScoreText != null)
        {
            _defenderScoreText.text = _cachedRoundManager.SyncedBlueScore.ToString();
        }
        if (_roundNumberText != null)
        {
            _roundNumberText.text = _cachedRoundManager.SyncedRoundNumber.ToString();
        }
    }

    private void BuildRows()
    {
        _rows.Clear();

        if (MatchDataBridge.PlayersCache != null)
        {
            foreach (var kvp in MatchDataBridge.PlayersCache)
            {
                _rows.Add(new ScoreRow
                {
                    ClientId = kvp.Key,
                    Name = kvp.Value.Name,
                    Team = kvp.Value.SelectedTeam
                });
            }
        }

        ScoreboardManager manager = ScoreboardManager.Instance;
        if (manager != null && manager.PlayerStats != null)
        {
            for (int i = 0; i < manager.PlayerStats.Count; i++)
            {
                PlayerScoreStats s = manager.PlayerStats[i];
                var row = new ScoreRow
                {
                    ClientId = s.ClientId,
                    Name = s.PlayerName.ToString(),
                    Team = s.PlayerTeam,
                    Kills = s.Kills,
                    Assists = s.Assists,
                    Deaths = s.Deaths
                };

                int existing = _rows.FindIndex(r => r.ClientId == s.ClientId);
                if (existing >= 0) _rows[existing] = row;
                else _rows.Add(row);
            }
        }

        _rows.Sort((a, b) =>
        {
            int byKills = b.Kills.CompareTo(a.Kills);
            return byKills != 0 ? byKills : a.Deaths.CompareTo(b.Deaths);
        });
    }

    private void RefreshScoreboard()
    {
        BuildRows();
        HideSlots(_attackerSlots);
        HideSlots(_defenderSlots);

        int attackerIndex = 0;
        int defenderIndex = 0;

        for (int i = 0; i < _rows.Count; i++)
        {
            ScoreRow row = _rows[i];

            if (row.Team == Team.Red)
            {
                FillSlot(_attackerSlots, attackerIndex, row);
                attackerIndex++;
            }
            else if (row.Team == Team.Blue)
            {
                FillSlot(_defenderSlots, defenderIndex, row);
                defenderIndex++;
            }
        }
    }

    private static void HideSlots(ScoreboardSlotUI[] slots)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].gameObject.SetActive(false);
                slots[i].ClearSlot();
            }
        }
    }

    private void FillSlot(ScoreboardSlotUI[] slots, int index, ScoreRow row)
    {
        if (slots == null || index >= slots.Length || slots[index] == null) return;
        slots[index].gameObject.SetActive(true);
        slots[index].SetData(row.Name, row.Kills, row.Assists, row.Deaths);
    }

    private void ValidateSlots()
    {
        if (_attackerSlots == null || _attackerSlots.Length == 0)
            Debug.LogWarning("[ScoreboardUI] _attackerSlots está vacío en el Inspector.", this);
        if (_defenderSlots == null || _defenderSlots.Length == 0)
            Debug.LogWarning("[ScoreboardUI] _defenderSlots está vacío en el Inspector.", this);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!_debugOverlay) return;

        bool isPressed = (InputManager.Instance != null && InputManager.Instance.IsScoreboardPressed) ||
                         (Keyboard.current != null && Keyboard.current.tabKey.isPressed) ||
                         (Gamepad.current != null && Gamepad.current.selectButton.isPressed);

        if (!isPressed) return;

        ScoreboardManager manager = ScoreboardManager.Instance;

        GUILayout.BeginArea(new Rect(10, 10, 560, 360), GUI.skin.box);
        GUILayout.Label("DEBUG SCOREBOARD");
        GUILayout.Label($"ScoreboardManager: {(manager == null ? "NULL" : "OK, Count=" + manager.PlayerStats.Count)}");

        for (int i = 0; i < _rows.Count; i++)
        {
            ScoreRow r = _rows[i];
            GUILayout.Label($"[{r.ClientId}] {r.Name} | {r.Team} | K:{r.Kills} A:{r.Assists} D:{r.Deaths}");
        }

        GUILayout.EndArea();
    }
#endif
}