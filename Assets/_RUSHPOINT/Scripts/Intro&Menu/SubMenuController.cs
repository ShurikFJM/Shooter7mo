using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SubMenuController : MonoBehaviour
{
    public CanvasGroup subMenuCanvasGroup;
    public RectTransform[] buttonsToMove;
    public float moveDistance = 150f;
    public float animationSpeed = 12f;
    public Button toggleButton;

    private bool _isExpanded = false;
    private Coroutine _animationRoutine;
    private float _currentProgress = 0f;
    private Vector2[] _originalPositions;

    private void Awake()
    {
        if (buttonsToMove != null)
        {
            _originalPositions = new Vector2[buttonsToMove.Length];
            for (int i = 0; i < buttonsToMove.Length; i++)
            {
                if (buttonsToMove[i] != null)
                {
                    _originalPositions[i] = buttonsToMove[i].anchoredPosition;
                }
            }
        }

        if (subMenuCanvasGroup != null)
        {
            subMenuCanvasGroup.alpha = 0f;
            subMenuCanvasGroup.interactable = false;
            subMenuCanvasGroup.blocksRaycasts = false;
        }

        if (toggleButton == null)
        {
            toggleButton = GetComponent<Button>();
        }
    }

    public void ToggleMenu()
    {
        _isExpanded = !_isExpanded;

        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
        _animationRoutine = StartCoroutine(AnimateSubMenu());
    }

    private IEnumerator AnimateSubMenu()
    {
        float targetProgress = _isExpanded ? 1f : 0f;

        if (subMenuCanvasGroup != null)
        {
            subMenuCanvasGroup.interactable = _isExpanded;
            subMenuCanvasGroup.blocksRaycasts = _isExpanded;
        }

        while (Mathf.Abs(_currentProgress - targetProgress) > 0.001f)
        {
            _currentProgress = Mathf.Lerp(_currentProgress, targetProgress, Time.deltaTime * animationSpeed);

            if (subMenuCanvasGroup != null) subMenuCanvasGroup.alpha = _currentProgress;
            UpdateButtonsPosition(_currentProgress);

            yield return null;
        }

        _currentProgress = targetProgress;
        if (subMenuCanvasGroup != null) subMenuCanvasGroup.alpha = _currentProgress;
        UpdateButtonsPosition(_currentProgress);

        if (_isExpanded)
        {
            FocusFirstSubMenuButton();
        }
        else if (toggleButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(toggleButton.gameObject);
        }
    }

    private void UpdateButtonsPosition(float progress)
    {
        if (buttonsToMove == null || _originalPositions == null) return;

        for (int i = 0; i < buttonsToMove.Length; i++)
        {
            if (buttonsToMove[i] != null && i < _originalPositions.Length)
            {
                float newY = _originalPositions[i].y - (moveDistance * progress);
                buttonsToMove[i].anchoredPosition = new Vector2(_originalPositions[i].x, newY);
            }
        }
    }

    private void FocusFirstSubMenuButton()
    {
        if (subMenuCanvasGroup == null || EventSystem.current == null) return;

        Button[] childButtons = subMenuCanvasGroup.GetComponentsInChildren<Button>();
        for (int i = 0; i < childButtons.Length; i++)
        {
            if (childButtons[i] != null && childButtons[i].interactable && childButtons[i].gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(childButtons[i].gameObject);
                return;
            }
        }
    }
}