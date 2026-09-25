using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TeamSide
{
    Terrorist,
    CounterTerrorist
}

public class TacticalChatManager : MonoBehaviour
{
    public static TacticalChatManager Instance;

    public GameObject chatInputContainer;   
    public TMP_InputField chatInputField; 
    public TextMeshProUGUI channelPromptText;
    public TextMeshProUGUI chatLogText;     

    public float chatLogDisplayDuration = 5f;

    [Header("Datos del Jugador Local")]
    public string localPlayerName = "Jugador1";
    public TeamSide localTeam = TeamSide.CounterTerrorist;
    public bool isLocalPlayerDead = false;

    [Header("TutsiPOP")]
    public string tColorHex = "#FFA500";       
    public string ctColorHex = "#4DA6FF";     
    public string deadColorHex = "#FF4444";  
    public string messageColorHex = "#FFFFFF"; 

    public SimpleFPSController playerController;

    private bool isChatOpen = false;
    private bool isTeamChatOnly = false;
    private List<string> messageHistory = new List<string>();
    private const int maxMessageHistory = 10;

    private Coroutine hideChatLogCoroutine;

    void Awake()
    {
        Instance = this;
        if (chatInputContainer != null) chatInputContainer.SetActive(false);
    }

    void Start()
    {
        if (chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isChatOpen)
        {
            if (Input.GetKeyDown(KeyCode.Y)) OpenChat(teamOnly: false);
            if (Input.GetKeyDown(KeyCode.T)) OpenChat(teamOnly: true);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendCurrentMessage();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseChat();
            }
        }
    }

    void OpenChat(bool teamOnly)
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

    void CloseChat()
    {
        isChatOpen = false;

        if (chatInputField != null) chatInputField.DeactivateInputField();
        if (chatInputContainer != null) chatInputContainer.SetActive(false);

        SetPlayerInputLock(false);
        ResetHideTimer();
    }

    void SendCurrentMessage()
    {
        if (chatInputField != null && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string rawText = chatInputField.text.Trim();
            BroadcastMessage(localPlayerName, localTeam, isLocalPlayerDead, isTeamChatOnly, rawText);
        }

        CloseChat();
    }

    public void BroadcastMessage(string senderName, TeamSide team, bool isDead, bool isTeamOnly, string message)
    {
        string teamColor = (team == TeamSide.Terrorist) ? tColorHex : ctColorHex;
        string teamLabel = (team == TeamSide.Terrorist) ? "(Equipo - Terroristas)" : "(Equipo - Anti-Terroristas)";
        string formattedLine = "";

        if (isDead)
        {
            formattedLine += $"<color={deadColorHex}>*MUERTO*</color> ";
        }

        if (isTeamOnly)
        {
            formattedLine += $"<color={teamColor}>{teamLabel} {senderName}</color> : <color={messageColorHex}>{message}</color>";
        }
        else
        {
            formattedLine += $"<color={teamColor}>{senderName}</color> : <color={messageColorHex}>{message}</color>";
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

    void UpdateChatUI()
    {
        if (chatLogText == null) return;
        chatLogText.text = string.Join("\n", messageHistory);
    }

    void ShowChatLog()
    {
        if (chatLogText != null && !chatLogText.gameObject.activeSelf)
        {
            chatLogText.gameObject.SetActive(true);
        }
    }

    void ResetHideTimer()
    {
        if (hideChatLogCoroutine != null) StopCoroutine(hideChatLogCoroutine);
        hideChatLogCoroutine = StartCoroutine(HideChatLogRoutine());
    }

    IEnumerator HideChatLogRoutine()
    {
        yield return new WaitForSeconds(chatLogDisplayDuration);
        if (!isChatOpen && chatLogText != null)
        {
            chatLogText.gameObject.SetActive(false);
        }
    }

    void SetPlayerInputLock(bool locked)
    {
        if (playerController != null)
        {
            playerController.enabled = !locked;
        }

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }
}