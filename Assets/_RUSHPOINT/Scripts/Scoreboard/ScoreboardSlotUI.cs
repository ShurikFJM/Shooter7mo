using TMPro;
using UnityEngine;

public class ScoreboardSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private TMP_Text _killsText;
    [SerializeField] private TMP_Text _assistsText;
    [SerializeField] private TMP_Text _deathsText;

    private bool _warnedMissingRefs;

    public void SetData(string playerName, int kills, int assists, int deaths)
    {
        gameObject.SetActive(true);

        if (!_warnedMissingRefs &&
            (_playerNameText == null || _killsText == null || _assistsText == null || _deathsText == null))
        {
            _warnedMissingRefs = true;
            Debug.LogWarning($"[ScoreboardSlotUI] '{name}' tiene TMP_Text sin asignar en el Inspector " +
                             "(nombre/kills/asistencias/muertes). Los campos sin asignar no se llenan.", this);
        }

        if (_playerNameText != null) _playerNameText.text = playerName;
        if (_killsText != null) _killsText.text = kills.ToString();
        if (_assistsText != null) _assistsText.text = assists.ToString();
        if (_deathsText != null) _deathsText.text = deaths.ToString();
    }

    public void ClearSlot()
    {
        gameObject.SetActive(false);
    }
}