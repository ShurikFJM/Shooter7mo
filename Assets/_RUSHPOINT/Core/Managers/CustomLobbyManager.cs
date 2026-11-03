using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class CustomLobbyManager : NetworkBehaviour
{
    public const int MAX_PLAYERS_PER_TEAM = 5;
    private const float DEFAULT_ROUND_DURATION = 90f;
    private const int DEFAULT_ROUNDS_TO_WIN = 5;
    private const string DEFAULT_MAP_SCENE_NAME = "RUSPOINT";
    private const string PLAYER_PREFS_NAME_KEY = "PlayerName";
    private const string DEFAULT_PLAYER_NAME_PREFIX = "Player_";

    private static CustomLobbyManager _instance;
    public static CustomLobbyManager Instance => _instance;

    private static readonly Dictionary<ulong, Team> _savedTeams = new Dictionary<ulong, Team>();
    public static Dictionary<ulong, Team> SavedTeams => _savedTeams;

    private static readonly Dictionary<ulong, PlayerRoleType> _savedRoles = new Dictionary<ulong, PlayerRoleType>();
    public static Dictionary<ulong, PlayerRoleType> SavedRoles => _savedRoles;

    private static float _savedRoundDuration = DEFAULT_ROUND_DURATION;
    public static float SavedRoundDuration
    {
        get => _savedRoundDuration;
        set => _savedRoundDuration = value;
    }

    private static int _savedRoundsToWin = DEFAULT_ROUNDS_TO_WIN;
    public static int SavedRoundsToWin
    {
        get => _savedRoundsToWin;
        set => _savedRoundsToWin = value;
    }

    public event Action OnLobbyDataUpdated;

    [SerializeField] private string _gameSceneName = DEFAULT_MAP_SCENE_NAME;
    public string GameSceneName => _gameSceneName;

    [SerializeField] private GameObject _customLobbyCanvas;
    [SerializeField] private GameObject _roleSelectCanvas;
    [SerializeField] private GameObject _spectatorWaitCanvas;

    private NetworkList<LobbyPlayerData> _players;
    public NetworkList<LobbyPlayerData> Players => _players;

    private readonly NetworkVariable<FixedString64Bytes> _lobbyJoinCode = new NetworkVariable<FixedString64Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> LobbyJoinCode => _lobbyJoinCode;

    private readonly NetworkVariable<float> _selectedRoundDuration = new NetworkVariable<float>(
        DEFAULT_ROUND_DURATION,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> SelectedRoundDuration => _selectedRoundDuration;

    private readonly NetworkVariable<int> _selectedRoundsToWin = new NetworkVariable<int>(
        DEFAULT_ROUNDS_TO_WIN,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> SelectedRoundsToWin => _selectedRoundsToWin;

    public static void ClearLobbyData()
    {
        _savedTeams.Clear();
        _savedRoles.Clear();
        _savedRoundDuration = DEFAULT_ROUND_DURATION;
        _savedRoundsToWin = DEFAULT_ROUNDS_TO_WIN;
    }

    public bool CanStartMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return false;
        if (_players == null || _players.Count == 0) return true;

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].AssignedTeam != Team.Neutral && !_players[i].IsReady)
            {
                return false;
            }
        }
        return true;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _players = new NetworkList<LobbyPlayerData>(
            new List<LobbyPlayerData>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnDestroy()
    {
        if (_instance == this) _instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            _savedRoundDuration = _selectedRoundDuration.Value;
            _savedRoundsToWin = _selectedRoundsToWin.Value;

            HandleClientConnected(NetworkManager.Singleton.LocalClientId);
        }

        _players.OnListChanged += (changeEvent) => OnLobbyDataUpdated?.Invoke();
        _lobbyJoinCode.OnValueChanged += (prev, curr) => OnLobbyDataUpdated?.Invoke();
        _selectedRoundDuration.OnValueChanged += HandleRoundDurationNetworkChanged;
        _selectedRoundsToWin.OnValueChanged += HandleRoundsToWinNetworkChanged;

        OnLobbyDataUpdated?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        _selectedRoundDuration.OnValueChanged -= HandleRoundDurationNetworkChanged;
        _selectedRoundsToWin.OnValueChanged -= HandleRoundsToWinNetworkChanged;
    }

    private void HandleRoundDurationNetworkChanged(float previousValue, float currentValue)
    {
        if (IsServer)
        {
            _savedRoundDuration = currentValue;
        }
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleRoundsToWinNetworkChanged(int previousValue, int currentValue)
    {
        if (IsServer)
        {
            _savedRoundsToWin = currentValue;
        }
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId) return;
        }

        string defaultName = PlayerPrefs.GetString(PLAYER_PREFS_NAME_KEY, string.Concat(DEFAULT_PLAYER_NAME_PREFIX, clientId));
        Team defaultTeam = Team.Neutral;

        _players.Add(new LobbyPlayerData(clientId, defaultName, defaultTeam, false));
        _savedTeams[clientId] = defaultTeam;
        OnLobbyDataUpdated?.Invoke();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
            {
                _players.RemoveAt(i);
                break;
            }
        }

        _savedTeams.Remove(clientId);
        _savedRoles.Remove(clientId);
        OnLobbyDataUpdated?.Invoke();
    }

    public static void SavePlayerData(ulong clientId, Team team, PlayerRoleType role)
    {
        _savedTeams[clientId] = team;
        _savedRoles[clientId] = role;
    }

    public Team GetPlayerAssignedTeam(ulong clientId)
    {
        if (_savedTeams != null && _savedTeams.TryGetValue(clientId, out Team team))
        {
            return team;
        }

        if (_players != null)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].ClientId == clientId)
                {
                    return _players[i].AssignedTeam;
                }
            }
        }

        return Team.Red;
    }

    public void SetLobbyJoinCode(string code)
    {
        if (IsServer)
        {
            _lobbyJoinCode.Value = code;
            OnLobbyDataUpdated?.Invoke();
        }
        else
        {
            SetLobbyJoinCodeServerRpc(code);
        }
    }

    [Rpc(SendTo.Server)]
    public void SetLobbyJoinCodeServerRpc(FixedString64Bytes code)
    {
        _lobbyJoinCode.Value = code;
        OnLobbyDataUpdated?.Invoke();
    }

    public void SetRoundDuration(float duration)
    {
        if (IsServer)
        {
            _selectedRoundDuration.Value = duration;
            _savedRoundDuration = duration;
            OnLobbyDataUpdated?.Invoke();
        }
        else
        {
            SetRoundDurationServerRpc(duration);
        }
    }

    [Rpc(SendTo.Server)]
    public void SetRoundDurationServerRpc(float duration)
    {
        _selectedRoundDuration.Value = duration;
        _savedRoundDuration = duration;
        OnLobbyDataUpdated?.Invoke();
    }

    public void SetRoundsToWin(int rounds)
    {
        if (IsServer)
        {
            _selectedRoundsToWin.Value = rounds;
            _savedRoundsToWin = rounds;
            OnLobbyDataUpdated?.Invoke();
        }
        else
        {
            SetRoundsToWinServerRpc(rounds);
        }
    }

    [Rpc(SendTo.Server)]
    public void SetRoundsToWinServerRpc(int rounds)
    {
        _selectedRoundsToWin.Value = rounds;
        _savedRoundsToWin = rounds;
        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void RequestUpdatePlayerNameServerRpc(FixedString32Bytes newName, ulong clientId = 0, RpcParams rpcParams = default)
    {
        ulong targetId = (clientId != 0) ? clientId : rpcParams.Receive.SenderClientId;
        if (targetId == 0 && NetworkManager.Singleton != null)
        {
            targetId = NetworkManager.Singleton.LocalClientId;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = _players[i];
                updated.PlayerName = newName;
                _players[i] = updated;
                break;
            }
        }

        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void UpdatePlayerNameServerRpc(FixedString32Bytes newName, ulong clientId = 0, RpcParams rpcParams = default)
    {
        RequestUpdatePlayerNameServerRpc(newName, clientId, rpcParams);
    }

    [Rpc(SendTo.Server)]
    public void RequestSelectTeamServerRpc(Team newTeam, ulong clientId = 0, RpcParams rpcParams = default)
    {
        SelectTeamInternal(newTeam, clientId, rpcParams);
    }

    [Rpc(SendTo.Server)]
    public void SelectTeamServerRpc(Team newTeam, ulong clientId = 0, RpcParams rpcParams = default)
    {
        SelectTeamInternal(newTeam, clientId, rpcParams);
    }

    private void SelectTeamInternal(Team newTeam, ulong clientId, RpcParams rpcParams)
    {
        ulong targetId = (clientId != 0) ? clientId : rpcParams.Receive.SenderClientId;
        if (targetId == 0 && NetworkManager.Singleton != null)
        {
            targetId = NetworkManager.Singleton.LocalClientId;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = _players[i];
                updated.AssignedTeam = newTeam;
                _players[i] = updated;
                _savedTeams[targetId] = newTeam;
                break;
            }
        }

        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyServerRpc(ulong clientId = 0, RpcParams rpcParams = default)
    {
        ulong targetId = (clientId != 0) ? clientId : rpcParams.Receive.SenderClientId;
        if (targetId == 0 && NetworkManager.Singleton != null)
        {
            targetId = NetworkManager.Singleton.LocalClientId;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = _players[i];
                updated.IsReady = !updated.IsReady;
                _players[i] = updated;
                break;
            }
        }

        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void RequestToggleReadyServerRpc(ulong clientId = 0, RpcParams rpcParams = default)
    {
        ToggleReadyServerRpc(clientId, rpcParams);
    }

    [Rpc(SendTo.Server)]
    public void RequestStartGameServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        _savedRoundDuration = _selectedRoundDuration.Value;
        _savedRoundsToWin = _selectedRoundsToWin.Value;

        StartRoleSelectionClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartRoleSelectionClientRpc()
    {
        if (_customLobbyCanvas != null) _customLobbyCanvas.SetActive(false);

        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        Team myTeam = GetPlayerAssignedTeam(localId);

        if (myTeam == Team.Neutral)
        {
            if (_spectatorWaitCanvas != null) _spectatorWaitCanvas.SetActive(true);
            if (_roleSelectCanvas != null) _roleSelectCanvas.SetActive(false);
        }
        else
        {
            if (_roleSelectCanvas != null) _roleSelectCanvas.SetActive(true);
            if (_spectatorWaitCanvas != null) _spectatorWaitCanvas.SetActive(false);
        }
    }
}