using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum BombState : byte
{
    Dropped,
    Carried,
    Planted,
    Defusing,
    Defused,
    Exploded
}

[RequireComponent(typeof(NetworkObject), typeof(AudioSource))]
public class Bomb : NetworkBehaviour
{
    public const ulong UNASSIGNED_CARRIER_ID = 9999;
    private const float MIN_REMAINING_DETONATION_TIME = 0f;
    private const float MIN_NORMALIZED_PROGRESS = 0f;
    private const float MAX_NORMALIZED_PROGRESS = 1f;
    private const float GROUND_CHECK_RAY_ORIGIN_HEIGHT = 0.5f;
    private const float GROUND_CHECK_RAY_DISTANCE = 3.5f;
    private const float GROUND_OFFSET_Y = 0.2f;
    private const float THROW_FORWARD_FORCE = 4f;
    private const float THROW_UPWARD_FORCE = 2f;
    private const float TEMPORARY_COLLISION_IGNORE_DURATION = 0.4f;
    private const float DROP_THROW_ORIGIN_FORWARD_OFFSET = 0.9f;
    private const float AUDIO_CLEANUP_EXTRA_DELAY = 0.1f;

    [SerializeField] private Team _bombCarrierTeam = Team.Red;
    [SerializeField] private Team _defuseTeam = Team.Blue;
    [SerializeField] private float _detonationDuration = 45f;
    [SerializeField] private GameObject _dropVfxPrefab;
    [SerializeField] private GameObject _plantVfxPrefab;
    [SerializeField] private GameObject _defuseVfxPrefab;
    [SerializeField] private GameObject _explosionVfxPrefab;
    [SerializeField] private AudioClip _globalPlantedClip;
    [SerializeField] private float _globalPlantedVolume = 0.25f;
    [SerializeField] private AudioSource _bombAudioSource;
    [SerializeField] private AudioClip _plantedBeepClip;
    [SerializeField] private float _beepVolume = 0.2f;
    [SerializeField] private float _initialBeepInterval = 1f;
    [SerializeField] private float _fastestBeepInterval = 0.08f;
    [SerializeField] private float _audioMinDistance = 2f;
    [SerializeField] private float _audioMaxDistance = 35f;

    public NetworkVariable<BombState> bombState = new NetworkVariable<BombState>(
        BombState.Dropped,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> carrierClientId = new NetworkVariable<ulong>(
        UNASSIGNED_CARRIER_ID,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> defuserClientId = new NetworkVariable<ulong>(
        UNASSIGNED_CARRIER_ID,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<double> plantedServerTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public Team BombCarrierTeam => _bombCarrierTeam;
    public Team DefuseTeam => _defuseTeam;
    public float DetonationTimeDuration => _detonationDuration;

    private Coroutine _beepCoroutine;
    private Collider _bombCollider;
    private Rigidbody _rigidbody;
    private Renderer[] _renderers;
    private NetworkTransform _networkTransform;

    private void Awake()
    {
        _bombCollider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _networkTransform = GetComponent<NetworkTransform>();

        InitializeAudioSourceConfiguration();
    }

    private void InitializeAudioSourceConfiguration()
    {
        if (_bombAudioSource == null)
        {
            _bombAudioSource = GetComponent<AudioSource>();
        }

        if (_bombAudioSource != null)
        {
            _bombAudioSource.spatialBlend = 1f;
            _bombAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _bombAudioSource.minDistance = _audioMinDistance;
            _bombAudioSource.maxDistance = _audioMaxDistance;
            _bombAudioSource.playOnAwake = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        bombState.OnValueChanged += HandleBombStateChanged;
        ApplyPhysicsAndVisualState(bombState.Value);

        if (bombState.Value == BombState.Planted && _beepCoroutine == null)
        {
            _beepCoroutine = StartCoroutine(PlantedBeepLoop());
        }
    }

    public override void OnNetworkDespawn()
    {
        bombState.OnValueChanged -= HandleBombStateChanged;
        StopBeepRoutine();
    }

    private void HandleBombStateChanged(BombState previousState, BombState currentState)
    {
        ApplyPhysicsAndVisualState(currentState);

        switch (currentState)
        {
            case BombState.Carried:
            case BombState.Dropped:
                StopBeepRoutine();
                break;
            case BombState.Planted:
            case BombState.Defusing:
                if (_beepCoroutine == null)
                {
                    _beepCoroutine = StartCoroutine(PlantedBeepLoop());
                }
                break;
            case BombState.Defused:
            case BombState.Exploded:
                StopBeepRoutine();
                break;
        }
    }

    private void ApplyPhysicsAndVisualState(BombState currentState)
    {
        bool isDropped = currentState == BombState.Dropped;
        bool isPlanted = currentState == BombState.Planted || currentState == BombState.Defusing;

        UpdateRendererVisibility(isDropped || isPlanted);

        if (_rigidbody != null)
        {
            if (currentState == BombState.Carried || isPlanted)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            else if (isDropped && IsServer)
            {
                _rigidbody.isKinematic = false;
            }
        }

        if (_bombCollider != null)
        {
            _bombCollider.enabled = isDropped || isPlanted;
        }
    }

    private void UpdateRendererVisibility(bool isVisible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = isVisible;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPickupServerRpc(ulong requestingClientId)
    {
        if (bombState.Value != BombState.Dropped) return;

        carrierClientId.Value = requestingClientId;
        bombState.Value = BombState.Carried;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(requestingClientId, out NetworkClient networkClient) && networkClient.PlayerObject != null)
        {
            transform.position = networkClient.PlayerObject.transform.position;
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.TrySetParent(networkClient.PlayerObject.transform);
            }
            else
            {
                transform.SetParent(networkClient.PlayerObject.transform);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDropServerRpc(ulong requestingClientId, Vector3 dropOriginPosition, Vector3 throwDirectionVector)
    {
        if (bombState.Value != BombState.Carried || carrierClientId.Value != requestingClientId) return;

        if (NetworkObject.IsSpawned && transform.parent != null)
        {
            NetworkObject.TryRemoveParent();
        }
        else
        {
            transform.SetParent(null);
        }

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;

        Vector3 calculatedDropPosition = dropOriginPosition + throwDirectionVector * DROP_THROW_ORIGIN_FORWARD_OFFSET;
        Vector3 rayOrigin = calculatedDropPosition + Vector3.up * GROUND_CHECK_RAY_ORIGIN_HEIGHT;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, GROUND_CHECK_RAY_DISTANCE, ~0, QueryTriggerInteraction.Ignore))
        {
            calculatedDropPosition.y = Mathf.Max(calculatedDropPosition.y, groundHit.point.y + GROUND_OFFSET_Y);
        }

        Quaternion randomizedRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (_networkTransform != null)
        {
            _networkTransform.Teleport(calculatedDropPosition, randomizedRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(calculatedDropPosition, randomizedRotation);
        }

        bombState.Value = BombState.Dropped;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(requestingClientId, out NetworkClient networkClient) && networkClient.PlayerObject != null)
        {
            Collider playerCollider = networkClient.PlayerObject.GetComponent<Collider>();
            if (playerCollider != null && _bombCollider != null)
            {
                StartCoroutine(TemporarilyDisablePlayerCollision(playerCollider, _bombCollider));
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            Vector3 appliedForce = (throwDirectionVector * THROW_FORWARD_FORCE) + (Vector3.up * THROW_UPWARD_FORCE);
            _rigidbody.AddForce(appliedForce, ForceMode.Impulse);
        }

        SpawnDropVfxClientRpc(calculatedDropPosition);
    }

    private IEnumerator TemporarilyDisablePlayerCollision(Collider playerCollider, Collider bombColliderInstance)
    {
        Physics.IgnoreCollision(playerCollider, bombColliderInstance, true);
        yield return new WaitForSeconds(TEMPORARY_COLLISION_IGNORE_DURATION);
        if (playerCollider != null && bombColliderInstance != null)
        {
            Physics.IgnoreCollision(playerCollider, bombColliderInstance, false);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPlantServerRpc(ulong requestingClientId, NetworkBehaviourReference bombSiteReference, Vector3 targetPlantPosition, Quaternion targetPlantRotation)
    {
        if (bombState.Value != BombState.Carried || carrierClientId.Value != requestingClientId) return;

        if (bombSiteReference.TryGet(out BombSite site))
        {
            if (!site.IsPositionInside(targetPlantPosition))
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (NetworkObject.IsSpawned && transform.parent != null)
        {
            NetworkObject.TryRemoveParent();
        }
        else
        {
            transform.SetParent(null);
        }

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        if (_networkTransform != null)
        {
            _networkTransform.Teleport(targetPlantPosition, targetPlantRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(targetPlantPosition, targetPlantRotation);
        }

        plantedServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        bombState.Value = BombState.Planted;

        SynchronizePlantedTransformClientRpc(targetPlantPosition, targetPlantRotation);
        SpawnPlantVfxAndGlobalAudioClientRpc(targetPlantPosition);
    }

    [Rpc(SendTo.Server)]
    public void RequestStartDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Planted)
        {
            bombState.Value = BombState.Defusing;
            defuserClientId.Value = requestingClientId;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestStopDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Defusing && defuserClientId.Value == requestingClientId)
        {
            bombState.Value = BombState.Planted;
            defuserClientId.Value = UNASSIGNED_CARRIER_ID;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Defusing && defuserClientId.Value == requestingClientId)
        {
            bombState.Value = BombState.Defused;
            defuserClientId.Value = UNASSIGNED_CARRIER_ID;
            SpawnDefuseVfxClientRpc(transform.position);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SynchronizePlantedTransformClientRpc(Vector3 synchronizedPosition, Quaternion synchronizedRotation)
    {
        transform.SetParent(null);

        if (_networkTransform != null)
        {
            _networkTransform.Teleport(synchronizedPosition, synchronizedRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(synchronizedPosition, synchronizedRotation);
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnDropVfxClientRpc(Vector3 spawnPosition)
    {
        if (_dropVfxPrefab != null) Instantiate(_dropVfxPrefab, spawnPosition, Quaternion.identity);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnPlantVfxAndGlobalAudioClientRpc(Vector3 spawnPosition)
    {
        if (_plantVfxPrefab != null) Instantiate(_plantVfxPrefab, spawnPosition, Quaternion.identity);

        if (_globalPlantedClip != null)
        {
            GameObject soundHostObject = new GameObject("GlobalBombPlantedAudio");
            AudioSource temporaryAudioSource = soundHostObject.AddComponent<AudioSource>();

            if (_bombAudioSource != null && _bombAudioSource.outputAudioMixerGroup != null)
            {
                temporaryAudioSource.outputAudioMixerGroup = _bombAudioSource.outputAudioMixerGroup;
            }

            temporaryAudioSource.spatialBlend = 0f;
            temporaryAudioSource.volume = _globalPlantedVolume;
            temporaryAudioSource.clip = _globalPlantedClip;
            temporaryAudioSource.Play();
            Destroy(soundHostObject, _globalPlantedClip.length + AUDIO_CLEANUP_EXTRA_DELAY);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnDefuseVfxClientRpc(Vector3 spawnPosition)
    {
        if (_defuseVfxPrefab != null) Instantiate(_defuseVfxPrefab, spawnPosition, Quaternion.identity);
    }

    private IEnumerator PlantedBeepLoop()
    {
        while (bombState.Value == BombState.Planted || bombState.Value == BombState.Defusing)
        {
            double elapsedServerSeconds = NetworkManager.Singleton.ServerTime.Time - plantedServerTime.Value;
            float remainingDetonationSeconds = _detonationDuration - (float)elapsedServerSeconds;

            if (remainingDetonationSeconds <= MIN_REMAINING_DETONATION_TIME)
            {
                StopBeepRoutine();

                if (IsServer && bombState.Value != BombState.Defused)
                {
                    bombState.Value = BombState.Exploded;
                    SpawnExplosionVfxClientRpc(transform.position);
                }
                yield break;
            }

            float progressionFactor = Mathf.Clamp(1f - (remainingDetonationSeconds / _detonationDuration), MIN_NORMALIZED_PROGRESS, MAX_NORMALIZED_PROGRESS);
            float currentBeepInterval = Mathf.Lerp(_initialBeepInterval, _fastestBeepInterval, progressionFactor * progressionFactor);

            if (_plantedBeepClip != null && _bombAudioSource != null)
            {
                _bombAudioSource.PlayOneShot(_plantedBeepClip, _beepVolume);
            }

            yield return new WaitForSeconds(currentBeepInterval);
        }

        _beepCoroutine = null;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnExplosionVfxClientRpc(Vector3 spawnPosition)
    {
        UpdateRendererVisibility(false);
        if (_explosionVfxPrefab != null) Instantiate(_explosionVfxPrefab, spawnPosition, Quaternion.identity);
    }

    private void StopBeepRoutine()
    {
        if (_beepCoroutine != null)
        {
            StopCoroutine(_beepCoroutine);
            _beepCoroutine = null;
        }

        if (_bombAudioSource != null) _bombAudioSource.Stop();
    }

    public void ServerResetBomb(Vector3 targetResetPosition)
    {
        if (!IsServer) return;

        StopBeepRoutine();

        if (NetworkObject.IsSpawned && transform.parent != null)
        {
            NetworkObject.TryRemoveParent();
        }
        else
        {
            transform.SetParent(null);
        }

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;
        defuserClientId.Value = UNASSIGNED_CARRIER_ID;
        plantedServerTime.Value = 0;

        if (_networkTransform != null)
        {
            _networkTransform.Teleport(targetResetPosition, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = targetResetPosition;
        }

        bombState.Value = BombState.Dropped;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}