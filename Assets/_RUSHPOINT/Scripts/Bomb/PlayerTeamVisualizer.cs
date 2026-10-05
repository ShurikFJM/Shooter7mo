using Unity.Netcode;
using UnityEngine;

public class PlayerTeamVisualizer : NetworkBehaviour
{
    [SerializeField] private PlayerTeam _playerTeam;
    [SerializeField] private Renderer[] _teamMeshRenderers;
    [SerializeField] private Material _terroristMaterial;
    [SerializeField] private Material _counterTerroristMaterial;

    private void Awake()
    {
        if (_playerTeam == null)
        {
            _playerTeam = GetComponent<PlayerTeam>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (_playerTeam != null)
        {
            _playerTeam.CurrentTeam.OnValueChanged += HandleTeamChanged;
            ApplyTeamVisuals(_playerTeam.CurrentTeam.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_playerTeam != null)
        {
            _playerTeam.CurrentTeam.OnValueChanged -= HandleTeamChanged;
        }

        base.OnNetworkDespawn();
    }

    private void HandleTeamChanged(Team previousTeam, Team currentTeam)
    {
        ApplyTeamVisuals(currentTeam);
    }

    private void ApplyTeamVisuals(Team team)
    {
        Material targetMaterial = team == Team.Red ? _terroristMaterial : _counterTerroristMaterial;

        if (targetMaterial == null || _teamMeshRenderers == null) return;

        for (int i = 0; i < _teamMeshRenderers.Length; i++)
        {
            if (_teamMeshRenderers[i] != null)
            {
                _teamMeshRenderers[i].material = targetMaterial;
            }
        }
    }
}