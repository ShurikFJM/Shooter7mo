using UnityEngine;
using TMPro;

public class KillFeedEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text _killerText;
    [SerializeField] private TMP_Text _victimText;
    [SerializeField] private float _displayTime = 5f; 

    public void Setup(string killerName, string victimName, Color killerColor, Color victimColor)
    {
        _killerText.text = killerName;
        _killerText.color = killerColor;

        _victimText.text = victimName;
        _victimText.color = victimColor;
        Destroy(gameObject, _displayTime);
    }
}