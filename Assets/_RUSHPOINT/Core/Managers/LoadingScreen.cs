using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class LoadingScreen : MonoBehaviour
{
    private const string DEFAULT_MAP_NAME = "HEAVEN";
    private const string DEFAULT_MODE_NAME = "SEARCH & DESTROY";
    private const string TARGET_SCENE_NAME = "RUSPOINT";
    private const float MAX_DURATION_TIMEOUT = 3.5f;
    private const float DISMISS_HOLD_DELAY = 0.15f;
    private const float SAFETY_CLEANUP_DELAY = 0.8f;

    public static LoadingScreen Instance { get; private set; }

    [SerializeField] private GameObject _loadingScreenRoot;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _mapNameText;
    [SerializeField] private TMP_Text _modeNameText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.35f;

    private Canvas _canvas;
    private bool _isLoading;
    private bool _isDismissing;
    private bool _isHidden = true;

    public bool IsVisible => !_isHidden;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);

        _canvas = GetComponent<Canvas>();
        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 999;
        }

        HideImmediate();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(TARGET_SCENE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            if (gameObject.activeInHierarchy && !_isHidden)
            {
                StartCoroutine(SafetyDismissRoutine());
            }
            else
            {
                HideImmediate();
            }
        }
    }

    private IEnumerator SafetyDismissRoutine()
    {
        yield return new WaitForSecondsRealtime(SAFETY_CLEANUP_DELAY);
        Dismiss();
    }

    public void ShowLoadingScreen()
    {
        _isLoading = true;
        _isDismissing = false;
        _isHidden = false;

        gameObject.SetActive(true);

        if (_canvas != null)
        {
            _canvas.enabled = true;
            _canvas.sortingOrder = 999;
        }

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(true);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_mapNameText != null) _mapNameText.text = DEFAULT_MAP_NAME;
        if (_modeNameText != null) _modeNameText.text = DEFAULT_MODE_NAME;
        if (_progressBar != null) _progressBar.value = 0f;
        if (_progressText != null) _progressText.text = "0%";

        StopAllCoroutines();
        StartCoroutine(SimulateProgressRoutine());
        StartCoroutine(MaxDurationWatchdogRoutine());
    }

    private IEnumerator MaxDurationWatchdogRoutine()
    {
        yield return new WaitForSecondsRealtime(MAX_DURATION_TIMEOUT);
        Dismiss();
    }

    private IEnumerator SimulateProgressRoutine()
    {
        float currentProgress = 0f;
        while (_isLoading && currentProgress < 0.95f)
        {
            currentProgress += Time.unscaledDeltaTime * UnityEngine.Random.Range(0.45f, 0.75f);
            float clampedProgress = Mathf.Clamp01(currentProgress);
            if (_progressBar != null) _progressBar.value = clampedProgress;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(clampedProgress * 100f)}%";
            yield return null;
        }
    }

    public void Dismiss()
    {
        if (_isHidden || _isDismissing) return;

        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        _isLoading = false;
        _isDismissing = true;
        StopAllCoroutines();
        StartCoroutine(DismissLoadingScreenRoutine());
    }

    private IEnumerator DismissLoadingScreenRoutine()
    {
        if (_progressBar != null) _progressBar.value = 1f;
        if (_progressText != null) _progressText.text = "100%";

        yield return new WaitForSecondsRealtime(DISMISS_HOLD_DELAY);

        if (_canvasGroup != null)
        {
            float elapsedDuration = 0f;
            while (elapsedDuration < _fadeDuration)
            {
                elapsedDuration += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedDuration / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        HideImmediate();
    }

    public void HideImmediate()
    {
        _isLoading = false;
        _isDismissing = false;
        _isHidden = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_loadingScreenRoot != null && _loadingScreenRoot != gameObject)
        {
            _loadingScreenRoot.SetActive(false);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
    }
}