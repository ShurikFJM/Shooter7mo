using UnityEngine;

public class TopBarIconsManager : MonoBehaviour
{
    [SerializeField] private PlayerIconSlot[] _redTeamSlots;
    [SerializeField] private PlayerIconSlot[] _blueTeamSlots;

    private float _updateTimer = 0f;
    private const float REFRESH_RATE = 1f;

    private void Start()
    {
        ClearAllSlots();
    }

    private void Update()
    {
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= REFRESH_RATE)
        {
            _updateTimer = 0f;
            RefreshSlots();
        }
    }

    private void RefreshSlots()
    {
        ClearAllSlots();
        NetworkHealth[] allPlayers = FindObjectsByType<NetworkHealth>(FindObjectsSortMode.None);

        int redIndex = 0;
        int blueIndex = 0;

        foreach (NetworkHealth healthComp in allPlayers)
        {
            PlayerTeam teamComp = healthComp.GetComponent<PlayerTeam>();
            if (teamComp == null) teamComp = healthComp.GetComponentInChildren<PlayerTeam>();

            if (teamComp != null)
            {
                Team team = teamComp.CurrentTeam.Value;
                if (team == Team.Red && redIndex < _redTeamSlots.Length)
                {
                    _redTeamSlots[redIndex].Bind(healthComp);
                    redIndex++;
                }
                else if (team == Team.Blue && blueIndex < _blueTeamSlots.Length)
                {
                    _blueTeamSlots[blueIndex].Bind(healthComp);
                    blueIndex++;
                }
            }
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < _redTeamSlots.Length; i++) _redTeamSlots[i].Clear();
        for (int i = 0; i < _blueTeamSlots.Length; i++) _blueTeamSlots[i].Clear();
    }
}