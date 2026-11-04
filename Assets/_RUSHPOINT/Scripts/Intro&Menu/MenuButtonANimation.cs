using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    
    public Image backgroundImage;
    public Image arrowImage;
    public TextMeshProUGUI buttonText;
    public TMP_FontAsset hoverFont;

    public Color normalTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color hoverTextColor = Color.black;
    public float hoverScale = 1.05f;
    public float animationSpeed = 12f;

    private TMP_FontAsset _normalFont;
    private Coroutine _animationRoutine;
    private Vector3 _originalScale;
    private float _currentProgress = 0f;
    private float _targetProgress = 0f;

    private bool _isHovered;
    private bool _isSelected;

    private void Awake()
    {
        if (buttonText != null)
        {
            _originalScale = buttonText.transform.localScale;
            _normalFont = buttonText.font;
        }
        ApplyVisualState(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        EvaluateHighlightState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        EvaluateHighlightState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        EvaluateHighlightState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        EvaluateHighlightState();
    }

    private void EvaluateHighlightState()
    {
        bool shouldHighlight = _isHovered || _isSelected;
        _targetProgress = shouldHighlight ? 1f : 0f;

        if (buttonText != null)
        {
            buttonText.font = (shouldHighlight && hoverFont != null) ? hoverFont : _normalFont;
        }

        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
        _animationRoutine = StartCoroutine(AnimateButton());
    }

    private IEnumerator AnimateButton()
    {
        while (Mathf.Abs(_currentProgress - _targetProgress) > 0.001f)
        {
            _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * animationSpeed);
            ApplyVisualState(_currentProgress);
            yield return null;
        }

        _currentProgress = _targetProgress;
        ApplyVisualState(_currentProgress);
    }

    private void ApplyVisualState(float progress)
    {
        SetAlpha(backgroundImage, progress);
        SetAlpha(arrowImage, progress);

        if (buttonText != null)
        {
            buttonText.color = Color.Lerp(normalTextColor, hoverTextColor, progress);
            float currentScale = Mathf.Lerp(1f, hoverScale, progress);
            buttonText.transform.localScale = _originalScale * currentScale;
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}