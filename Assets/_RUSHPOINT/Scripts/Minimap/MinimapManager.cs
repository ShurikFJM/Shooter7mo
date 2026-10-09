using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [SerializeField] private RectTransform _minimapRect;
    [SerializeField] private MinimapPlayerIcon _teammateIconPrefab;
    [SerializeField] private Color _teammateColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private float _minWorldX = -50f;
    [SerializeField] private float _maxWorldX = 50f;
    [SerializeField] private float _minWorldZ = -50f;
    [SerializeField] private float _maxWorldZ = 50f;
    [SerializeField] private bool _invertX = false;
    [SerializeField] private bool _invertZ = true;
    [SerializeField] private float _rotationOffset = 0f;

    private readonly Dictionary<ulong, MinimapPlayerIcon> _spawnedIcons = new Dictionary<ulong, MinimapPlayerIcon>();
    private PlayerTeam _localPlayerTeam;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        if (_localPlayerTeam == null)
        {
            NetworkObject localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            if (localObj != null)
            {
                _localPlayerTeam = localObj.GetComponent<PlayerTeam>();
            }
            return;
        }

        RefreshTeammates();
    }

    private void RefreshTeammates()
    {
        if (_minimapRect == null || _teammateIconPrefab == null) return;

        Team myTeam = _localPlayerTeam.CurrentTeam.Value;
        if (myTeam == Team.Neutral) return;

        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;
        HashSet<ulong> currentConnectedIds = new HashSet<ulong>();

        for (int i = 0; i < clients.Count; i++)
        {
            NetworkClient client = clients[i];
            if (client.PlayerObject == null) continue;

            ulong clientId = client.ClientId;
            if (clientId == NetworkManager.Singleton.LocalClientId) continue;

            currentConnectedIds.Add(clientId);

            PlayerTeam playerTeamComp = client.PlayerObject.GetComponent<PlayerTeam>();
            if (playerTeamComp == null || playerTeamComp.CurrentTeam.Value != myTeam)
            {
                RemoveIcon(clientId);
                continue;
            }

            if (!_spawnedIcons.ContainsKey(clientId))
            {
                MinimapPlayerIcon newIcon = Instantiate(_teammateIconPrefab, _minimapRect);
                newIcon.Setup(
                    client.PlayerObject.transform,
                    _minimapRect,
                    _teammateColor,
                    _minWorldX,
                    _maxWorldX,
                    _minWorldZ,
                    _maxWorldZ,
                    _invertX,
                    _invertZ,
                    _rotationOffset
                );
                _spawnedIcons.Add(clientId, newIcon);
            }
        }

        List<ulong> toRemove = new List<ulong>();
        foreach (KeyValuePair<ulong, MinimapPlayerIcon> kvp in _spawnedIcons)
        {
            if (!currentConnectedIds.Contains(kvp.Key) || kvp.Value == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            RemoveIcon(toRemove[i]);
        }
    }

    private void RemoveIcon(ulong clientId)
    {
        if (_spawnedIcons.TryGetValue(clientId, out MinimapPlayerIcon icon))
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
            _spawnedIcons.Remove(clientId);
        }
    }
}