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

    private Transform _headBone;

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

        if (IsOwner)
        {
            Animator anim = GetComponentInChildren<Animator>(true);
            if (anim != null && anim.isHuman)
            {
                _headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            }
            else
            {
                Transform[] bones = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i].name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _headBone = bones[i];
                        break;
                    }
                }
            }
        }

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

    private void LateUpdate()
    {
        if (IsOwner && _headBone != null)
        {
            _headBone.localScale = Vector3.zero;
        }
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
        Material targetMaterial = null;
        if (team == Team.Red) targetMaterial = _terroristMaterial;
        else if (team == Team.Blue) targetMaterial = _counterTerroristMaterial;
        else targetMaterial = _counterTerroristMaterial; 

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

            if (targetMaterial != null) rend.material = targetMaterial;

            rend.enabled = true;
            rend.shadowCastingMode = ShadowCastingMode.On;
        }
    }
}