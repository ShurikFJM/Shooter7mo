using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [SerializeField] private GameObject _loadingScreenRoot;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _mapNameText;
    [SerializeField] private TMP_Text _modeNameText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.5f;

    private Canvas _canvas;
    private bool _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _canvas = GetComponent<Canvas>();
        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 999;
        }

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(false);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "RUSPOINT")
        {
            StartCoroutine(SafetyDismissRoutine());
        }
    }

    private IEnumerator SafetyDismissRoutine()
    {
        // En 3 segundos reales, si el servidor no lo cerro, se desvanece forzosamente
        yield return new WaitForSecondsRealtime(3.0f);
        Dismiss();
    }

    public void ShowLoadingScreen()
    {
        _isLoading = true;
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

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_mapNameText != null) _mapNameText.text = "HEAVEN";
        if (_modeNameText != null) _modeNameText.text = "SEARCH & DESTROY";
        if (_progressBar != null) _progressBar.value = 0f;
        if (_progressText != null) _progressText.text = "0%";

        StopAllCoroutines();
        StartCoroutine(SimulateProgressRoutine());
    }

    private IEnumerator SimulateProgressRoutine()
    {
        float progress = 0f;
        while (_isLoading && progress < 0.95f)
        {
            progress += Time.unscaledDeltaTime * Random.Range(0.4f, 0.7f);
            float clamped = Mathf.Clamp01(progress);
            if (_progressBar != null) _progressBar.value = clamped;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            yield return null;
        }
    }

    public void Dismiss()
    {
        _isLoading = false;
        StopAllCoroutines();
        StartCoroutine(DismissLoadingScreenRoutine());
    }

    private IEnumerator DismissLoadingScreenRoutine()
    {
        if (_progressBar != null) _progressBar.value = 1f;
        if (_progressText != null) _progressText.text = "100%";

        yield return new WaitForSecondsRealtime(0.2f);

        if (_canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_loadingScreenRoot != null)
        {
            _loadingScreenRoot.SetActive(false);
        }

        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
    }
}