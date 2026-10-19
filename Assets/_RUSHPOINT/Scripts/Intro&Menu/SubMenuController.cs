using System.Collections;
using UnityEngine;

public class SubMenuController : MonoBehaviour
{
    public CanvasGroup subMenuCanvasGroup;
    public RectTransform[] buttonsToMove; 
    public float moveDistance = 150f; 
    public float animationSpeed = 12f;
    private bool _isExpanded = false;
    private Coroutine _animationRoutine;
    private float _currentProgress = 0f;
    private Vector2[] _originalPositions;

    private void Awake()
    {
        _originalPositions = new Vector2[buttonsToMove.Length];
        for (int i = 0; i < buttonsToMove.Length; i++)
        {
            _originalPositions[i] = buttonsToMove[i].anchoredPosition;
        }
        subMenuCanvasGroup.alpha = 0f;
        subMenuCanvasGroup.interactable = false;
        subMenuCanvasGroup.blocksRaycasts = false;
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

        subMenuCanvasGroup.interactable = _isExpanded;
        subMenuCanvasGroup.blocksRaycasts = _isExpanded;

        while (Mathf.Abs(_currentProgress - targetProgress) > 0.001f)
        {
            _currentProgress = Mathf.Lerp(_currentProgress, targetProgress, Time.deltaTime * animationSpeed);

            subMenuCanvasGroup.alpha = _currentProgress;
            for (int i = 0; i < buttonsToMove.Length; i++)
            {
                float newY = _originalPositions[i].y - (moveDistance * _currentProgress);
                buttonsToMove[i].anchoredPosition = new Vector2(_originalPositions[i].x, newY);
            }

            yield return null;
        }
        _currentProgress = targetProgress;
        subMenuCanvasGroup.alpha = _currentProgress;
        for (int i = 0; i < buttonsToMove.Length; i++)
        {
            float finalY = _originalPositions[i].y - (moveDistance * _currentProgress);
            buttonsToMove[i].anchoredPosition = new Vector2(_originalPositions[i].x, finalY);
        }
    }
}