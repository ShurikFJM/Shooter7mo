using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Cronómetro visible para TODOS los jugadores (no solo quien carga/desactiva
/// la bomba) mientras esta está plantada. Usa el reloj sincronizado de red
/// (NetworkManager.ServerTime) en vez de un contador local, así todos los
/// clientes ven el mismo tiempo restante sin importar lag o cuándo se conectaron.
/// Colocar en el mismo Canvas de pantalla que BombPromptUI.
/// </summary>
public class BombTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject _timerRoot;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private float _warningThreshold = 10f;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = new Color(0.9f, 0.2f, 0.2f);

    private Bomb _bomb;

    private void Update()
    {
        if (_bomb == null)
        {
            // Normalmente solo hay una bomba activa por partida; cachearla una vez basta.
            _bomb = FindFirstObjectByType<Bomb>();
            if (_bomb == null)
            {
                HideTimer();
                return;
            }
        }

        if (_bomb.State.Value != BombState.Planted || NetworkManager.Singleton == null)
        {
            HideTimer();
            return;
        }

        double elapsed = NetworkManager.Singleton.ServerTime.Time - _bomb.PlantedServerTime.Value;
        float remaining = Mathf.Max(0f, _bomb.DetonationTimeDuration - (float)elapsed);

        ShowTimer(remaining);
    }

    private void ShowTimer(float remainingSeconds)
    {
        if (_timerRoot != null) _timerRoot.SetActive(true);
        if (_timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        _timerText.text = $"{minutes:00}:{seconds:00}";
        _timerText.color = remainingSeconds <= _warningThreshold ? _warningColor : _normalColor;
    }

    private void HideTimer()
    {
        if (_timerRoot != null) _timerRoot.SetActive(false);
    }
}
