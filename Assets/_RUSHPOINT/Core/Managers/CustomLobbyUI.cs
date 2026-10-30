using System;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CustomLobbyUI : MonoBehaviour
{
    private const string MAP_NAME = "HEAVEN";
    private const string MODE_NAME = "SEARCH & DESTROY";
    private const string COMING_SOON_LABEL = " [COMING SOON]";

    [SerializeField] private TMP_Text _lobbyCodeText;
    [SerializeField] private Button _copyCodeButton;
    [SerializeField] private Button _backButton;

    [SerializeField] private TMP_Dropdown _mapDropdown;
    [SerializeField] private TMP_Dropdown _modeDropdown;
    [SerializeField] private TMP_Dropdown _roundTimeDropdown;
    [SerializeField] private TMP_Dropdown _roundsDropdown;

    [SerializeField] private TMP_Text _attackersCountText;
    [SerializeField] private Button _joinAttackersButton;
    [SerializeField] private TMP_Text[] _attackerSlotTexts;

    [SerializeField] private TMP_Text _defendersCountText;
    [SerializeField] private Button _joinDefendersButton;
    [SerializeField] private TMP_Text[] _defenderSlotTexts;

    [SerializeField] private Button _joinSpectatorsButton;
    [SerializeField] private TMP_Text[] _spectatorSlotTexts;

    [SerializeField] private Button _actionButton;
    [SerializeField] private TMP_Text _actionButtonText;

    private readonly float[] _roundTimeDurations = new float[] { 90f, 120f, 170f };
    private readonly int[] _roundsToWinOptions = new int[] { 5, 8, 13 };

    private void OnEnable()
    {
        SyncLocalNameToLobby();
    }

    private void Start()
    {
        InitializeDropdowns();

        if (_copyCodeButton != null) _copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
        if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

        if (_joinAttackersButton != null) _joinAttackersButton.onClick.AddListener(() => SelectTeam(Team.Red));
        if (_joinDefendersButton != null) _joinDefendersButton.onClick.AddListener(() => SelectTeam(Team.Blue));
        if (_joinSpectatorsButton != null) _joinSpectatorsButton.onClick.AddListener(() => SelectTeam(Team.Neutral));

        if (_actionButton != null) _actionButton.onClick.AddListener(OnActionButtonClicked);

        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.OnLobbyDataUpdated += RefreshUI;
        }

        SyncLocalNameToLobby();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_copyCodeButton != null) _copyCodeButton.onClick.RemoveListener(OnCopyCodeClicked);
        if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);

        if (_joinAttackersButton != null) _joinAttackersButton.onClick.RemoveAllListeners();
        if (_joinDefendersButton != null) _joinDefendersButton.onClick.RemoveAllListeners();
        if (_joinSpectatorsButton != null) _joinSpectatorsButton.onClick.RemoveAllListeners();

        if (_actionButton != null) _actionButton.onClick.RemoveListener(OnActionButtonClicked);

        if (_roundTimeDropdown != null) _roundTimeDropdown.onValueChanged.RemoveListener(OnRoundTimeChanged);
        if (_roundsDropdown != null) _roundsDropdown.onValueChanged.RemoveListener(OnRoundsChanged);

        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.OnLobbyDataUpdated -= RefreshUI;
        }
    }

    private void SyncLocalNameToLobby()
    {
        StopCoroutine(nameof(SyncNameRoutine));
        StartCoroutine(nameof(SyncNameRoutine));
    }

    private IEnumerator SyncNameRoutine()
    {
        while (CustomLobbyManager.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            yield return null;
        }

        string localName = LobbyUI.LocalPlayerName;
        if (string.IsNullOrEmpty(localName))
        {
            localName = PlayerPrefs.GetString("Rushpoint_PlayerName", $"Player_{NetworkManager.Singleton.LocalClientId + 1}");
        }

        CustomLobbyManager.Instance.RequestUpdatePlayerNameServerRpc(
            new Unity.Collections.FixedString32Bytes(localName),
            NetworkManager.Singleton.LocalClientId
        );
    }

    private void InitializeDropdowns()
    {
        if (_mapDropdown != null)
        {
            _mapDropdown.ClearOptions();
            _mapDropdown.options.Add(new TMP_Dropdown.OptionData(MAP_NAME));
            _mapDropdown.options.Add(new TMP_Dropdown.OptionData("ASCENT" + COMING_SOON_LABEL));
            _mapDropdown.options.Add(new TMP_Dropdown.OptionData("BIND" + COMING_SOON_LABEL));
            _mapDropdown.value = 0;
            _mapDropdown.RefreshShownValue();
            _mapDropdown.onValueChanged.AddListener(index =>
            {
                if (index != 0) _mapDropdown.value = 0;
            });
        }

        if (_modeDropdown != null)
        {
            _modeDropdown.ClearOptions();
            _modeDropdown.options.Add(new TMP_Dropdown.OptionData(MODE_NAME));
            _modeDropdown.options.Add(new TMP_Dropdown.OptionData("DEATHMATCH" + COMING_SOON_LABEL));
            _modeDropdown.options.Add(new TMP_Dropdown.OptionData("SPIKE RUSH" + COMING_SOON_LABEL));
            _modeDropdown.value = 0;
            _modeDropdown.RefreshShownValue();
            _modeDropdown.onValueChanged.AddListener(index =>
            {
                if (index != 0) _modeDropdown.value = 0;
            });
        }

        if (_roundTimeDropdown != null)
        {
            _roundTimeDropdown.onValueChanged.AddListener(OnRoundTimeChanged);
        }

        if (_roundsDropdown != null)
        {
            _roundsDropdown.onValueChanged.AddListener(OnRoundsChanged);
        }
    }

    private void OnRoundTimeChanged(int index)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (index >= 0 && index < _roundTimeDurations.Length && CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.SetRoundDurationServerRpc(_roundTimeDurations[index]);
        }
    }

    private void OnRoundsChanged(int index)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (index >= 0 && index < _roundsToWinOptions.Length && CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.SetRoundsToWinServerRpc(_roundsToWinOptions[index]);
        }
    }

    private void SelectTeam(Team team)
    {
        if (CustomLobbyManager.Instance == null || NetworkManager.Singleton == null) return;
        CustomLobbyManager.Instance.RequestSelectTeamServerRpc(team, NetworkManager.Singleton.LocalClientId);
    }

    private void OnActionButtonClicked()
    {
        if (CustomLobbyManager.Instance == null || NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            if (CustomLobbyManager.Instance.CanStartMatch())
            {
                CustomLobbyManager.Instance.RequestStartGameServerRpc();
            }
            else
            {
                CustomLobbyManager.Instance.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }
        else
        {
            CustomLobbyManager.Instance.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnCopyCodeClicked()
    {
        if (CustomLobbyManager.Instance == null) return;
        string code = CustomLobbyManager.Instance.LobbyJoinCode.Value.ToString();
        if (!string.IsNullOrEmpty(code))
        {
            GUIUtility.systemCopyBuffer = code;
        }
    }

    private void OnBackClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void RefreshUI()
    {
        if (CustomLobbyManager.Instance == null || NetworkManager.Singleton == null) return;

        bool isServer = NetworkManager.Singleton.IsServer;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (_roundTimeDropdown != null)
        {
            _roundTimeDropdown.interactable = isServer;
            int timeIndex = Array.IndexOf(_roundTimeDurations, CustomLobbyManager.Instance.SelectedRoundDuration.Value);
            if (timeIndex != -1 && _roundTimeDropdown.value != timeIndex)
            {
                _roundTimeDropdown.SetValueWithoutNotify(timeIndex);
            }
        }

        if (_roundsDropdown != null)
        {
            _roundsDropdown.interactable = isServer;
            int roundIndex = Array.IndexOf(_roundsToWinOptions, CustomLobbyManager.Instance.SelectedRoundsToWin.Value);
            if (roundIndex != -1 && _roundsDropdown.value != roundIndex)
            {
                _roundsDropdown.SetValueWithoutNotify(roundIndex);
            }
        }

        string rawCode = CustomLobbyManager.Instance.LobbyJoinCode.Value.ToString();
        if (_lobbyCodeText != null)
        {
            _lobbyCodeText.text = string.IsNullOrEmpty(rawCode) ? "#CONNECTING..." : $"#{rawCode}";
        }

        ClearSlotTexts();

        int attackerIndex = 0;
        int defenderIndex = 0;
        int spectatorIndex = 0;
        bool localIsReady = false;

        for (int i = 0; i < CustomLobbyManager.Instance.Players.Count; i++)
        {
            LobbyPlayerData p = CustomLobbyManager.Instance.Players[i];
            bool isMe = p.ClientId == localId;
            if (isMe) localIsReady = p.IsReady;

            string readyBadge = p.AssignedTeam == Team.Neutral ? "" : (p.IsReady ? " <color=#00FF66>[READY]</color>" : " <color=#FF3333>[NOT READY]</color>");
            string display = $"{p.PlayerName}{(isMe ? " (YOU)" : "")}{readyBadge}";

            if (p.AssignedTeam == Team.Red)
            {
                if (_attackerSlotTexts != null && attackerIndex < _attackerSlotTexts.Length)
                {
                    _attackerSlotTexts[attackerIndex].text = display;
                    _attackerSlotTexts[attackerIndex].gameObject.SetActive(true);
                }
                attackerIndex++;
            }
            else if (p.AssignedTeam == Team.Blue)
            {
                if (_defenderSlotTexts != null && defenderIndex < _defenderSlotTexts.Length)
                {
                    _defenderSlotTexts[defenderIndex].text = display;
                    _defenderSlotTexts[defenderIndex].gameObject.SetActive(true);
                }
                defenderIndex++;
            }
            else
            {
                if (_spectatorSlotTexts != null && spectatorIndex < _spectatorSlotTexts.Length)
                {
                    _spectatorSlotTexts[spectatorIndex].text = display;
                    _spectatorSlotTexts[spectatorIndex].gameObject.SetActive(true);
                }
                spectatorIndex++;
            }
        }

        if (_attackersCountText != null) _attackersCountText.text = $"{attackerIndex}/{CustomLobbyManager.MAX_PLAYERS_PER_TEAM}";
        if (_defendersCountText != null) _defendersCountText.text = $"{defenderIndex}/{CustomLobbyManager.MAX_PLAYERS_PER_TEAM}";

        UpdateActionButtonVisual(isServer, localIsReady);
    }

    private void UpdateActionButtonVisual(bool isServer, bool localIsReady)
    {
        if (_actionButton == null || _actionButtonText == null) return;

        if (isServer)
        {
            bool canStart = CustomLobbyManager.Instance.CanStartMatch();
            if (canStart)
            {
                _actionButton.interactable = true;
                _actionButtonText.text = "START MATCH";
            }
            else
            {
                _actionButton.interactable = true;
                _actionButtonText.text = localIsReady ? "WAITING FOR PLAYERS..." : "SET READY";
            }
        }
        else
        {
            _actionButton.interactable = true;
            _actionButtonText.text = localIsReady ? "CANCEL READY" : "READY";
        }
    }

    private void ClearSlotTexts()
    {
        if (_attackerSlotTexts != null)
        {
            for (int i = 0; i < _attackerSlotTexts.Length; i++)
            {
                if (_attackerSlotTexts[i] != null) _attackerSlotTexts[i].text = "Empty Slot";
            }
        }

        if (_defenderSlotTexts != null)
        {
            for (int i = 0; i < _defenderSlotTexts.Length; i++)
            {
                if (_defenderSlotTexts[i] != null) _defenderSlotTexts[i].text = "Empty Slot";
            }
        }

        if (_spectatorSlotTexts != null)
        {
            for (int i = 0; i < _spectatorSlotTexts.Length; i++)
            {
                if (_spectatorSlotTexts[i] != null) _spectatorSlotTexts[i].text = "";
            }
        }
    }
}