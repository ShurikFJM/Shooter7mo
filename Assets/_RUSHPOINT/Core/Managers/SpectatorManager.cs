using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance { get; private set; }

    private const float BACKUP_HEAD_HEIGHT = 1.6f;

    [Header("Configuración de Espectador")]
    [Tooltip("Permite alternar entre jugadores de ambos equipos (Rojo y Azul)")]
    [SerializeField] private bool _allowSpectateAllTeams = true;

    private readonly List<NetworkPlayerController> _spectatablePlayers = new List<NetworkPlayerController>();
    private NetworkPlayerController _currentSpectatedPlayer;
    private Camera _localCamera;
    private Transform _originalCameraParent;
    private float _originalLocalY;
    private int _spectatedIndex = 0;
    private bool _isSpectating = false;
    private Team _localTeam = Team.Neutral;

    public bool isSpectating => _isSpectating;
    public NetworkPlayerController currentSpectatedPlayer => _currentSpectatedPlayer;

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

        ForceResetCamera();
    }

    public void ForceResetCamera()
    {
        if (_localCamera != null && _originalCameraParent != null)
        {
            _localCamera.transform.position = _originalCameraParent.position + Vector3.up * _originalLocalY;
            _localCamera.transform.rotation = _originalCameraParent.rotation;
        }
    }

    private void HandleInputCycle()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen) return;

        bool cycleNext = false;
        bool cyclePrev = false;

        // Soporte para Mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) cycleNext = true;
            if (Mouse.current.rightButton.wasPressedThisFrame) cyclePrev = true;
        }

        // Soporte para Teclado (Flechas, A/D, Espacio)
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

        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;

        for (int i = 0; i < clients.Count; i++)
        {
            NetworkClient client = clients[i];
            if (client.PlayerObject == null) continue;
            if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;

            NetworkPlayerController candidate = client.PlayerObject.GetComponent<NetworkPlayerController>();
            PlayerTeam candidateTeam = client.PlayerObject.GetComponent<PlayerTeam>();
            NetworkHealth candidateHealth = client.PlayerObject.GetComponent<NetworkHealth>();

            if (candidate == null || candidateTeam == null || candidateHealth == null) continue;

            // Si _allowSpectateAllTeams está activo o somos Neutrales, se incluyen ambos bandos (Rojo y Azul)
            if (!_allowSpectateAllTeams && _localTeam != Team.Neutral && candidateTeam.CurrentTeam.Value != _localTeam)
            {
                continue;
            }

            // Solo añadir jugadores vivos
            if (!candidateHealth.IsAlive.Value || candidateHealth.CurrentHealth.Value <= 0f)
            {
                continue;
            }

            _spectatablePlayers.Add(candidate);
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
        if (targetHealth == null || !targetHealth.IsAlive.Value || targetHealth.CurrentHealth.Value <= 0f)
        {
            CycleNextPlayer(1);
            return;
        }

        if (_localCamera != null)
        {
            Camera targetCam = _currentSpectatedPlayer.GetComponentInChildren<Camera>();

            if (targetCam != null)
            {
                _localCamera.transform.position = targetCam.transform.position;
                _localCamera.transform.rotation = targetCam.transform.rotation;
            }
            else
            {
                _localCamera.transform.position = _currentSpectatedPlayer.transform.position + Vector3.up * BACKUP_HEAD_HEIGHT;
                _localCamera.transform.rotation = _currentSpectatedPlayer.transform.rotation;
            }
        }
    }
}