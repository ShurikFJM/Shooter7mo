using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

public enum TeamSide
{
    Terrorist,
    CounterTerrorist
}

[RequireComponent(typeof(NetworkObject))]
public class TacticalChatManager : NetworkBehaviour
{
    public static TacticalChatManager Instance { get; private set; }

    [Header("UI Containers & Inputs")]
    [SerializeField] private GameObject _chatInputContainer;
    [SerializeField] private TMP_InputField _chatInputField;
    [SerializeField] private TextMeshProUGUI _channelPromptText;
    [SerializeField] private TextMeshProUGUI _chatLogText;

    [Header("Configuración")]
    [SerializeField] private float _chatLogDisplayDuration = 6f;
    private const int MaxMessageHistory = 10;

    [Header("Paleta de Colores")]
    [SerializeField] private string _tColorHex = "#FFA500";
    [SerializeField] private string _ctColorHex = "#4DA6FF";
    [SerializeField] private string _deadColorHex = "#FF4444";
    [SerializeField] private string _messageColorHex = "#FFFFFF";

    [Header("Datos Jugador Local")]
    public string localPlayerName = "Jugador";
    public TeamSide localTeam = TeamSide.CounterTerrorist;
    public bool isLocalPlayerDead;

    private bool _isChatOpen;
    private bool _isTeamChatOnly;

    private readonly List<string> _messageHistory = new();

    private Coroutine _hideChatLogCoroutine;

    private InputAction _globalChatAction;
    private InputAction _teamChatAction;
    private InputAction _sendMessageAction;
    private InputAction _closeChatAction;

    public bool IsChatOpen => _isChatOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ConfigureInputActions();
        ConfigureInitialUI();
    }

    private void OnEnable()
    {
        SubscribeToInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputEvents();
    }

    private void OnDestroy()
    {
        DisposeInputActions();
    }

    private void ConfigureInitialUI()
    {
        if (_chatInputContainer != null)
        {
            _chatInputContainer.SetActive(false);
        }

        if (_chatLogText != null)
        {
            _chatLogText.gameObject.SetActive(false);
        }
    }

    private void ConfigureInputActions()
    {
        _globalChatAction = new InputAction(
            name: "GlobalChat",
            type: InputActionType.Button,
            binding: "<Keyboard>/y"
        );

        _teamChatAction = new InputAction(
            name: "TeamChat",
            type: InputActionType.Button,
            binding: "<Keyboard>/t"
        );

        _sendMessageAction = new InputAction(
            name: "SendChatMessage",
            type: InputActionType.Button
        );

        _sendMessageAction.AddBinding("<Keyboard>/enter");
        _sendMessageAction.AddBinding("<Keyboard>/numpadEnter");

        _closeChatAction = new InputAction(
            name: "CloseChat",
            type: InputActionType.Button,
            binding: "<Keyboard>/escape"
        );
    }

    private void SubscribeToInputEvents()
    {
        if (_globalChatAction != null)
        {
            _globalChatAction.performed += OnGlobalChatPerformed;
            _globalChatAction.Enable();
        }

        if (_teamChatAction != null)
        {
            _teamChatAction.performed += OnTeamChatPerformed;
            _teamChatAction.Enable();
        }

        if (_sendMessageAction != null)
        {
            _sendMessageAction.performed += OnSendMessagePerformed;
            _sendMessageAction.Enable();
        }

        if (_closeChatAction != null)
        {
            _closeChatAction.performed += OnCloseChatPerformed;
            _closeChatAction.Enable();
        }
    }

    private void UnsubscribeFromInputEvents()
    {
        if (_globalChatAction != null)
        {
            _globalChatAction.performed -= OnGlobalChatPerformed;
            _globalChatAction.Disable();
        }

        if (_teamChatAction != null)
        {
            _teamChatAction.performed -= OnTeamChatPerformed;
            _teamChatAction.Disable();
        }

        if (_sendMessageAction != null)
        {
            _sendMessageAction.performed -= OnSendMessagePerformed;
            _sendMessageAction.Disable();
        }

        if (_closeChatAction != null)
        {
            _closeChatAction.performed -= OnCloseChatPerformed;
            _closeChatAction.Disable();
        }
    }

    private void DisposeInputActions()
    {
        _globalChatAction?.Dispose();
        _teamChatAction?.Dispose();
        _sendMessageAction?.Dispose();
        _closeChatAction?.Dispose();
    }

    private void OnGlobalChatPerformed(InputAction.CallbackContext context)
    {
        if (_isChatOpen)
        {
            return;
        }

        if (IsPauseMenuActive())
        {
            return;
        }

        OpenChat(false);
    }

    private void OnTeamChatPerformed(InputAction.CallbackContext context)
    {
        if (_isChatOpen)
        {
            return;
        }

        if (IsPauseMenuActive())
        {
            return;
        }

        OpenChat(true);
    }

    private void OnSendMessagePerformed(InputAction.CallbackContext context)
    {
        if (!_isChatOpen)
        {
            return;
        }

        SendCurrentMessage();
    }

    private void OnCloseChatPerformed(InputAction.CallbackContext context)
    {
        if (!_isChatOpen)
        {
            return;
        }

        CloseChat();
    }

    public void OpenChat(bool teamOnly)
    {
        if (IsPauseMenuActive())
        {
            return;
        }

        _isChatOpen = true;
        _isTeamChatOnly = teamOnly;

        if (_chatInputContainer != null)
        {
            _chatInputContainer.SetActive(true);
        }

        ShowChatLog();

        StopHideChatLogCoroutine();

        if (_channelPromptText != null)
        {
            _channelPromptText.text = teamOnly
                ? "<color=#FFFF00>[EQUIPO]:</color>"
                : "<color=#FFFFFF>[TODOS]:</color>";
        }

        SetPlayerInputLock(true);

        if (_chatInputField != null)
        {
            _chatInputField.text = string.Empty;
            _chatInputField.Select();
            _chatInputField.ActivateInputField();
        }
    }

    public void CloseChat()
    {
        _isChatOpen = false;

        if (_chatInputField != null)
        {
            _chatInputField.DeactivateInputField();
        }

        if (_chatInputContainer != null)
        {
            _chatInputContainer.SetActive(false);
        }

        if (!IsPauseMenuActive())
        {
            SetPlayerInputLock(false);
        }

        ResetHideTimer();
    }

    private void SendCurrentMessage()
    {
        if (_chatInputField == null || string.IsNullOrWhiteSpace(_chatInputField.text))
        {
            CloseChat();
            return;
        }

        string rawText = _chatInputField.text.Trim();

        CheckLocalPlayerDeathState();

        SendMessageServerRpc(
            localPlayerName,
            localTeam,
            isLocalPlayerDead,
            _isTeamChatOnly,
            rawText
        );

        CloseChat();
    }

    private void CheckLocalPlayerDeathState()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.SpawnManager == null)
        {
            return;
        }

        NetworkObject localPlayer =
            NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

        if (localPlayer == null)
        {
            return;
        }

        NetworkHealth health = localPlayer.GetComponent<NetworkHealth>();

        if (health == null)
        {
            return;
        }

        isLocalPlayerDead = health.CurrentHealth.Value <= 0;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(
        string senderName,
        TeamSide senderTeam,
        bool isDead,
        bool isTeamOnly,
        string message)
    {
        ReceiveMessageClientRpc(
            senderName,
            senderTeam,
            isDead,
            isTeamOnly,
            message
        );
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(
        string senderName,
        TeamSide senderTeam,
        bool isDead,
        bool isTeamOnly,
        string message)
    {
        if (isTeamOnly && senderTeam != localTeam)
        {
            return;
        }

        string teamColor = senderTeam == TeamSide.Terrorist
            ? _tColorHex
            : _ctColorHex;

        string teamLabel = senderTeam == TeamSide.Terrorist
            ? "(Equipo - T)"
            : "(Equipo - CT)";

        string formattedLine = string.Empty;

        if (isDead)
        {
            formattedLine +=
                $"<color={_deadColorHex}>*MUERTO*</color> ";
        }

        if (isTeamOnly)
        {
            formattedLine +=
                $"<color={teamColor}>{teamLabel} {senderName}</color>: " +
                $"<color={_messageColorHex}>{message}</color>";
        }
        else
        {
            formattedLine +=
                $"<color={teamColor}>{senderName}</color>: " +
                $"<color={_messageColorHex}>{message}</color>";
        }

        _messageHistory.Add(formattedLine);

        if (_messageHistory.Count > MaxMessageHistory)
        {
            _messageHistory.RemoveAt(0);
        }

        UpdateChatUI();
        ShowChatLog();

        if (!_isChatOpen)
        {
            ResetHideTimer();
        }
    }

    private void UpdateChatUI()
    {
        if (_chatLogText == null)
        {
            return;
        }

        _chatLogText.text = string.Join("\n", _messageHistory);
    }

    private void ShowChatLog()
    {
        if (_chatLogText == null)
        {
            return;
        }

        if (!_chatLogText.gameObject.activeSelf)
        {
            _chatLogText.gameObject.SetActive(true);
        }
    }

    private void ResetHideTimer()
    {
        StopHideChatLogCoroutine();

        _hideChatLogCoroutine = StartCoroutine(HideChatLogRoutine());
    }

    private void StopHideChatLogCoroutine()
    {
        if (_hideChatLogCoroutine == null)
        {
            return;
        }

        StopCoroutine(_hideChatLogCoroutine);
        _hideChatLogCoroutine = null;
    }

    private IEnumerator HideChatLogRoutine()
    {
        yield return new WaitForSeconds(_chatLogDisplayDuration);

        if (!_isChatOpen && _chatLogText != null)
        {
            _chatLogText.gameObject.SetActive(false);
        }

        _hideChatLogCoroutine = null;
    }

    private void SetPlayerInputLock(bool locked)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer =
                NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            if (localPlayer != null)
            {
                PlayerInput playerInput =
                    localPlayer.GetComponent<PlayerInput>();

                if (playerInput != null)
                {
                    if (locked)
                    {
                        playerInput.DeactivateInput();
                    }
                    else
                    {
                        playerInput.ActivateInput();
                    }
                }
            }
        }

        Cursor.lockState = locked
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = locked;
    }

    private bool IsPauseMenuActive()
    {
        return PauseMenuManager.Instance != null &&
               PauseMenuManager.Instance.IsPaused;
    }
}