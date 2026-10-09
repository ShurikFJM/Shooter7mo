using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    private const float MAX_SPOT_DISTANCE = 35f;
    private const float SPOT_ANGLE = 50f;
    private const float RAYCAST_HEIGHT_OFFSET = 1.2f;

    public RectTransform minimapRect;
    public MinimapPlayerIcon teammateIconPrefab;
    public MinimapPlayerIcon enemyIconPrefab;
    public RectTransform bombIconRect;
    public Team terroristTeam;
    public Vector2 worldCenter = Vector2.zero;
    public Vector2 worldSize = new Vector2(100f, 100f);
    public Color teammateColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color enemyColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float iconRotationOffset = 90f;
    public LayerMask obstacleLayers;

    private readonly Dictionary<ulong, MinimapPlayerIcon> _playerIcons = new Dictionary<ulong, MinimapPlayerIcon>();
    private PlayerTeam _localPlayerTeam;
    private Transform _currentBombTransform;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        if (_localPlayerTeam == null)
        {
            NetworkObject localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            if (localObj != null) _localPlayerTeam = localObj.GetComponent<PlayerTeam>();
            return;
        }

        UpdateMinimapIcons();
    }

    private void UpdateMinimapIcons()
    {
        if (minimapRect == null || teammateIconPrefab == null || enemyIconPrefab == null) return;

        Team myTeam = _localPlayerTeam.CurrentTeam.Value;
        if (myTeam == Team.Neutral) return;

        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;
        HashSet<ulong> currentConnectedIds = new HashSet<ulong>();

        List<Transform> teammates = new List<Transform>();
        List<Transform> enemies = new List<Transform>();
        List<ulong> enemyIds = new List<ulong>();

        for (int i = 0; i < clients.Count; i++)
        {
            NetworkClient client = clients[i];
            if (client.PlayerObject == null) continue;

            ulong clientId = client.ClientId;
            currentConnectedIds.Add(clientId);
            PlayerTeam playerTeamComp = client.PlayerObject.GetComponent<PlayerTeam>();

            if (playerTeamComp != null)
            {
                if (playerTeamComp.CurrentTeam.Value == myTeam)
                {
                    teammates.Add(client.PlayerObject.transform);
                    ManageIcon(clientId, client.PlayerObject.transform, true);
                }
                else if (playerTeamComp.CurrentTeam.Value != Team.Neutral)
                {
                    enemies.Add(client.PlayerObject.transform);
                    enemyIds.Add(clientId);
                    ManageIcon(clientId, client.PlayerObject.transform, false);
                }
            }
        }

        UpdateEnemyVisibility(teammates, enemies, enemyIds);
        UpdateBombIconVisibility(myTeam);

        List<ulong> toRemove = new List<ulong>();
        foreach (KeyValuePair<ulong, MinimapPlayerIcon> kvp in _playerIcons)
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

    private void ManageIcon(ulong clientId, Transform target, bool isTeammate)
    {
        if (!_playerIcons.ContainsKey(clientId))
        {
            MinimapPlayerIcon prefabToUse = isTeammate ? teammateIconPrefab : enemyIconPrefab;
            Color colorToUse = isTeammate ? teammateColor : enemyColor;

            MinimapPlayerIcon newIcon = Instantiate(prefabToUse, minimapRect);
            newIcon.Setup(target, this, colorToUse, iconRotationOffset, isTeammate);
            _playerIcons.Add(clientId, newIcon);
        }
    }

    private void UpdateEnemyVisibility(List<Transform> teammates, List<Transform> enemies, List<ulong> enemyIds)
    {
        Transform localPlayerTransform = _localPlayerTeam.transform;

        for (int i = 0; i < enemies.Count; i++)
        {
            Transform enemy = enemies[i];
            ulong enemyId = enemyIds[i];
            bool isVisible = false;

            if (IsEnemyVisibleTo(localPlayerTransform, enemy))
            {
                isVisible = true;
            }
            else
            {
                for (int j = 0; j < teammates.Count; j++)
                {
                    if (IsEnemyVisibleTo(teammates[j], enemy))
                    {
                        isVisible = true;
                        break;
                    }
                }
            }

            if (_playerIcons.TryGetValue(enemyId, out MinimapPlayerIcon enemyIcon))
            {
                enemyIcon.SetVisibility(isVisible);
            }
        }
    }

    private bool IsEnemyVisibleTo(Transform observer, Transform target)
    {
        Vector3 directionToTarget = target.position - observer.position;
        float distance = directionToTarget.magnitude;

        if (distance > MAX_SPOT_DISTANCE) return false;

        float angle = Vector3.Angle(observer.forward, directionToTarget);
        if (angle > SPOT_ANGLE) return false;

        Vector3 rayOrigin = observer.position + Vector3.up * RAYCAST_HEIGHT_OFFSET;
        if (Physics.Raycast(rayOrigin, directionToTarget.normalized, out RaycastHit hit, distance, obstacleLayers))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                return false;
            }
        }

        return true;
    }

    private void RemoveIcon(ulong clientId)
    {
        if (_playerIcons.TryGetValue(clientId, out MinimapPlayerIcon icon))
        {
            if (icon != null) Destroy(icon.gameObject);
            _playerIcons.Remove(clientId);
        }
    }

    private void UpdateBombIconVisibility(Team myTeam)
    {
        if (bombIconRect == null) return;

        if (myTeam != terroristTeam)
        {
            if (bombIconRect.gameObject.activeSelf) bombIconRect.gameObject.SetActive(false);
            return;
        }

        if (_currentBombTransform == null)
        {
            Bomb bomb = FindAnyObjectByType<Bomb>();
            if (bomb != null) _currentBombTransform = bomb.transform;
        }

        if (_currentBombTransform != null)
        {
            if (!bombIconRect.gameObject.activeSelf) bombIconRect.gameObject.SetActive(true);
            bombIconRect.anchoredPosition = CalculateMinimapPosition(_currentBombTransform.position);
        }
        else
        {
            if (bombIconRect.gameObject.activeSelf) bombIconRect.gameObject.SetActive(false);
        }
    }

    public Vector2 CalculateMinimapPosition(Vector3 worldPosition)
    {
        if (worldSize.x == 0f || worldSize.y == 0f) return Vector2.zero;

        float normalizedX = (worldPosition.x - worldCenter.x) / worldSize.x;
        float normalizedZ = (worldPosition.z - worldCenter.y) / worldSize.y;

        float mapX = normalizedX * minimapRect.rect.width;
        float mapY = normalizedZ * minimapRect.rect.height;

        return new Vector2(mapX, mapY);
    }
}