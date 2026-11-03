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

        if (Instance == this) Instance = null;

        base.OnDestroy();
    }

    public void SelectRole(PlayerRoleType role)
    {
        _localSelectedRole = role;
    }

    public void ConfirmRoleSelection(PlayerRoleType role, Team team)
    {
        _localSelectedRole = role;

        if (_roleSelectCanvas != null)
        {
            _roleSelectCanvas.SetActive(false);
        }

        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.ShowLoadingScreen();
        }

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

        CustomLobbyManager.SavePlayerData(clientId, team, role);

        if (!_lockedInClients.Contains(clientId))
        {
            _lockedInClients.Add(clientId);
        }

        CheckAllActivePlayersLockedIn();
    }

    private void CheckAllActivePlayersLockedIn()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;

        int requiredPlayers = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;

            Team assignedTeam = Team.Red;
            if (CustomLobbyManager.Instance != null)
            {
                assignedTeam = CustomLobbyManager.Instance.GetPlayerAssignedTeam(clientId);
            }

            if (assignedTeam != Team.Neutral)
            {
                requiredPlayers++;
                if (!_lockedInClients.Contains(clientId))
                {
                    return;
                }
            }
        }

        if (requiredPlayers > 0 || NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            StartGameSceneLoad();
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