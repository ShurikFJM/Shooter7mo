using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum LoadingExitAnimation
{
    SlideUp,
    SlideDown,
    SlideLeft,
    ScaleCollapse
}

[RequireComponent(typeof(Canvas))]
public class LoadingScreen : MonoBehaviour
{
    private const string DEFAULT_MAP_NAME = "HEAVEN";
    private const string DEFAULT_MODE_NAME = "SEARCH & DESTROY";
    private const string TARGET_SCENE_NAME = "RUSPOINT";
    private const float DISMISS_HOLD_DELAY = 0.15f;
    private const float WAITING_DOTS_INTERVAL = 0.4f;
    private const float MAX_SCENE_READY_WAIT = 10f;

    private static readonly string[] WAITING_STATUS_LABELS = new string[]
    {
        "100% - ESPERANDO JUGADORES",
        "100% - ESPERANDO JUGADORES.",
        "100% - ESPERANDO JUGADORES..",
        "100% - ESPERANDO JUGADORES..."
    };

    private static readonly WaitForSecondsRealtime HOLD_WAIT = new WaitForSecondsRealtime(DISMISS_HOLD_DELAY);
    private static readonly WaitForSecondsRealtime DOTS_WAIT = new WaitForSecondsRealtime(WAITING_DOTS_INTERVAL);

    public static LoadingScreen Instance { get; private set; }

    [SerializeField] private GameObject _loadingScreenRoot;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _mapNameText;
    [SerializeField] private TMP_Text _modeNameText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Animación de Salida (Sin Fade Out)")]
    [SerializeField] private LoadingExitAnimation _exitAnimation = LoadingExitAnimation.SlideUp;
    [SerializeField] private float _exitAnimationDuration = 0.45f;

    private Canvas _canvas;
    private RectTransform _animatedRect;
    private Vector2 _originalAnchoredPosition;
    private Vector3 _originalLocalScale = Vector3.one;
    private bool _hasCachedTransform;

    private Coroutine _progressCoroutine;
    private Coroutine _waitReadyCoroutine;
    private Coroutine _dismissCoroutine;

    private bool _isLoading;
    private bool _isDismissing;
    private bool _isHidden = true;
    private bool _hasTargetSceneLoaded;

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

        CacheAnimatedTransform();
        HideImmediate();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void CacheAnimatedTransform()
    {
        if (_hasCachedTransform) return;

        if (_loadingScreenRoot != null && _loadingScreenRoot != gameObject)
        {
            _animatedRect = _loadingScreenRoot.GetComponent<RectTransform>();
        }
        else if (transform.childCount > 0)
        {
            _animatedRect = transform.GetChild(0).GetComponent<RectTransform>();
        }

        if (_animatedRect != null)
        {
            _originalAnchoredPosition = _animatedRect.anchoredPosition;
            _originalLocalScale = _animatedRect.localScale;
            _hasCachedTransform = true;
        }
    }

    private void ResetAnimatedTransform()
    {
        CacheAnimatedTransform();
        if (_animatedRect != null)
        {
            _animatedRect.anchoredPosition = _originalAnchoredPosition;
            _animatedRect.localScale = _originalLocalScale;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(TARGET_SCENE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            _hasTargetSceneLoaded = true;

            if (gameObject.activeInHierarchy && !_isHidden && !_isDismissing)
            {
                StartWaitingUntilMatchReady();
            }
        }
        else
        {
            _hasTargetSceneLoaded = false;
        }
    }

    public void ShowLoadingScreen()
    {
        StopRunningCoroutines();

        _isLoading = true;
        _isDismissing = false;
        _isHidden = false;
        _hasTargetSceneLoaded = SceneManager.GetActiveScene().name.Equals(TARGET_SCENE_NAME, StringComparison.OrdinalIgnoreCase);

        gameObject.SetActive(true);
        ResetAnimatedTransform();

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

        _progressCoroutine = StartCoroutine(SimulateProgressRoutine());
    }

    private IEnumerator SimulateProgressRoutine()
    {
        float currentProgress = 0f;
        while (_isLoading && currentProgress < 1f)
        {
            currentProgress += Time.unscaledDeltaTime * UnityEngine.Random.Range(0.35f, 0.65f);
            float clampedProgress = Mathf.Clamp01(currentProgress);
            if (_progressBar != null) _progressBar.value = clampedProgress;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(clampedProgress * 100f)}%";
            yield return null;
        }

        if (_progressBar != null) _progressBar.value = 1f;

        // Se mantiene visible al 100% hasta que todos estén listos y arranque la partida
        int dotIndex = 0;
        while (_isLoading)
        {
            if (_progressText != null)
            {
                _progressText.text = WAITING_STATUS_LABELS[dotIndex];
            }

            dotIndex = (dotIndex + 1) % WAITING_STATUS_LABELS.Length;
            yield return DOTS_WAIT;
        }
    }

    private bool IsMatchVisuallyReady()
    {
        bool isTargetScene = _hasTargetSceneLoaded ||
                             SceneManager.GetActiveScene().name.Equals(TARGET_SCENE_NAME, StringComparison.OrdinalIgnoreCase);

        if (!isTargetScene) return false;

        if (RoleSelectScreenUI.instance != null && RoleSelectScreenUI.instance.isRoleSelectionActive)
        {
            return false;
        }

        if (RoundManager.Instance != null && RoundManager.Instance.SyncedPhase == RoundPhase.WaitingForPlayers)
        {
            return false;
        }

        if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating)
        {
            return true;
        }

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            NetworkManager.Singleton.SpawnManager != null)
        {
            return NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null;
        }

        return true;
    }

    private void StartWaitingUntilMatchReady()
    {
        if (_waitReadyCoroutine != null || _isDismissing) return;
        _waitReadyCoroutine = StartCoroutine(WaitUntilMatchReadyRoutine());
    }

    private IEnumerator WaitUntilMatchReadyRoutine()
    {
        float elapsed = 0f;
        while (!IsMatchVisuallyReady() && elapsed < MAX_SCENE_READY_WAIT)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _waitReadyCoroutine = null;

        if (!_isHidden && !_isDismissing)
        {
            BeginDismissAnimation();
        }
    }

    public void Dismiss(bool force = false)
    {
        if (_isHidden || _isDismissing) return;

        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        if (!force)
        {
            bool isTargetScene = _hasTargetSceneLoaded ||
                                 SceneManager.GetActiveScene().name.Equals(TARGET_SCENE_NAME, StringComparison.OrdinalIgnoreCase);

            // Si todavía estamos en la escena anterior esperando roles, no cerrar
            if (!isTargetScene) return;

            // Si ya cargó RUSPOINT pero el jugador/ronda aún no está listo, espera automáticamente
            if (!IsMatchVisuallyReady())
            {
                StartWaitingUntilMatchReady();
                return;
            }
        }

        BeginDismissAnimation();
    }

    private void BeginDismissAnimation()
    {
        if (_isHidden || _isDismissing) return;

        _isLoading = false;
        _isDismissing = true;
        StopRunningCoroutines();
        _dismissCoroutine = StartCoroutine(DismissLoadingScreenRoutine());
    }

    private IEnumerator DismissLoadingScreenRoutine()
    {
        if (_progressBar != null) _progressBar.value = 1f;
        if (_progressText != null) _progressText.text = "100%";

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
        }

        yield return HOLD_WAIT;

        if (_animatedRect != null && _exitAnimationDuration > 0.01f)
        {
            Rect canvasRect = _canvas != null ? ((RectTransform)_canvas.transform).rect : new Rect(0f, 0f, 1920f, 1080f);
            float moveDistanceY = Mathf.Max(canvasRect.height, Screen.height) + 120f;
            float moveDistanceX = Mathf.Max(canvasRect.width, Screen.width) + 120f;

            Vector2 startPos = _originalAnchoredPosition;
            Vector2 endPos = startPos;

            if (_exitAnimation == LoadingExitAnimation.SlideUp)
                endPos = startPos + new Vector2(0f, moveDistanceY);
            else if (_exitAnimation == LoadingExitAnimation.SlideDown)
                endPos = startPos - new Vector2(0f, moveDistanceY);
            else if (_exitAnimation == LoadingExitAnimation.SlideLeft)
                endPos = startPos - new Vector2(moveDistanceX, 0f);

            float elapsed = 0f;
            while (elapsed < _exitAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _exitAnimationDuration);

                // Curva táctil con ligero impulso inicial (EaseInBack suave)
                float c1 = 1.2f;
                float c3 = c1 + 1f;
                float eased = Mathf.Clamp01((c3 * t * t * t) - (c1 * t * t));

                if (_exitAnimation == LoadingExitAnimation.ScaleCollapse)
                {
                    float scaleY = Mathf.Lerp(_originalLocalScale.y, 0f, eased);
                    _animatedRect.localScale = new Vector3(_originalLocalScale.x, scaleY, _originalLocalScale.z);
                }
                else
                {
                    _animatedRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
                }

                yield return null;
            }
        }

        HideImmediate();
    }

    private void StopRunningCoroutines()
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }

        if (_waitReadyCoroutine != null)
        {
            StopCoroutine(_waitReadyCoroutine);
            _waitReadyCoroutine = null;
        }

        if (_dismissCoroutine != null)
        {
            StopCoroutine(_dismissCoroutine);
            _dismissCoroutine = null;
        }
    }

    public void HideImmediate()
    {
        StopRunningCoroutines();

        _isLoading = false;
        _isDismissing = false;
        _isHidden = true;

        ResetAnimatedTransform();

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