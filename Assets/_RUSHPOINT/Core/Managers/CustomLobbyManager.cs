using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class CustomLobbyManager : NetworkBehaviour
{
    public const int MAX_PLAYERS_PER_TEAM = 5;

    public static CustomLobbyManager Instance { get; private set; }

    public static readonly Dictionary<ulong, Team> SavedTeams = new Dictionary<ulong, Team>();
    public static readonly Dictionary<ulong, PlayerRoleType> SavedRoles = new Dictionary<ulong, PlayerRoleType>();

    public event Action OnLobbyDataUpdated;

    [Header("Escena")]
    [SerializeField] private string _gameSceneName = "RUSPOINT";
    public string GameSceneName => _gameSceneName;

    [Header("UI Canvas")]
    [SerializeField] private GameObject _customLobbyCanvas;
    [SerializeField] private GameObject _roleSelectCanvas;
    [SerializeField] private GameObject _spectatorWaitCanvas;

    public NetworkList<LobbyPlayerData> Players;

    public NetworkVariable<FixedString64Bytes> LobbyJoinCode = new NetworkVariable<FixedString64Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> SelectedRoundDuration = new NetworkVariable<float>(
        115f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> SelectedRoundsToWin = new NetworkVariable<int>(
        13,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool CanStartMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return false;
        if (Players == null || Players.Count == 0) return true;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].AssignedTeam != Team.Neutral && !Players[i].IsReady)
            {
                return false;
            }
        }
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Players = new NetworkList<LobbyPlayerData>(
            new List<LobbyPlayerData>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            HandleClientConnected(NetworkManager.Singleton.LocalClientId);
        }

        Players.OnListChanged += (changeEvent) => OnLobbyDataUpdated?.Invoke();
        LobbyJoinCode.OnValueChanged += (prev, curr) => OnLobbyDataUpdated?.Invoke();
        SelectedRoundDuration.OnValueChanged += (prev, curr) => OnLobbyDataUpdated?.Invoke();
        SelectedRoundsToWin.OnValueChanged += (prev, curr) => OnLobbyDataUpdated?.Invoke();

        OnLobbyDataUpdated?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

  


    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId) return;
        }

        string defaultName = PlayerPrefs.GetString("PlayerName", $"Player_{clientId}");
        Team defaultTeam = Team.Neutral;

        Players.Add(new LobbyPlayerData(clientId, defaultName, defaultTeam, false));
        SavedTeams[clientId] = defaultTeam;
        OnLobbyDataUpdated?.Invoke();
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

        SavedTeams.Remove(clientId);
        SavedRoles.Remove(clientId);
        OnLobbyDataUpdated?.Invoke();
    }

    public static void SavePlayerData(ulong clientId, Team team, PlayerRoleType role)
    {
        SavedTeams[clientId] = team;
        SavedRoles[clientId] = role;
    }

    public Team GetPlayerAssignedTeam(ulong clientId)
    {
        if (SavedTeams != null && SavedTeams.TryGetValue(clientId, out Team team))
        {
            return team;
        }

        if (Players != null)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].ClientId == clientId)
                {
                    return Players[i].AssignedTeam;
                }
            }
        }

        return Team.Red;
    }

    public void SetLobbyJoinCode(string code)
    {
        if (IsServer)
        {
            LobbyJoinCode.Value = code;
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
        LobbyJoinCode.Value = code;
        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void SetRoundDurationServerRpc(float duration)
    {
        SelectedRoundDuration.Value = duration;
        OnLobbyDataUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void SetRoundsToWinServerRpc(int rounds)
    {
        SelectedRoundsToWin.Value = rounds;
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

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = Players[i];
                updated.PlayerName = newName;
                Players[i] = updated;
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

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = Players[i];
                updated.AssignedTeam = newTeam;
                Players[i] = updated;
                SavedTeams[targetId] = newTeam;
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

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == targetId)
            {
                LobbyPlayerData updated = Players[i];
                updated.IsReady = !updated.IsReady;
                Players[i] = updated;
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