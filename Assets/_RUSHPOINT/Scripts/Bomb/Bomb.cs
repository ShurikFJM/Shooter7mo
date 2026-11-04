using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public enum BombState : byte
{
    OnGround,
    Carried,
    Planted,
    Defusing,
    Defused,
    Exploded
}

[RequireComponent(typeof(NetworkObject), typeof(AudioSource))]
public class Bomb : NetworkBehaviour
{
    private const float DEFAULT_DETONATION_TIME = 40f;
    private const float CARRIED_OFFSET_UP = 0.95f;
    private const float CARRIED_OFFSET_BACK = 0.22f;
    public const ulong NO_CARRIER_ID = ulong.MaxValue;

    [SerializeField] private float _detonationTimeDuration = DEFAULT_DETONATION_TIME;
    [SerializeField] private AudioClip _beepAudioClip;
    [SerializeField] private AudioClip _plantStartAudioClip;
    [SerializeField] private AudioClip _plantCompletedAudioClip;
    [SerializeField] private AudioClip _defuseStartAudioClip;
    [SerializeField] private AudioClip _defusedAudioClip;
    [SerializeField] private AudioClip _explosionAudioClip;
    [SerializeField] private GameObject _explosionVfxPrefab;

    [Header("Audio Balance")]
    [SerializeField] private float _minSoundDistance = 8f;
    [SerializeField] private float _maxSoundDistance = 75f;
    [SerializeField] private float _audioSpatialBlend = 0.8f;

    public readonly NetworkVariable<BombState> bombState = new NetworkVariable<BombState>(
        BombState.OnGround,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<ulong> carrierClientId = new NetworkVariable<ulong>(
        NO_CARRIER_ID,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<double> plantedServerTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<FixedString32Bytes> plantedSiteId = new NetworkVariable<FixedString32Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private BombState _syncedState = BombState.OnGround;
    private ulong _syncedCarrierId = NO_CARRIER_ID;
    private double _syncedPlantedTime;

    public BombState CurrentState => _syncedState;
    public ulong CurrentCarrierId => _syncedCarrierId;
    public double PlantedTime => _syncedPlantedTime;
    public float DetonationTimeDuration => _detonationTimeDuration;

    private Collider _collider;
    private Rigidbody _rigidbody;
    private AudioSource _audioSource;
    private Transform _carrierTransform;
    private Renderer[] _renderers;
    private float _nextBeepTime;
    private float _beepInterval = 1.0f;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _renderers = GetComponentsInChildren<Renderer>(true);

        ConfigureAudioSource();
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null) return;

        _audioSource.spatialBlend = _audioSpatialBlend;
        _audioSource.minDistance = _minSoundDistance;
        _audioSource.maxDistance = _maxSoundDistance;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.volume = 1.0f;
        _audioSource.playOnAwake = false;
    }

    private void LateUpdate()
    {
        if (_syncedState == BombState.Carried)
        {
            if (_carrierTransform == null)
            {
                ResolveCarrierTransform(_syncedCarrierId);
            }

            if (_carrierTransform != null)
            {
                transform.position = _carrierTransform.position + (_carrierTransform.up * CARRIED_OFFSET_UP) - (_carrierTransform.forward * CARRIED_OFFSET_BACK);
                transform.rotation = _carrierTransform.rotation;
            }
        }

        if (_syncedState == BombState.Planted || _syncedState == BombState.Defusing)
        {
            HandleBeeping();
        }
    }

    public void ApplySyncedBombState(BombState state, ulong carrierId, Vector3 targetPos, Quaternion targetRot, double plantServerTime, byte soundType)
    {
        _syncedState = state;
        _syncedCarrierId = carrierId;
        _syncedPlantedTime = plantServerTime;

        if (!IsSpawned || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer))
        {
            try
            {
                bombState.Value = state;
                carrierClientId.Value = carrierId;
                plantedServerTime.Value = plantServerTime;
            }
            catch { }
        }

        if (state != BombState.Carried)
        {
            _carrierTransform = null;
            transform.SetPositionAndRotation(targetPos, targetRot);
        }
        else
        {
            ResolveCarrierTransform(carrierId);
        }

        ApplyStateVisualsAndPhysics(state, carrierId);
        PlaySoundEffect(soundType);
    }

    private void BroadcastToAllClients(byte soundType)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            if (clientPair.Value.PlayerObject != null)
            {
                BombInteractor interactor = clientPair.Value.PlayerObject.GetComponent<BombInteractor>();
                if (interactor != null && interactor.IsSpawned)
                {
                    interactor.SyncBombStateClientRpc(
                        (byte)_syncedState,
                        _syncedCarrierId,
                        transform.position,
                        transform.rotation,
                        _syncedPlantedTime,
                        soundType
                    );
                    break;
                }
            }
        }
    }

    private void ResolveCarrierTransform(ulong clientId)
    {
        _carrierTransform = null;

        if (clientId == NO_CARRIER_ID) return;

        NetworkPlayerController[] allPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i] != null && allPlayers[i].OwnerClientId == clientId)
            {
                _carrierTransform = allPlayers[i].transform;
                return;
            }
        }
    }

    private void ApplyStateVisualsAndPhysics(BombState state, ulong currentCarrierId)
    {
        bool isLocalCarrier = NetworkManager.Singleton != null && currentCarrierId == NetworkManager.Singleton.LocalClientId;

        if (state == BombState.Carried)
        {
            if (_collider != null) _collider.enabled = false;
            if (_rigidbody != null) _rigidbody.isKinematic = true;

            SetRenderersVisibility(true);
            SetShadowsOnly(isLocalCarrier);
        }
        else if (state == BombState.OnGround || state == BombState.Planted || state == BombState.Defusing)
        {
            if (_collider != null)
            {
                _collider.enabled = true;
                _collider.isTrigger = true;
            }
            if (_rigidbody != null) _rigidbody.isKinematic = true;

            SetRenderersVisibility(true);
            SetShadowsOnly(false);
        }
        else if (state == BombState.Defused || state == BombState.Exploded)
        {
            if (_collider != null) _collider.enabled = false;
            if (_rigidbody != null) _rigidbody.isKinematic = true;
            if (state == BombState.Exploded) SetRenderersVisibility(false);
        }
    }

    private void SetRenderersVisibility(bool visible)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null) _renderers[i].enabled = visible;
        }
    }

    private void SetShadowsOnly(bool shadowsOnly)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].shadowCastingMode = shadowsOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }
    }

    private void HandleBeeping()
    {
        if (Time.time < _nextBeepTime || NetworkManager.Singleton == null) return;

        double elapsed = NetworkManager.Singleton.ServerTime.Time - _syncedPlantedTime;
        float remaining = Mathf.Max(0f, _detonationTimeDuration - (float)elapsed);

        float progress = 1f - (remaining / _detonationTimeDuration);
        _beepInterval = Mathf.Lerp(1.0f, 0.12f, progress);

        if (_beepAudioClip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_beepAudioClip, 1.15f);
        }

        _nextBeepTime = Time.time + _beepInterval;
    }

    public void ServerPickup(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _syncedState != BombState.OnGround) return;

        ApplySyncedBombState(BombState.Carried, clientId, transform.position, transform.rotation, 0, 0);
        BroadcastToAllClients(0);
    }

    public void ServerDrop(Vector3 dropPosition, Quaternion dropRotation)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _syncedState != BombState.Carried) return;

        ApplySyncedBombState(BombState.OnGround, NO_CARRIER_ID, dropPosition, dropRotation, 0, 0);
        BroadcastToAllClients(0);
    }

    public void ServerPlant(string siteId, Vector3 plantPosition, Quaternion plantRotation)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        double serverTime = NetworkManager.Singleton.ServerTime.Time;
        try { plantedSiteId.Value = siteId; } catch { }

        ApplySyncedBombState(BombState.Planted, NO_CARRIER_ID, plantPosition, plantRotation, serverTime, 1);
        BroadcastToAllClients(1);

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.NotifyBombStateChangedServer(BombState.Planted);
        }
    }

    public void ServerDefuse()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (_syncedState != BombState.Planted && _syncedState != BombState.Defusing) return;

        ApplySyncedBombState(BombState.Defused, NO_CARRIER_ID, transform.position, transform.rotation, _syncedPlantedTime, 2);
        BroadcastToAllClients(2);

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.NotifyBombStateChangedServer(BombState.Defused);
        }
    }

    public void ServerExplode()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        ApplySyncedBombState(BombState.Exploded, NO_CARRIER_ID, transform.position, transform.rotation, _syncedPlantedTime, 3);
        BroadcastToAllClients(3);

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.NotifyBombStateChangedServer(BombState.Exploded);
        }
    }

    public void ServerResetBomb(Vector3 resetPosition)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        try { plantedSiteId.Value = string.Empty; } catch { }
        ApplySyncedBombState(BombState.OnGround, NO_CARRIER_ID, resetPosition, Quaternion.identity, 0, 0);
        BroadcastToAllClients(0);
    }

    public void PlaySoundEffect(byte soundType)
    {
        if (soundType == 0 || _audioSource == null) return;

        if (soundType == 1 && _plantCompletedAudioClip != null)
        {
            _audioSource.PlayOneShot(_plantCompletedAudioClip, 1.4f);
        }
        else if (soundType == 2 && _defusedAudioClip != null)
        {
            _audioSource.PlayOneShot(_defusedAudioClip, 1.3f);
        }
        else if (soundType == 3)
        {
            if (_explosionAudioClip != null) _audioSource.PlayOneShot(_explosionAudioClip, 1.6f);
            if (_explosionVfxPrefab != null) Instantiate(_explosionVfxPrefab, transform.position, Quaternion.identity);
        }
        else if (soundType == 4)
        {
            AudioClip clip = _plantStartAudioClip != null ? _plantStartAudioClip : _beepAudioClip;
            if (clip != null) _audioSource.PlayOneShot(clip, 1.25f);
        }
        else if (soundType == 5)
        {
            AudioClip clip = _defuseStartAudioClip != null ? _defuseStartAudioClip : _defusedAudioClip;
            if (clip != null) _audioSource.PlayOneShot(clip, 1.25f);
        }
    }
}