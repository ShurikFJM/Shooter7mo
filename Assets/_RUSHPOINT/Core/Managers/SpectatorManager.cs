using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    private const float BACKUP_HEAD_HEIGHT = 1.6f;

    public static SpectatorManager Instance { get; private set; }

    private readonly List<NetworkPlayerController> _spectatablePlayers = new List<NetworkPlayerController>();
    private NetworkPlayerController _currentSpectatedPlayer;
    private Camera _localCamera;
    private Transform _originalCameraParent;
    private float _originalLocalY;
    private int _spectatedIndex;
    private bool _isSpectating;
    private Team _localTeam = Team.Neutral;

    public bool IsSpectating => _isSpectating;
    public bool isSpectating => _isSpectating;
    public NetworkPlayerController CurrentSpectatedPlayer => _currentSpectatedPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void LateUpdate()
    {
        if (!_isSpectating) return;

        HandleInputCycle();
        UpdateCameraFollow();
    }

    public void StartSpectating(Team playerTeam, Camera playerCamera, Transform originalParent, float defaultY)
    {
        _localTeam = playerTeam;
        _localCamera = playerCamera;
        _originalCameraParent = originalParent;
        _originalLocalY = defaultY;
        _isSpectating = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                NetworkPlayerController localController = localPlayer.GetComponent<NetworkPlayerController>();
                if (localController != null)
                {
                    localController.SetFirstPersonVisibility(false);
                }
            }
        }

        RefreshPlayersList();

        if (_spectatablePlayers.Count > 0)
        {
            _spectatedIndex = 0;
            _currentSpectatedPlayer = _spectatablePlayers[_spectatedIndex];
        }
        else
        {
            _currentSpectatedPlayer = null;
        }
    }

    public void StopSpectating()
    {
        _isSpectating = false;
        _currentSpectatedPlayer = null;
        _spectatablePlayers.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                NetworkPlayerController localController = localPlayer.GetComponent<NetworkPlayerController>();
                if (localController != null)
                {
                    localController.SetFirstPersonVisibility(true);
                }
            }
        }

        ForceResetCamera();
    }

    public void ForceResetCamera()
    {
        if (_localCamera != null && _originalCameraParent != null)
        {
            _localCamera.transform.position = _originalCameraParent.position + (Vector3.up * _originalLocalY);
            _localCamera.transform.rotation = _originalCameraParent.rotation;
        }
    }

    private void HandleInputCycle()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) return;

        bool cycleNext = false;
        bool cyclePrev = false;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) cycleNext = true;
            if (Mouse.current.rightButton.wasPressedThisFrame) cyclePrev = true;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                cycleNext = true;
            }
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                cyclePrev = true;
            }
        }

        if (cycleNext) CycleNextPlayer(1);
        else if (cyclePrev) CycleNextPlayer(-1);
    }

    public void RefreshPlayersList()
    {
        _spectatablePlayers.Clear();

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        IReadOnlyList<NetworkClient> connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        for (int i = 0; i < connectedClients.Count; i++)
        {
            NetworkClient client = connectedClients[i];
            if (client.PlayerObject == null) continue;
            if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;

            NetworkPlayerController candidateController = client.PlayerObject.GetComponent<NetworkPlayerController>();
            PlayerTeam candidateTeam = client.PlayerObject.GetComponent<PlayerTeam>();
            if (candidateTeam == null) candidateTeam = client.PlayerObject.GetComponentInChildren<PlayerTeam>();

            NetworkHealth candidateHealth = client.PlayerObject.GetComponent<NetworkHealth>();
            if (candidateHealth == null) candidateHealth = client.PlayerObject.GetComponentInChildren<NetworkHealth>();

            if (candidateController == null || candidateTeam == null || candidateHealth == null) continue;

            if (_localTeam != Team.Neutral && candidateTeam.CurrentTeam.Value != _localTeam)
            {
                continue;
            }

            if (!candidateHealth.IsAlive.Value || candidateHealth.CurrentHealth.Value <= 0f)
            {
                continue;
            }

            _spectatablePlayers.Add(candidateController);
        }
    }

    public void CycleNextPlayer(int direction)
    {
        RefreshPlayersList();

        if (_spectatablePlayers.Count == 0)
        {
            _currentSpectatedPlayer = null;
            return;
        }

        _spectatedIndex = (_spectatedIndex + direction + _spectatablePlayers.Count) % _spectatablePlayers.Count;
        _currentSpectatedPlayer = _spectatablePlayers[_spectatedIndex];
    }

    private void UpdateCameraFollow()
    {
        if (_currentSpectatedPlayer == null)
        {
            RefreshPlayersList();
            if (_spectatablePlayers.Count > 0)
            {
                _spectatedIndex = 0;
                _currentSpectatedPlayer = _spectatablePlayers[_spectatedIndex];
            }
            return;
        }

        NetworkHealth targetHealth = _currentSpectatedPlayer.GetComponent<NetworkHealth>();
        if (targetHealth == null) targetHealth = _currentSpectatedPlayer.GetComponentInChildren<NetworkHealth>();

        if (targetHealth == null || !targetHealth.IsAlive.Value || targetHealth.CurrentHealth.Value <= 0f)
        {
            CycleNextPlayer(1);
            return;
        }

        if (_localCamera != null)
        {
            Camera targetCamera = _currentSpectatedPlayer.GetComponentInChildren<Camera>();

            if (targetCamera != null)
            {
                _localCamera.transform.position = targetCamera.transform.position;
                _localCamera.transform.rotation = targetCamera.transform.rotation;
            }
            else
            {
                _localCamera.transform.position = _currentSpectatedPlayer.transform.position + (Vector3.up * BACKUP_HEAD_HEIGHT);
                _localCamera.transform.rotation = _currentSpectatedPlayer.transform.rotation;
            }
        }
    }
}