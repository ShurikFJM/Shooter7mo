using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbySettingsController : MonoBehaviour
{
    private readonly float[] _roundDurationValues = { 90f, 120f, 170f };
    private readonly int[] _roundsToWinValues = { 5, 8, 13 };

    [SerializeField] private TMP_Dropdown _roundTimeDropdown;
    [SerializeField] private TMP_Dropdown _roundsDropdown;

    private void Start()
    {
        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.OnLobbyDataUpdated += RefreshDisplayFromLobby;
        }

        if (_roundTimeDropdown != null)
        {
            _roundTimeDropdown.onValueChanged.AddListener(HandleRoundTimeDropdownChanged);
        }

        if (_roundsDropdown != null)
        {
            _roundsDropdown.onValueChanged.AddListener(HandleRoundsDropdownChanged);
        }

        RefreshDisplayFromLobby();
    }

    private void OnDestroy()
    {
        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.OnLobbyDataUpdated -= RefreshDisplayFromLobby;
        }

        if (_roundTimeDropdown != null)
        {
            _roundTimeDropdown.onValueChanged.RemoveListener(HandleRoundTimeDropdownChanged);
        }

        if (_roundsDropdown != null)
        {
            _roundsDropdown.onValueChanged.RemoveListener(HandleRoundsDropdownChanged);
        }
    }

    private void RefreshDisplayFromLobby()
    {
        if (CustomLobbyManager.Instance == null) return;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        if (_roundTimeDropdown != null)
        {
            _roundTimeDropdown.interactable = isHost;
            int durationIndex = GetDurationDropdownIndex(CustomLobbyManager.Instance.SelectedRoundDuration.Value);
            _roundTimeDropdown.SetValueWithoutNotify(durationIndex);
        }

        if (_roundsDropdown != null)
        {
            _roundsDropdown.interactable = isHost;
            int roundsIndex = GetRoundsDropdownIndex(CustomLobbyManager.Instance.SelectedRoundsToWin.Value);
            _roundsDropdown.SetValueWithoutNotify(roundsIndex);
        }
    }

    private void HandleRoundTimeDropdownChanged(int index)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (index < 0 || index >= _roundDurationValues.Length) return;

        float selectedDuration = _roundDurationValues[index];

        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.SetRoundDuration(selectedDuration);
        }
    }

    private void HandleRoundsDropdownChanged(int index)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (index < 0 || index >= _roundsToWinValues.Length) return;

        int selectedRounds = _roundsToWinValues[index];

        if (CustomLobbyManager.Instance != null)
        {
            CustomLobbyManager.Instance.SetRoundsToWin(selectedRounds);
        }
    }

    private int GetDurationDropdownIndex(float duration)
    {
        for (int i = 0; i < _roundDurationValues.Length; i++)
        {
            if (Mathf.Approximately(_roundDurationValues[i], duration))
            {
                return i;
            }
        }
        return 0;
    }

    private int GetRoundsDropdownIndex(int rounds)
    {
        for (int i = 0; i < _roundsToWinValues.Length; i++)
        {
            if (_roundsToWinValues[i] == rounds)
            {
                return i;
            }
        }
        return 0;
    }
}