using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class TacticalChatManager : NetworkBehaviour
{
    private const int MAX_MESSAGE_HISTORY = 10;
    private const string DEFAULT_PLAYER_NAME_PREFIX = "Player_";
    private const string PLAYER_PREFS_NAME_KEY = "PlayerUsername";
    private const string TERRORIST_COLOR_HEX = "#FFA500";
    private const string COUNTER_TERRORIST_COLOR_HEX = "#4DA6FF";
    private const string DEAD_COLOR_HEX = "#FF4444";
    private const string MESSAGE_COLOR_HEX = "#FFFFFF";

    public static TacticalChatManager Instance { get; private set; }

    [SerializeField] private GameObject chatInputContainer;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI channelPromptText;
    [SerializeField] private TextMeshProUGUI chatLogText;
    [SerializeField] private float chatLogDisplayDuration = 6f;

    public string localPlayerName = string.Empty;
    public Team localTeam = Team.Blue;
    public bool isLocalPlayerDead;

    private bool _isChatOpen;
    private bool _isTeamChatOnly;
    private readonly List<string> _messageHistory = new List<string>();
    private Coroutine _hideChatLogCoroutine;

    private InputAction _globalChatAction;
    private InputAction _teamChatAction;
    private InputAction _toggleChannelAction;
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

        InitializeInputActions();
        InitializeUserInterface();
    }

    private void Start()
    {
        ResolveLocalPlayerProfile();
    }

    private void OnEnable()
    {
        EnableInputActions();
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        DisposeInputActions();
    }

    private void InitializeUserInterface()
    {
        if (chatInputContainer != null)
        {
            chatInputContainer.SetActive(false);
        }

        if (chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }
    }

    private void InitializeInputActions()
    {
        _globalChatAction = new InputAction(name: "GlobalChat", type: InputActionType.Button, binding: "<Keyboard>/y");
        _teamChatAction = new InputAction(name: "TeamChat", type: InputActionType.Button, binding: "<Keyboard>/t");
        _toggleChannelAction = new InputAction(name: "ToggleChannel", type: InputActionType.Button, binding: "<Keyboard>/tab");

        _sendMessageAction = new InputAction(name: "SendChatMessage", type: InputActionType.Button);
        _sendMessageAction.AddBinding("<Keyboard>/enter");
        _sendMessageAction.AddBinding("<Keyboard>/numpadEnter");

        _closeChatAction = new InputAction(name: "CloseChat", type: InputActionType.Button, binding: "<Keyboard>/escape");
    }

    private void EnableInputActions()
    {
        if (_globalChatAction != null)
        {
            _globalChatAction.performed += HandleGlobalChatPerformed;
            _globalChatAction.Enable();
        }

        if (_teamChatAction != null)
        {
            _teamChatAction.performed += HandleTeamChatPerformed;
            _teamChatAction.Enable();
        }

        if (_toggleChannelAction != null)
        {
            _toggleChannelAction.performed += HandleToggleChannelPerformed;
            _toggleChannelAction.Enable();
        }

        if (_sendMessageAction != null)
        {
            _sendMessageAction.performed += HandleSendMessagePerformed;
            _sendMessageAction.Enable();
        }

        if (_closeChatAction != null)
        {
            _closeChatAction.performed += HandleCloseChatPerformed;
            _closeChatAction.Enable();
        }
    }

    private void DisableInputActions()
    {
        if (_globalChatAction != null)
        {
            _globalChatAction.performed -= HandleGlobalChatPerformed;
            _globalChatAction.Disable();
        }

        if (_teamChatAction != null)
        {
            _teamChatAction.performed -= HandleTeamChatPerformed;
            _teamChatAction.Disable();
        }

        if (_toggleChannelAction != null)
        {
            _toggleChannelAction.performed -= HandleToggleChannelPerformed;
            _toggleChannelAction.Disable();
        }

        if (_sendMessageAction != null)
        {
            _sendMessageAction.performed -= HandleSendMessagePerformed;
            _sendMessageAction.Disable();
        }

        if (_closeChatAction != null)
        {
            _closeChatAction.performed -= HandleCloseChatPerformed;
            _closeChatAction.Disable();
        }
    }

    private void DisposeInputActions()
    {
        _globalChatAction?.Dispose();
        _teamChatAction?.Dispose();
        _toggleChannelAction?.Dispose();
        _sendMessageAction?.Dispose();
        _closeChatAction?.Dispose();
    }

    private void HandleGlobalChatPerformed(InputAction.CallbackContext context)
    {
        if (_isChatOpen || IsPauseMenuActive())
        {
            return;
        }

        OpenChat(false);
    }

    private void HandleTeamChatPerformed(InputAction.CallbackContext context)
    {
        if (_isChatOpen || IsPauseMenuActive())
        {
            return;
        }

        OpenChat(true);
    }

    private void HandleToggleChannelPerformed(InputAction.CallbackContext context)
    {
        if (!_isChatOpen)
        {
            return;
        }

        _isTeamChatOnly = !_isTeamChatOnly;
        UpdateChannelPromptUI();
    }

    private void HandleSendMessagePerformed(InputAction.CallbackContext context)
    {
        if (!_isChatOpen)
        {
            return;
        }

        SendCurrentMessage();
    }

    private void HandleCloseChatPerformed(InputAction.CallbackContext context)
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

        ResolveLocalPlayerProfile();

        _isChatOpen = true;
        _isTeamChatOnly = teamOnly;

        if (chatInputContainer != null)
        {
            chatInputContainer.SetActive(true);
        }

        ShowChatLog();
        StopHideChatLogCoroutine();
        UpdateChannelPromptUI();

        if (CursorStateManager.Instance != null)
        {
            CursorStateManager.Instance.RegisterCursorUnlockRequester();
        }

        SetPlayerMovementInputLock(true);

        if (chatInputField != null)
        {
            chatInputField.text = string.Empty;
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    public void CloseChat()
    {
        _isChatOpen = false;

        if (chatInputField != null)
        {
            chatInputField.DeactivateInputField();
        }

        if (chatInputContainer != null)
        {
            chatInputContainer.SetActive(false);
        }

        if (CursorStateManager.Instance != null)
        {
            CursorStateManager.Instance.UnregisterCursorUnlockRequester();
        }

        if (!IsPauseMenuActive())
        {
            SetPlayerMovementInputLock(false);
        }

        ResetHideTimer();
    }

    private void UpdateChannelPromptUI()
    {
        if (channelPromptText == null)
        {
            return;
        }

        channelPromptText.text = _isTeamChatOnly
            ? "<color=#FFFF00>[TEAM (TAB)]:</color>"
            : "<color=#FFFFFF>[ALL (TAB)]:</color>";
    }

    private void SendCurrentMessage()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
        {
            CloseChat();
            return;
        }

        string rawMessage = chatInputField.text.Trim();

        ResolveLocalPlayerProfile();

        SendMessageServerRpc(
            localPlayerName,
            localTeam,
            isLocalPlayerDead,
            _isTeamChatOnly,
            rawMessage
        );

        CloseChat();
    }

    private void ResolveLocalPlayerProfile()
    {
        if (string.IsNullOrEmpty(localPlayerName))
        {
            if (PlayerPrefs.HasKey(PLAYER_PREFS_NAME_KEY))
            {
                localPlayerName = PlayerPrefs.GetString(PLAYER_PREFS_NAME_KEY);
            }
            else if (NetworkManager.Singleton != null)
            {
                localPlayerName = string.Concat(DEFAULT_PLAYER_NAME_PREFIX, NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                localPlayerName = string.Concat(DEFAULT_PLAYER_NAME_PREFIX, Random.Range(100, 999));
            }
        }

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
        {
            return;
        }

        NetworkObject localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject == null)
        {
            return;
        }

        PlayerTeam playerTeamComponent = localPlayerObject.GetComponent<PlayerTeam>();
        if (playerTeamComponent != null)
        {
            localTeam = playerTeamComponent.CurrentTeam.Value;
        }

        NetworkHealth healthComponent = localPlayerObject.GetComponent<NetworkHealth>();
        if (healthComponent != null)
        {
            isLocalPlayerDead = healthComponent.CurrentHealth.Value <= 0f;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendMessageServerRpc(
        string senderName,
        Team senderTeam,
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
        Team senderTeam,
        bool isDead,
        bool isTeamOnly,
        string message)
    {
        ResolveLocalPlayerProfile();

        if (isTeamOnly && senderTeam != localTeam)
        {
            return;
        }

        string teamColorHex = senderTeam == Team.Red ? TERRORIST_COLOR_HEX : COUNTER_TERRORIST_COLOR_HEX;
        string teamPrefix = senderTeam == Team.Red ? "(T)" : "(CT)";
        string deadPrefix = isDead ? string.Concat("<color=", DEAD_COLOR_HEX, ">*DEAD* </color>") : string.Empty;

        string formattedLine;
        if (isTeamOnly)
        {
            formattedLine = string.Concat(deadPrefix, "<color=", teamColorHex, ">[TEAM] ", teamPrefix, " ", senderName, "</color>: <color=", MESSAGE_COLOR_HEX, ">", message, "</color>");
        }
        else
        {
            formattedLine = string.Concat(deadPrefix, "<color=", teamColorHex, ">[ALL] ", teamPrefix, " ", senderName, "</color>: <color=", MESSAGE_COLOR_HEX, ">", message, "</color>");
        }

        _messageHistory.Add(formattedLine);

        if (_messageHistory.Count > MAX_MESSAGE_HISTORY)
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
        if (chatLogText == null)
        {
            return;
        }

        chatLogText.text = string.Join("\n", _messageHistory);
    }

    private void ShowChatLog()
    {
        if (chatLogText == null)
        {
            return;
        }

        if (!chatLogText.gameObject.activeSelf)
        {
            chatLogText.gameObject.SetActive(true);
        }
    }

    private void ResetHideTimer()
    {
        StopHideChatLogCoroutine();
        _hideChatLogCoroutine = StartCoroutine(HideChatLogRoutine());
    }

    private void StopHideChatLogCoroutine()
    {
        if (_hideChatLogCoroutine != null)
        {
            StopCoroutine(_hideChatLogCoroutine);
            _hideChatLogCoroutine = null;
        }
    }

    private IEnumerator HideChatLogRoutine()
    {
        yield return new WaitForSeconds(chatLogDisplayDuration);

        if (!_isChatOpen && chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }

        _hideChatLogCoroutine = null;
    }

    private void SetPlayerMovementInputLock(bool locked)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject != null)
            {
                PlayerInput playerInput = localPlayerObject.GetComponent<PlayerInput>();
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
    }

    private bool IsPauseMenuActive()
    {
        return PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused;
    }
}