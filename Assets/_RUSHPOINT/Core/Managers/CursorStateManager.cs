using System;
using UnityEngine;

public class CursorStateManager : MonoBehaviour
{
    public static CursorStateManager Instance { get; private set; }

    public event Action<bool> OnCursorLockStateChanged;

    public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

    private int _unlockRequestCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterCursorUnlockRequester()
    {
        _unlockRequestCount++;
        EvaluateCursorState();
    }

    public void UnregisterCursorUnlockRequester()
    {
        _unlockRequestCount = Mathf.Max(0, _unlockRequestCount - 1);
        EvaluateCursorState();
    }

    public void ForceEvaluateState()
    {
        EvaluateCursorState();
    }

    private void EvaluateCursorState()
    {
        bool shouldUnlock = _unlockRequestCount > 0 || ShouldUnlockExternalSystems();

        if (shouldUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OnCursorLockStateChanged?.Invoke(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnCursorLockStateChanged?.Invoke(true);
        }
    }

    private bool ShouldUnlockExternalSystems()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            return true;
        }

        if (RoleSelectScreenUI.instance != null && RoleSelectScreenUI.instance.isRoleSelectionActive)
        {
            return true;
        }

        if (TacticalChatManager.Instance != null && TacticalChatManager.Instance.IsChatOpen)
        {
            return true;
        }

        return false;
    }
}