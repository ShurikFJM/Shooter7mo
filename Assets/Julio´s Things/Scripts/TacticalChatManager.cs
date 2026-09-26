using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private GameObject chatInputContainer;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI channelPromptText;
    [SerializeField] private TextMeshProUGUI chatLogText;

    [Header("Configuración")]
    [SerializeField] private float chatLogDisplayDuration = 6f;
    private const int maxMessageHistory = 10;

    [Header("Paleta de Colores")]
    [SerializeField] private string tColorHex = "#FFA500";       // Naranja Terroristas
    [SerializeField] private string ctColorHex = "#4DA6FF";      // Azul Counter-Terrorists
    [SerializeField] private string deadColorHex = "#FF4444";    // Rojo muertos
    [SerializeField] private string messageColorHex = "#FFFFFF"; // Blanco texto general

    [Header("Datos Jugador Local")]
    public string localPlayerName = "Jugador";
    public TeamSide localTeam = TeamSide.CounterTerrorist;
    public bool isLocalPlayerDead = false;

    private bool isChatOpen = false;
    private bool isTeamChatOnly = false;
    private List<string> messageHistory = new List<string>();
    private Coroutine hideChatLogCoroutine;

    public bool IsChatOpen => isChatOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (chatInputContainer != null) chatInputContainer.SetActive(false);
    }

    private void Start()
    {
        if (chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // No abrir chat si el menú de pausa está activo
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            if (isChatOpen) CloseChat();
            return;
        }

        if (!isChatOpen)
        {
            // Y = Chat Global | T = Chat de Equipo
            if (Keyboard.current != null)
            {
                if (Keyboard.current.yKey.wasPressedThisFrame) OpenChat(teamOnly: false);
                if (Keyboard.current.tKey.wasPressedThisFrame) OpenChat(teamOnly: true);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Y)) OpenChat(teamOnly: false);
                if (Input.GetKeyDown(KeyCode.T)) OpenChat(teamOnly: true);
            }
        }
        else
        {
            // Enter para enviar, Escape para cancelar
            bool enterPressed = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
                                || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

            bool escPressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                              || Input.GetKeyDown(KeyCode.Escape);

            if (enterPressed)
            {
                SendCurrentMessage();
            }
            else if (escPressed)
            {
                CloseChat();
            }
        }
    }

    public void OpenChat(bool teamOnly)
    {
        isChatOpen = true;
        isTeamChatOnly = teamOnly;

        if (chatInputContainer != null) chatInputContainer.SetActive(true);

        ShowChatLog();
        if (hideChatLogCoroutine != null) StopCoroutine(hideChatLogCoroutine);

        if (channelPromptText != null)
        {
            channelPromptText.text = teamOnly ? "<color=#FFFF00>[EQUIPO]:</color>" : "<color=#FFFFFF>[TODOS]:</color>";
        }

        SetPlayerInputLock(true);

        if (chatInputField != null)
        {
            chatInputField.text = "";
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    public void CloseChat()
    {
        isChatOpen = false;

        if (chatInputField != null) chatInputField.DeactivateInputField();
        if (chatInputContainer != null) chatInputContainer.SetActive(false);

        // Si la pausa no está puesta, devolvemos el cursor y el control al jugador
        bool pauseActive = PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused;
        if (!pauseActive)
        {
            SetPlayerInputLock(false);
        }

        ResetHideTimer();
    }

    private void SendCurrentMessage()
    {
        if (chatInputField != null && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string rawText = chatInputField.text.Trim();

            // Revisar si el jugador local está muerto consultando su NetworkHealth
            CheckLocalPlayerDeathState();

            // Enviar mensaje al servidor para que lo replique
            SendMessageServerRpc(localPlayerName, localTeam, isLocalPlayerDead, isTeamChatOnly, rawText);
        }

        CloseChat();
    }

    private void CheckLocalPlayerDeathState()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                NetworkHealth health = localPlayer.GetComponent<NetworkHealth>();
                if (health != null)
                {
                    // Si la vida actual llegó a 0, se considera muerto
                    isLocalPlayerDead = health.CurrentHealth.Value <= 0;
                }
            }
        }
    }

    // --- RPCs DE RED ---

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string senderName, TeamSide senderTeam, bool isDead, bool isTeamOnly, string message)
    {
        ReceiveMessageClientRpc(senderName, senderTeam, isDead, isTeamOnly, message);
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(string senderName, TeamSide senderTeam, bool isDead, bool isTeamOnly, string message)
    {
        // Si el mensaje es solo de equipo y el receptor no es del mismo equipo, se ignora
        if (isTeamOnly && senderTeam != localTeam)
        {
            return;
        }

        string teamColor = (senderTeam == TeamSide.Terrorist) ? tColorHex : ctColorHex;
        string teamLabel = (senderTeam == TeamSide.Terrorist) ? "(Equipo - T)" : "(Equipo - CT)";
        string formattedLine = "";

        if (isDead)
        {
            formattedLine += $"<color={deadColorHex}>*MUERTO*</color> ";
        }

        if (isTeamOnly)
        {
            formattedLine += $"<color={teamColor}>{teamLabel} {senderName}</color>: <color={messageColorHex}>{message}</color>";
        }
        else
        {
            formattedLine += $"<color={teamColor}>{senderName}</color>: <color={messageColorHex}>{message}</color>";
        }

        messageHistory.Add(formattedLine);
        if (messageHistory.Count > maxMessageHistory)
        {
            messageHistory.RemoveAt(0);
        }

        UpdateChatUI();
        ShowChatLog();

        if (!isChatOpen)
        {
            ResetHideTimer();
        }
    }

    private void UpdateChatUI()
    {
        if (chatLogText == null) return;
        chatLogText.text = string.Join("\n", messageHistory);
    }

    private void ShowChatLog()
    {
        if (chatLogText != null && !chatLogText.gameObject.activeSelf)
        {
            chatLogText.gameObject.SetActive(true);
        }
    }

    private void ResetHideTimer()
    {
        if (hideChatLogCoroutine != null) StopCoroutine(hideChatLogCoroutine);
        hideChatLogCoroutine = StartCoroutine(HideChatLogRoutine());
    }

    private IEnumerator HideChatLogRoutine()
    {
        yield return new WaitForSeconds(chatLogDisplayDuration);
        if (!isChatOpen && chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }
    }

    private void SetPlayerInputLock(bool locked)
    {
        // Obtener al jugador local autoritativo sin depender de referencias estáticas de escena
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                PlayerInput pInput = localPlayer.GetComponent<PlayerInput>();
                if (pInput != null)
                {
                    if (locked) pInput.DeactivateInput();
                    else pInput.ActivateInput();
                }
            }
        }

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }
}