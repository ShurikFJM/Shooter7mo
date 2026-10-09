using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BombTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject _timerRoot;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private float _warningThreshold = 10f;
    [SerializeField] private Color _normalColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color _warningColor = new Color(1f, 0f, 0f);

    private Bomb _bomb;

    private void Start()
    {
        ResolveBombReference();
    }

    private void Update()
    {
        if (_bomb == null)
        {
            ResolveBombReference();
            if (_bomb == null)
            {
                HideTimer();
                return;
            }
        }

        if (NetworkManager.Singleton == null || _bomb.bombState.Value != BombState.Planted)
        {
            HideTimer();
            return;
        }

        double elapsed = NetworkManager.Singleton.ServerTime.Time - _bomb.plantedServerTime.Value;
        float remaining = Mathf.Max(0f, _bomb.DetonationTimeDuration - (float)elapsed);

        ShowTimer(remaining);
    }

    private void ResolveBombReference()
    {
        _bomb = FindAnyObjectByType<Bomb>();
        if (_bomb != null)
        {
            _bomb.bombState.OnValueChanged += HandleBombStateChanged;
            if (_bomb.bombState.Value == BombState.Planted)
            {
                if (_timerRoot != null) _timerRoot.SetActive(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (_bomb != null)
        {
            _bomb.bombState.OnValueChanged -= HandleBombStateChanged;
        }
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        if (currentState == BombState.Planted)
        {
            if (_timerRoot != null) _timerRoot.SetActive(true);
        }
        else
        {
            HideTimer();
        }
    }

    private void ShowTimer(float remainingSeconds)
    {
        if (_timerRoot != null && !_timerRoot.activeSelf) _timerRoot.SetActive(true);
        if (_timerText == null) return;

        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        int tenths = Mathf.FloorToInt((remainingSeconds * 10f) % 10f);

        _timerText.text = string.Format("{0:00}.{1}", seconds, tenths);
        _timerText.color = remainingSeconds <= _warningThreshold ? _warningColor : _normalColor;
    }

    private void HideTimer()
    {
        if (_timerRoot != null && _timerRoot.activeSelf) _timerRoot.SetActive(false);
    }
}