using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance { get; private set; }

    [SerializeField] private Vector3 _spectatorOffset = new Vector3(0f, 1.8f, -2.5f);
    [SerializeField] private float _followSpeed = 15f;

    private readonly List<NetworkPlayerController> _spectatableTeammates = new List<NetworkPlayerController>();
    private NetworkPlayerController _currentSpectatedPlayer;
    private Camera _localCamera;
    private Transform _originalCameraParent;
    private float _originalLocalY;
    private int _spectatedIndex = 0;
    private bool _isSpectating = false;
    private Team _localTeam = Team.Neutral;

    public bool IsSpectating => _isSpectating;
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

        RefreshTeammatesList();

        if (_spectatableTeammates.Count > 0)
        {
            _spectatedIndex = 0;
            _currentSpectatedPlayer = _spectatableTeammates[_spectatedIndex];
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
        _spectatableTeammates.Clear();

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
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CycleNextTeammate(1);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CycleNextTeammate(-1);
        }
    }

    private void RefreshTeammatesList()
    {
        _spectatableTeammates.Clear();

        NetworkPlayerController[] allPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsInactive.Exclude);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            NetworkPlayerController candidate = allPlayers[i];
            if (candidate.IsOwner) continue;

            PlayerTeam candidateTeam = candidate.GetComponent<PlayerTeam>();
            NetworkHealth candidateHealth = candidate.GetComponent<NetworkHealth>();

            if (candidateTeam == null || candidateHealth == null) continue;
            if (candidateTeam.CurrentTeam.Value != _localTeam) continue;
            if (!candidateHealth.IsAlive.Value || candidateHealth.CurrentHealth.Value <= 0f) continue;

            _spectatableTeammates.Add(candidate);
        }
    }

    private void CycleNextTeammate(int direction)
    {
        RefreshTeammatesList();

        if (_spectatableTeammates.Count == 0)
        {
            _currentSpectatedPlayer = null;
            return;
        }

        _spectatedIndex = (_spectatedIndex + direction + _spectatableTeammates.Count) % _spectatableTeammates.Count;
        _currentSpectatedPlayer = _spectatableTeammates[_spectatedIndex];
    }

    private void UpdateCameraFollow()
    {
        if (_currentSpectatedPlayer == null)
        {
            RefreshTeammatesList();
            if (_spectatableTeammates.Count > 0)
            {
                _spectatedIndex = 0;
                _currentSpectatedPlayer = _spectatableTeammates[_spectatedIndex];
            }
            return;
        }

        NetworkHealth targetHealth = _currentSpectatedPlayer.GetComponent<NetworkHealth>();
        if (targetHealth == null || !targetHealth.IsAlive.Value || targetHealth.CurrentHealth.Value <= 0f)
        {
            CycleNextTeammate(1);
            return;
        }

        if (_localCamera != null)
        {
            Transform targetTransform = _currentSpectatedPlayer.transform;
            Vector3 targetPosition = targetTransform.position + (targetTransform.rotation * _spectatorOffset);
            Quaternion targetRotation = Quaternion.LookRotation(targetTransform.position + Vector3.up * 1.5f - targetPosition);

            _localCamera.transform.position = Vector3.Lerp(_localCamera.transform.position, targetPosition, Time.deltaTime * _followSpeed);
            _localCamera.transform.rotation = Quaternion.Slerp(_localCamera.transform.rotation, targetRotation, Time.deltaTime * _followSpeed);
        }
    }
}