using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public class RoleLobbyManager : NetworkBehaviour
{
    public static RoleLobbyManager Instance { get; private set; }

    [SerializeField] private Button _lockInButton;
    [SerializeField] private GameObject _roleSelectCanvas;

    private readonly Dictionary<ulong, PlayerRoleType> _selectedRoles = new Dictionary<ulong, PlayerRoleType>();
    private readonly Dictionary<ulong, Team> _selectedTeams = new Dictionary<ulong, Team>();
    private readonly HashSet<ulong> _lockedInClients = new HashSet<ulong>();
    private PlayerRoleType _localSelectedRole = PlayerRoleType.Assault;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_lockInButton != null)
        {
            _lockInButton.onClick.AddListener(OnLockInClicked);
        }
    }

    public override void OnDestroy()
    {
        if (_lockInButton != null)
        {
            _lockInButton.onClick.RemoveListener(OnLockInClicked);
        }

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }

    public void SelectRole(PlayerRoleType role)
    {
        _localSelectedRole = role;
    }

    public void ConfirmRoleSelection(PlayerRoleType role, Team team)
    {
        _localSelectedRole = role;

        if (NetworkManager.Singleton == null) return;

        if (!IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartGameSceneLoad();
            }
            return;
        }

        LockInRoleAndTeamServerRpc(role, team);
    }

    private void OnLockInClicked()
    {
        if (NetworkManager.Singleton == null) return;

        if (_lockInButton != null)
        {
            _lockInButton.interactable = false;
        }

        Team localTeam = Team.Red;
        if (CustomLobbyManager.Instance != null)
        {
            localTeam = CustomLobbyManager.Instance.GetPlayerAssignedTeam(NetworkManager.Singleton.LocalClientId);
            if (localTeam == Team.Neutral) localTeam = Team.Red;
        }

        ConfirmRoleSelection(_localSelectedRole, localTeam);
    }

    [Rpc(SendTo.Server)]
    public void LockInRoleAndTeamServerRpc(PlayerRoleType role, Team team, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        _selectedRoles[clientId] = role;
        _selectedTeams[clientId] = team;

        if (CustomLobbyManager.Instance != null)
        {
            for (int i = 0; i < CustomLobbyManager.Instance.Players.Count; i++)
            {
                if (CustomLobbyManager.Instance.Players[i].ClientId == clientId)
                {
                    LobbyPlayerData updated = CustomLobbyManager.Instance.Players[i];
                    if (team != Team.Neutral)
                    {
                        updated.AssignedTeam = team;
                    }
                    CustomLobbyManager.Instance.Players[i] = updated;
                    break;
                }
            }
        }

        if (!_lockedInClients.Contains(clientId))
        {
            _lockedInClients.Add(clientId);
        }

        CheckAllActivePlayersLockedIn();
    }

    [Rpc(SendTo.Server)]
    public void SubmitRoleChoiceServerRpc(PlayerRoleType role, ulong clientId)
    {
        Team fallbackTeam = Team.Red;
        if (CustomLobbyManager.Instance != null)
        {
            fallbackTeam = CustomLobbyManager.Instance.GetPlayerAssignedTeam(clientId);
        }

        _selectedRoles[clientId] = role;
        _selectedTeams[clientId] = fallbackTeam;

        if (!_lockedInClients.Contains(clientId))
        {
            _lockedInClients.Add(clientId);
        }

        CheckAllActivePlayersLockedIn();
    }

    private void CheckAllActivePlayersLockedIn()
    {
        if (!IsServer) return;

        int totalConnected = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClients.Count : 1;

        if (_lockedInClients.Count < totalConnected)
        {
            return;
        }

        AssignRolesAndTeamsToPlayers();
        BroadcastRoster();
        StartGameSceneLoad();
    }

    private void AssignRolesAndTeamsToPlayers()
    {
        if (NetworkManager.Singleton == null) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;

            if (client.PlayerObject != null)
            {
                if (_selectedRoles.TryGetValue(clientId, out PlayerRoleType role))
                {
                    NetworkPlayerController controller = client.PlayerObject.GetComponent<NetworkPlayerController>();
                    if (controller != null)
                    {
                        controller.SetInitialRole(role);
                    }
                }

                if (_selectedTeams.TryGetValue(clientId, out Team team))
                {
                    PlayerTeam playerTeam = client.PlayerObject.GetComponent<PlayerTeam>();
                    if (playerTeam == null) playerTeam = client.PlayerObject.GetComponentInChildren<PlayerTeam>();
                    if (playerTeam != null)
                    {
                        playerTeam.CurrentTeam.Value = team;
                    }
                }
            }
        }
    }

    private void BroadcastRoster()
    {
        if (!IsServer) return;

        List<string> names = new List<string>();
        List<byte> teams = new List<byte>();
        List<byte> roles = new List<byte>();

        if (CustomLobbyManager.Instance != null && CustomLobbyManager.Instance.Players.Count > 0)
        {
            for (int i = 0; i < CustomLobbyManager.Instance.Players.Count; i++)
            {
                LobbyPlayerData p = CustomLobbyManager.Instance.Players[i];
                names.Add(p.PlayerName.ToString());
                teams.Add((byte)p.AssignedTeam);

                PlayerRoleType role = _selectedRoles.TryGetValue(p.ClientId, out var r) ? r : PlayerRoleType.Assault;
                roles.Add((byte)role);
            }
        }
        else if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                names.Add($"Player {client.ClientId + 1}");
                Team t = _selectedTeams.TryGetValue(client.ClientId, out var team) ? team : Team.Red;
                teams.Add((byte)t);
                PlayerRoleType r = _selectedRoles.TryGetValue(client.ClientId, out var role) ? role : PlayerRoleType.Assault;
                roles.Add((byte)r);
            }
        }

        string namesPayload = string.Join("|", names);
        SetLoadingScreenRosterClientRpc(namesPayload, teams.ToArray(), roles.ToArray());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetLoadingScreenRosterClientRpc(string namesPayload, byte[] teams, byte[] roles)
    {
        if (LoadingScreen.Instance != null)
        {
            string[] names = string.IsNullOrEmpty(namesPayload) ? new string[0] : namesPayload.Split('|');
            LoadingScreen.Instance.SetRosterData(names, teams, roles);
        }
    }

    public void StartGameSceneLoad()
    {
        if (!IsServer) return;

        string sceneToLoad = CustomLobbyManager.Instance != null ? CustomLobbyManager.Instance.GameSceneName : "RUSPOINT";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
            }
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
    }
}