using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerTeamVisualizer : NetworkBehaviour
{
    [SerializeField] private PlayerTeam _playerTeam;
    [SerializeField] private Transform _modelRoot;
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
            RefreshRenderers();
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

    public void EnsureRenderers(bool force = false)
    {
        if (!force && _teamMeshRenderers != null && _teamMeshRenderers.Length > 0 && _teamMeshRenderers[0] != null)
        {
            return;
        }

        Transform searchRoot = _modelRoot != null ? _modelRoot : transform;
        _teamMeshRenderers = searchRoot.GetComponentsInChildren<Renderer>(true);
    }

    public void RefreshRenderers(Renderer[] newRenderers = null)
    {
        if (newRenderers != null && newRenderers.Length > 0)
        {
            _teamMeshRenderers = newRenderers;
        }
        else
        {
            Transform searchRoot = _modelRoot != null ? _modelRoot : transform;
            _teamMeshRenderers = searchRoot.GetComponentsInChildren<Renderer>(true);
        }

        if (_playerTeam != null)
        {
            ApplyTeamVisuals(_playerTeam.CurrentTeam.Value);
        }
    }

    private void HandleTeamChanged(Team previousTeam, Team currentTeam)
    {
        ApplyTeamVisuals(currentTeam);
    }

    public void ApplyTeamVisuals(Team team)
    {
        if (team == Team.Neutral)
        {
            if (_teamMeshRenderers != null)
            {
                for (int i = 0; i < _teamMeshRenderers.Length; i++)
                {
                    if (_teamMeshRenderers[i] != null) _teamMeshRenderers[i].enabled = false;
                }
            }
            return;
        }

        Material targetMaterial = team == Team.Red ? _terroristMaterial : _counterTerroristMaterial;
        if (targetMaterial == null) return;

        EnsureRenderers();

        if (_teamMeshRenderers == null) return;

        for (int i = 0; i < _teamMeshRenderers.Length; i++)
        {
            Renderer rend = _teamMeshRenderers[i];
            if (rend == null) continue;

            if (rend.name.StartsWith("weapon_", System.StringComparison.OrdinalIgnoreCase) ||
                rend.name.StartsWith("AK_", System.StringComparison.OrdinalIgnoreCase) ||
                rend.name.StartsWith("Gun_", System.StringComparison.OrdinalIgnoreCase) ||
                (rend.name.IndexOf("Weapon", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                 rend.name.IndexOf("Container", System.StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            rend.material = targetMaterial;

            if (IsOwner)
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
            }
        }
    }
}