using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class CustomLobbyManager : NetworkBehaviour
{
    public const int MAX_PLAYERS_PER_TEAM = 5;
    private const float DEFAULT_ROUND_DURATION = 90f;
    private const int DEFAULT_ROUNDS_TO_WIN = 5;

    public static CustomLobbyManager Instance { get; private set; }

    public event Action OnLobbyDataUpdated;
    public event Action OnMatchStarted;

    [SerializeField] private GameObject _customLobbyCanvas;
    [SerializeField] private GameObject _roleSelectCanvas;

    public NetworkList<LobbyPlayerData> Players;

    public NetworkVariable<float> SelectedRoundDuration = new NetworkVariable<float>(
        DEFAULT_ROUND_DURATION,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> SelectedRoundsToWin = new NetworkVariable<int>(
        DEFAULT_ROUNDS_TO_WIN,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString32Bytes> LobbyJoinCode = new NetworkVariable<FixedString32Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Players = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        Players.OnListChanged += HandlePlayersChanged;
        SelectedRoundDuration.OnValueChanged += HandleRoundDurationChanged;
        SelectedRoundsToWin.OnValueChanged += HandleRoundsToWinChanged;
        LobbyJoinCode.OnValueChanged += HandleJoinCodeChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            RegisterPlayer(NetworkManager.Singleton.LocalClientId);
        }

        OnLobbyDataUpdated?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        Players.OnListChanged -= HandlePlayersChanged;
        SelectedRoundDuration.OnValueChanged -= HandleRoundDurationChanged;
        SelectedRoundsToWin.OnValueChanged -= HandleRoundsToWinChanged;
        LobbyJoinCode.OnValueChanged -= HandleJoinCodeChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetLobbyJoinCode(string code)
    {
        if (IsServer)
        {
            LobbyJoinCode.Value = code;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        RegisterPlayer(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players.RemoveAt(i);
                break;
            }
        }
    }

    private void RegisterPlayer(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId) return;
        }

        Team initialTeam = GetNextAvailableTeam();
        FixedString32Bytes defaultName = $"Player {clientId + 1}";
        Players.Add(new LobbyPlayerData(clientId, defaultName, initialTeam, false));
    }

    private Team GetNextAvailableTeam()
    {
        int redCount = 0;
        int blueCount = 0;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].AssignedTeam == Team.Red) redCount++;
            else if (Players[i].AssignedTeam == Team.Blue) blueCount++;
        }

        if (redCount < MAX_PLAYERS_PER_TEAM) return Team.Red;
        if (blueCount < MAX_PLAYERS_PER_TEAM) return Team.Blue;
        return Team.Neutral;
    }

    [Rpc(SendTo.Server)]
    public void RequestUpdatePlayerNameServerRpc(FixedString32Bytes newName, ulong requestingClientId)
    {
        if (string.IsNullOrWhiteSpace(newName.ToString())) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == requestingClientId)
            {
                LobbyPlayerData updated = Players[i];
                updated.PlayerName = newName;
                Players[i] = updated;
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestSelectTeamServerRpc(Team desiredTeam, ulong requestingClientId)
    {
        if (desiredTeam != Team.Neutral)
        {
            int currentTeamCount = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].AssignedTeam == desiredTeam) currentTeamCount++;
            }

            if (currentTeamCount >= MAX_PLAYERS_PER_TEAM) return;
        }

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == requestingClientId)
            {
                LobbyPlayerData updated = Players[i];
                updated.AssignedTeam = desiredTeam;
                updated.IsReady = false;
                Players[i] = updated;
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyServerRpc(ulong requestingClientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == requestingClientId)
            {
                LobbyPlayerData updated = Players[i];
                updated.IsReady = !updated.IsReady;
                Players[i] = updated;
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SetRoundDurationServerRpc(float duration)
    {
        if (!IsServer) return;
        SelectedRoundDuration.Value = duration;
    }

    [Rpc(SendTo.Server)]
    public void SetRoundsToWinServerRpc(int rounds)
    {
        if (!IsServer) return;
        SelectedRoundsToWin.Value = rounds;
    }

    public bool CanStartMatch()
    {
        if (!IsServer) return false;

        int activeRed = 0;
        int activeBlue = 0;

        for (int i = 0; i < Players.Count; i++)
        {
            LobbyPlayerData player = Players[i];

            if (player.AssignedTeam != Team.Neutral)
            {
                if (!player.IsReady) return false;

                if (player.AssignedTeam == Team.Red) activeRed++;
                else if (player.AssignedTeam == Team.Blue) activeBlue++;
            }
        }

        return (activeRed > 0 || activeBlue > 0);
    }

    [Rpc(SendTo.Server)]
    public void RequestStartGameServerRpc()
    {
        if (!IsServer || !CanStartMatch()) return;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ConfigureMatchSettingsServer(SelectedRoundDuration.Value, SelectedRoundsToWin.Value);
        }

        StartGameClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartGameClientRpc()
    {
        if (_customLobbyCanvas != null)
        {
            _customLobbyCanvas.SetActive(false);
        }

        if (_roleSelectCanvas != null)
        {
            _roleSelectCanvas.SetActive(true);
        }

        OnMatchStarted?.Invoke();
    }

    private void HandlePlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleRoundDurationChanged(float previous, float current)
    {
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleRoundsToWinChanged(int previous, int current)
    {
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleJoinCodeChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        OnLobbyDataUpdated?.Invoke();
    }

    public Team GetPlayerAssignedTeam(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                return Players[i].AssignedTeam;
            }
        }
        return Team.Neutral;
    }

    public string GetPlayerName(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                return Players[i].PlayerName.ToString();
            }
        }
        return $"Player {clientId + 1}";
    }
}