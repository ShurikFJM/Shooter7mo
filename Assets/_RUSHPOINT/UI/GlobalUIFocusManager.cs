using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class MenuBackEntry
{
    public GameObject currentPanel;
    public GameObject previousPanel;
    public Button backButtonOverride;
}

[RequireComponent(typeof(EventSystem))]
public class GlobalUIFocusManager : MonoBehaviour
{
    private const float BACK_INPUT_COOLDOWN = 0.15f;

    private static readonly string[] AUTO_BACK_KEYWORDS = new string[]
    {
        "back",
        "volver",
        "regresar",
        "close",
        "cerrar",
        "cancel",
        "return"
    };

    [Header("Opcional: Rutas manuales de Volver")]
    [SerializeField] private MenuBackEntry[] _menuBackEntries;

    private EventSystem _eventSystem;
    private float _nextBackAllowedTime;

    private void Awake()
    {
        _eventSystem = GetComponent<EventSystem>();
    }

    private void Update()
    {
        if (_eventSystem == null) return;

        HandleBackNavigationInput();

        if (_eventSystem.currentSelectedGameObject != null &&
            _eventSystem.currentSelectedGameObject.activeInHierarchy)
        {
            Selectable currentSel = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
            if (currentSel != null && currentSel.IsInteractable()) return;
        }

        AssignFocusToFrontmostSelectable();
    }

    private void HandleBackNavigationInput()
    {
        if (Time.unscaledTime < _nextBackAllowedTime) return;
        if (Cursor.lockState == CursorLockMode.Locked) return;

        bool backPressed = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            backPressed = true;
        }

        if (!backPressed && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            backPressed = true;
        }

        if (!backPressed) return;

        GameObject selectedObj = _eventSystem.currentSelectedGameObject;
        if (selectedObj != null)
        {
            TMP_InputField tmpInput = selectedObj.GetComponent<TMP_InputField>();
            if (tmpInput != null && tmpInput.isFocused)
            {
                tmpInput.DeactivateInputField();
                _nextBackAllowedTime = Time.unscaledTime + BACK_INPUT_COOLDOWN;
                return;
            }

            InputField legacyInput = selectedObj.GetComponent<InputField>();
            if (legacyInput != null && legacyInput.isFocused)
            {
                legacyInput.DeactivateInputField();
                _nextBackAllowedTime = Time.unscaledTime + BACK_INPUT_COOLDOWN;
                return;
            }
        }

        if (TryExecuteConfiguredBack() || TryClickAutoBackButton())
        {
            _nextBackAllowedTime = Time.unscaledTime + BACK_INPUT_COOLDOWN;
            AssignFocusToFrontmostSelectable();
        }
    }

    private bool TryExecuteConfiguredBack()
    {
        if (_menuBackEntries == null || _menuBackEntries.Length == 0) return false;

        for (int i = 0; i < _menuBackEntries.Length; i++)
        {
            MenuBackEntry entry = _menuBackEntries[i];
            if (entry == null || entry.currentPanel == null || !entry.currentPanel.activeInHierarchy) continue;

            if (entry.backButtonOverride != null &&
                entry.backButtonOverride.gameObject.activeInHierarchy &&
                entry.backButtonOverride.IsInteractable())
            {
                entry.backButtonOverride.onClick.Invoke();
                return true;
            }

            entry.currentPanel.SetActive(false);
            if (entry.previousPanel != null)
            {
                entry.previousPanel.SetActive(true);
            }
            return true;
        }

        return false;
    }

    private bool TryClickAutoBackButton()
    {
        Selectable[] allSelectables = Selectable.allSelectablesArray;
        Button bestBackButton = null;
        int bestSortScore = int.MinValue;

        for (int i = 0; i < allSelectables.Length; i++)
        {
            Button btn = allSelectables[i] as Button;
            if (btn == null || !btn.gameObject.activeInHierarchy || !btn.IsInteractable()) continue;

            if (!IsBackButtonName(btn.name)) continue;

            Canvas parentCanvas = btn.GetComponentInParent<Canvas>();
            int sortOrder = parentCanvas != null ? parentCanvas.sortingOrder : 0;
            int score = (sortOrder * 1000) + btn.transform.GetSiblingIndex();

            if (score > bestSortScore)
            {
                bestSortScore = score;
                bestBackButton = btn;
            }
        }

        if (bestBackButton != null)
        {
            bestBackButton.onClick.Invoke();
            return true;
        }

        return false;
    }

    private static bool IsBackButtonName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        for (int i = 0; i < AUTO_BACK_KEYWORDS.Length; i++)
        {
            if (objectName.IndexOf(AUTO_BACK_KEYWORDS[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void AssignFocusToFrontmostSelectable()
    {
        Selectable[] allSelectables = Selectable.allSelectablesArray;
        Selectable bestCandidate = null;
        int highestSiblingIndex = -1;

        for (int i = 0; i < allSelectables.Length; i++)
        {
            Selectable item = allSelectables[i];

            if (item == null || !item.gameObject.activeInHierarchy || !item.IsInteractable()) continue;

            int hierarchyDepth = item.transform.GetSiblingIndex();
            if (hierarchyDepth > highestSiblingIndex || bestCandidate == null)
            {
                highestSiblingIndex = hierarchyDepth;
                bestCandidate = item;
            }
        }

        if (bestCandidate != null)
        {
            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(bestCandidate.gameObject);
        }
    }
}