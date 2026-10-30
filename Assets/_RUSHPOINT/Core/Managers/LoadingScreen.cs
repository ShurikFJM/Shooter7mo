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
    [SerializeField] private float _fadeDuration = 0.35f;

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
        if (scene.name == "RUSPOINT" && gameObject.activeInHierarchy)
        {
            StartCoroutine(SafetyDismissRoutine());
        }
    }

    private IEnumerator SafetyDismissRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f);
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

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
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
        StartCoroutine(MaxDurationWatchdogRoutine());
    }

    private IEnumerator MaxDurationWatchdogRoutine()
    {
        // Si por cualquier motivo la red tarda mas de 4 segundos, se cierra sola sin bloquear la pantalla
        yield return new WaitForSecondsRealtime(4.0f);
        Dismiss();
    }

    private IEnumerator SimulateProgressRoutine()
    {
        float progress = 0f;
        while (_isLoading && progress < 0.95f)
        {
            progress += Time.unscaledDeltaTime * Random.Range(0.45f, 0.75f);
            float clamped = Mathf.Clamp01(progress);
            if (_progressBar != null) _progressBar.value = clamped;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            yield return null;
        }
    }

    public void Dismiss()
    {
        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        _isLoading = false;
        StopAllCoroutines();
        StartCoroutine(DismissLoadingScreenRoutine());
    }

    private IEnumerator DismissLoadingScreenRoutine()
    {
        if (_progressBar != null) _progressBar.value = 1f;
        if (_progressText != null) _progressText.text = "100%";

        yield return new WaitForSecondsRealtime(0.15f);

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

        HideImmediate();
    }

    private void HideImmediate()
    {
        _isLoading = false;

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