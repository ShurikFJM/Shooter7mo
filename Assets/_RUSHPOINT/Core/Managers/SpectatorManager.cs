using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class SpectatorManager : MonoBehaviour
{
    private const float BACKUP_HEAD_HEIGHT = 1.6f;
    private const float THIRD_PERSON_DISTANCE = 2.8f;
    private const float THIRD_PERSON_HEIGHT = 1.8f;
    private const float THIRD_PERSON_RIGHT_OFFSET = 0.45f;
    private const float WALL_COLLISION_RADIUS = 0.25f;
    private const float CAMERA_SMOOTH_SPEED = 16f;
    private const float FALLBACK_REFRESH_INTERVAL = 0.25f;
    private const string LOBBY_CAMERA_TAG = "LobbyCamera";

    private static SpectatorManager _instance;
    public static SpectatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SpectatorManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SpectatorManager_Auto");
                    _instance = go.AddComponent<SpectatorManager>();
                }
            }
            return _instance;
        }
    }

    private readonly List<NetworkPlayerController> _spectatablePlayers = new List<NetworkPlayerController>(12);
    private readonly Dictionary<Renderer, ShadowCastingMode> _hiddenTargetRenderers = new Dictionary<Renderer, ShadowCastingMode>(16);
    private readonly RaycastHit[] _cameraCollisionHits = new RaycastHit[12];

    private NetworkPlayerController _currentSpectatedPlayer;
    private NetworkHealth _currentSpectatedHealth;
    private Camera _localCamera;
    private AudioListener _localAudioListener;
    private Camera _spectatorCamera;
    private AudioListener _spectatorAudioListener;
    private int _spectatedIndex;
    private bool _isSpectating;
    private bool _isFirstPersonMode;
    private Team _localTeam = Team.Neutral;
    private float _orbitYawOffset;
    private float _orbitPitch = 14f;
    private float _nextFallbackRefreshTime;

    private GUIStyle _cachedGuiStyle;
    private string _cachedSpectatorLabel = "Buscando jugadores vivos...";

    public bool IsSpectating => _isSpectating;
    public bool isSpectating => _isSpectating;
    public NetworkPlayerController CurrentSpectatedPlayer => _currentSpectatedPlayer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        RestoreHiddenTargetRenderers();

        if (_spectatorCamera != null)
        {
            Destroy(_spectatorCamera.gameObject);
            _spectatorCamera = null;
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void LateUpdate()
    {
        if (!_isSpectating) return;

        EnsureSpectatorCameraActive();
        HandleInputCycle();
        UpdateCameraFollow();
    }

    public void StartSpectating(Team playerTeam, Camera playerCamera, Transform originalParent, float defaultY)
    {
        if (!_isSpectating || playerTeam == Team.Neutral)
        {
            _localTeam = playerTeam;
        }

        if (playerCamera != null && playerCamera != _spectatorCamera)
        {
            _localCamera = playerCamera;
            _localAudioListener = playerCamera.GetComponent<AudioListener>();
        }

        _isSpectating = true;

        if (LoadingScreen.Instance != null && LoadingScreen.Instance.IsVisible)
        {
            LoadingScreen.Instance.Dismiss();
        }

        Camera lobbyCamera = GameObject.FindWithTag(LOBBY_CAMERA_TAG)?.GetComponent<Camera>();
        if (lobbyCamera != null) lobbyCamera.gameObject.SetActive(false);

        EnsureSpectatorCameraCreated();
        SetLocalCameraActive(false);
        EnsureSpectatorCameraActive();
        AssignCanvasesToCamera(_spectatorCamera);

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
            if (_currentSpectatedPlayer == null || !_spectatablePlayers.Contains(_currentSpectatedPlayer))
            {
                _spectatedIndex = 0;
                SelectTargetPlayer(_spectatablePlayers[_spectatedIndex]);
            }
        }
        else
        {
            SelectTargetPlayer(null);
            PositionOverviewFallback();
        }
    }

    public void StopSpectating()
    {
        _isSpectating = false;
        RestoreHiddenTargetRenderers();
        _currentSpectatedPlayer = null;
        _currentSpectatedHealth = null;
        _spectatablePlayers.Clear();

        if (_spectatorCamera != null)
        {
            _spectatorCamera.enabled = false;
            _spectatorCamera.gameObject.SetActive(false);
        }

        if (_spectatorAudioListener != null)
        {
            _spectatorAudioListener.enabled = false;
        }

        ForceResetCamera();

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
    }

    public void ForceResetCamera()
    {
        if (!_isSpectating)
        {
            SetLocalCameraActive(true);
            if (_localCamera != null)
            {
                AssignCanvasesToCamera(_localCamera);
            }
        }
    }

    private void EnsureSpectatorCameraCreated()
    {
        if (_spectatorCamera != null) return;

        GameObject camObj = new GameObject("SpectatorCamera_Active");
        _spectatorCamera = camObj.AddComponent<Camera>();
        _spectatorAudioListener = camObj.AddComponent<AudioListener>();

        if (_localCamera != null)
        {
            _spectatorCamera.fieldOfView = _localCamera.fieldOfView;
            _spectatorCamera.nearClipPlane = Mathf.Min(0.05f, _localCamera.nearClipPlane);
            _spectatorCamera.farClipPlane = Mathf.Max(500f, _localCamera.farClipPlane);
            _spectatorCamera.clearFlags = _localCamera.clearFlags;
            _spectatorCamera.backgroundColor = _localCamera.backgroundColor;
            _spectatorCamera.cullingMask = _localCamera.cullingMask != 0 ? _localCamera.cullingMask : ~0;
        }
        else
        {
            _spectatorCamera.fieldOfView = 70f;
            _spectatorCamera.nearClipPlane = 0.05f;
            _spectatorCamera.farClipPlane = 1000f;
            _spectatorCamera.clearFlags = CameraClearFlags.Skybox;
            _spectatorCamera.cullingMask = ~0;
        }

        _spectatorCamera.depth = 50f;
    }

    private void EnsureSpectatorCameraActive()
    {
        EnsureSpectatorCameraCreated();

        if (!_spectatorCamera.gameObject.activeSelf) _spectatorCamera.gameObject.SetActive(true);
        if (!_spectatorCamera.enabled) _spectatorCamera.enabled = true;
        if (_spectatorAudioListener != null && !_spectatorAudioListener.enabled) _spectatorAudioListener.enabled = true;

        SetLocalCameraActive(false);
    }

    private void SetLocalCameraActive(bool active)
    {
        if (_localCamera != null && _localCamera.enabled != active) _localCamera.enabled = active;
        if (_localAudioListener != null && _localAudioListener.enabled != active) _localAudioListener.enabled = active;
    }

    private void AssignCanvasesToCamera(Camera targetCam)
    {
        if (targetCam == null) return;

        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null && allCanvases[i].renderMode == RenderMode.ScreenSpaceCamera)
            {
                allCanvases[i].worldCamera = targetCam;
            }
        }
    }

    private void PositionOverviewFallback()
    {
        if (_spectatorCamera == null) return;

        Vector3 focusPoint = Vector3.zero;
        BombSite site = FindAnyObjectByType<BombSite>();
        if (site != null)
        {
            focusPoint = site.transform.position;
        }

        _spectatorCamera.transform.position = focusPoint + new Vector3(0f, 18f, -15f);
        _spectatorCamera.transform.LookAt(focusPoint + Vector3.up * 1.5f);
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

            if (!_isFirstPersonMode && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _orbitYawOffset += delta.x * 0.15f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - (delta.y * 0.15f), -20f, 65f);
            }
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
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                _isFirstPersonMode = !_isFirstPersonMode;
                UpdateTargetFirstPersonVisibility();
                RebuildSpectatorLabel();
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightShoulder.wasPressedThisFrame) cycleNext = true;
            if (Gamepad.current.leftShoulder.wasPressedThisFrame) cyclePrev = true;
            if (Gamepad.current.buttonNorth.wasPressedThisFrame)
            {
                _isFirstPersonMode = !_isFirstPersonMode;
                UpdateTargetFirstPersonVisibility();
                RebuildSpectatorLabel();
            }
        }

        if (cycleNext) CycleNextPlayer(1);
        else if (cyclePrev) CycleNextPlayer(-1);
    }

    public void RefreshPlayersList()
    {
        _spectatablePlayers.Clear();

        List<NetworkPlayerController> activeList = NetworkPlayerController.ActiveControllers;
        int count = activeList.Count;

        for (int i = 0; i < count; i++)
        {
            NetworkPlayerController candidateController = activeList[i];
            if (candidateController == null || !candidateController.gameObject.activeInHierarchy) continue;
            if (candidateController.IsOwner) continue;

            PlayerTeam candidateTeam = candidateController.GetComponent<PlayerTeam>();
            if (candidateTeam == null) candidateTeam = candidateController.GetComponentInChildren<PlayerTeam>(true);

            NetworkHealth candidateHealth = candidateController.GetComponent<NetworkHealth>();
            if (candidateHealth == null) candidateHealth = candidateController.GetComponentInChildren<NetworkHealth>(true);

            if (candidateTeam == null || candidateHealth == null) continue;
            if (candidateTeam.CurrentTeam.Value == Team.Neutral) continue;

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
            SelectTargetPlayer(null);
            return;
        }

        _spectatedIndex = (_spectatedIndex + direction + _spectatablePlayers.Count) % _spectatablePlayers.Count;
        SelectTargetPlayer(_spectatablePlayers[_spectatedIndex]);
    }

    private void SelectTargetPlayer(NetworkPlayerController newTarget)
    {
        if (_currentSpectatedPlayer == newTarget)
        {
            RebuildSpectatorLabel();
            return;
        }

        RestoreHiddenTargetRenderers();
        _currentSpectatedPlayer = newTarget;
        _currentSpectatedHealth = newTarget != null
            ? (newTarget.GetComponent<NetworkHealth>() ?? newTarget.GetComponentInChildren<NetworkHealth>(true))
            : null;

        _orbitYawOffset = 0f;
        _orbitPitch = 14f;
        UpdateTargetFirstPersonVisibility();
        RebuildSpectatorLabel();
    }

    private void RebuildSpectatorLabel()
    {
        if (_currentSpectatedPlayer == null)
        {
            _cachedSpectatorLabel = "Buscando jugadores vivos...";
            return;
        }

        PlayerTeam targetTeamComp = _currentSpectatedPlayer.GetComponent<PlayerTeam>();
        string teamName = targetTeamComp != null
            ? (targetTeamComp.CurrentTeam.Value == Team.Red ? "ATACANTES" : "DEFENSORES")
            : "JUGADOR";
        string modeLabel = _isFirstPersonMode ? "1ra Persona" : "3ra Persona";
        _cachedSpectatorLabel = $"ESPECTANDO: Player {_currentSpectatedPlayer.OwnerClientId + 1} ({teamName})  |  Vista: {modeLabel}  |  [Clic Izq/Der] Cambiar  |  [V] Camara";
    }

    private void UpdateTargetFirstPersonVisibility()
    {
        RestoreHiddenTargetRenderers();

        if (!_isFirstPersonMode || _currentSpectatedPlayer == null) return;

        Renderer[] renderers = _currentSpectatedPlayer.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (rend == null) continue;

            _hiddenTargetRenderers[rend] = rend.shadowCastingMode;
            rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    private void RestoreHiddenTargetRenderers()
    {
        foreach (KeyValuePair<Renderer, ShadowCastingMode> pair in _hiddenTargetRenderers)
        {
            if (pair.Key != null)
            {
                pair.Key.shadowCastingMode = pair.Value;
            }
        }

        _hiddenTargetRenderers.Clear();
    }

    private void UpdateCameraFollow()
    {
        if (_currentSpectatedPlayer == null || !_currentSpectatedPlayer.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= _nextFallbackRefreshTime)
            {
                _nextFallbackRefreshTime = Time.unscaledTime + FALLBACK_REFRESH_INTERVAL;
                RefreshPlayersList();
                if (_spectatablePlayers.Count > 0)
                {
                    _spectatedIndex = 0;
                    SelectTargetPlayer(_spectatablePlayers[_spectatedIndex]);
                }
                else
                {
                    PositionOverviewFallback();
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (_currentSpectatedHealth == null || !_currentSpectatedHealth.IsAlive.Value || _currentSpectatedHealth.CurrentHealth.Value <= 0f)
        {
            CycleNextPlayer(1);
            if (_currentSpectatedPlayer == null) PositionOverviewFallback();
            return;
        }

        if (_spectatorCamera == null) return;

        Transform targetRoot = _currentSpectatedPlayer.transform;
        Camera targetCamera = _currentSpectatedPlayer.PlayerCamera;

        if (_isFirstPersonMode)
        {
            if (targetCamera != null)
            {
                _spectatorCamera.transform.position = targetCamera.transform.position;
                _spectatorCamera.transform.rotation = targetCamera.transform.rotation;
            }
            else
            {
                _spectatorCamera.transform.position = targetRoot.position + (Vector3.up * BACKUP_HEAD_HEIGHT);
                _spectatorCamera.transform.rotation = targetRoot.rotation;
            }
        }
        else
        {
            Vector3 pivot = targetRoot.position + (Vector3.up * THIRD_PERSON_HEIGHT);
            float baseYaw = targetRoot.eulerAngles.y + _orbitYawOffset;
            Quaternion orbitRot = Quaternion.Euler(_orbitPitch, baseYaw, 0f);

            Vector3 desiredPos = pivot + (orbitRot * new Vector3(THIRD_PERSON_RIGHT_OFFSET, 0f, -THIRD_PERSON_DISTANCE));
            Vector3 dirToCam = desiredPos - pivot;
            float distToCam = dirToCam.magnitude;

            if (distToCam > 0.05f)
            {
                Vector3 dirNorm = dirToCam / distToCam;
                int hitCount = Physics.SphereCastNonAlloc(pivot, WALL_COLLISION_RADIUS, dirNorm, _cameraCollisionHits, distToCam, ~0, QueryTriggerInteraction.Ignore);
                float closestDist = distToCam;

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hitCol = _cameraCollisionHits[i].collider;
                    if (hitCol == null) continue;
                    if (hitCol.transform.IsChildOf(targetRoot) || hitCol.transform == targetRoot) continue;

                    if (_cameraCollisionHits[i].distance < closestDist)
                    {
                        closestDist = _cameraCollisionHits[i].distance;
                    }
                }

                desiredPos = pivot + (dirNorm * Mathf.Max(0.45f, closestDist - 0.1f));
            }

            float lerpStep = Time.deltaTime * CAMERA_SMOOTH_SPEED;
            _spectatorCamera.transform.position = Vector3.Lerp(_spectatorCamera.transform.position, desiredPos, lerpStep);
            _spectatorCamera.transform.rotation = Quaternion.Slerp(_spectatorCamera.transform.rotation, orbitRot, lerpStep);
        }
    }

    private void OnGUI()
    {
        if (!_isSpectating) return;

        if (_cachedGuiStyle == null)
        {
            _cachedGuiStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        float barWidth = Mathf.Min(Screen.width - 40f, 760f);
        Rect bgRect = new Rect((Screen.width - barWidth) * 0.5f, Screen.height - 62f, barWidth, 36f);

        Color prevColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.Box(bgRect, GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(bgRect, _cachedSpectatorLabel, _cachedGuiStyle);
        GUI.color = prevColor;
    }
}