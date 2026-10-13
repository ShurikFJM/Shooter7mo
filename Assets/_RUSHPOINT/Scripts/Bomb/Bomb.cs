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
    private const float GROUND_CHECK_RAY_ORIGIN_HEIGHT = 1f;
    private const float GROUND_CHECK_RAY_DISTANCE = 10f;
    private const float GROUND_OFFSET_Y = 0.12f;
    private const float DROP_FORWARD_OFFSET = 1.0f;
    private const float AUDIO_CLEANUP_EXTRA_DELAY = 0.1f;
    private const float DEFUSE_TOLERANCE_MARGIN = 0.25f;
    private const float CARRIER_OFFSET_Y = 0.95f;
    private const float CARRIER_OFFSET_Z = 0.24f;

    [SerializeField] private Team _bombCarrierTeam = Team.Red;
    [SerializeField] private Team _defuseTeam = Team.Blue;
    [SerializeField] private float _detonationDuration = 45f;
    [SerializeField] private float _defuseDuration = 5f;
    [SerializeField] private GameObject _dropVfxPrefab;
    [SerializeField] private GameObject _plantVfxPrefab;
    [SerializeField] private GameObject _defuseVfxPrefab;
    [SerializeField] private GameObject _explosionVfxPrefab;
    [SerializeField] private AudioClip _globalPlantedClip;
    [SerializeField] private float _globalPlantedVolume = 0.6f;
    [SerializeField] private AudioSource _bombAudioSource;
    [SerializeField] private AudioClip _plantedBeepClip;
    [SerializeField] private float _beepVolume = 1f;
    [SerializeField] private float _initialBeepInterval = 1f;
    [SerializeField] private float _fastestBeepInterval = 0.08f;
    [SerializeField] private float _audioMinDistance = 8f;
    [SerializeField] private float _audioMaxDistance = 50f;

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
    public float DefuseDuration => _defuseDuration;

    private Coroutine _beepCoroutine;
    private Collider _bombCollider;
    private Rigidbody _rigidbody;
    private Renderer[] _renderers;
    private NetworkTransform _networkTransform;
    private double _defuseStartTime;

    private void Awake()
    {
        _bombCollider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _networkTransform = GetComponent<NetworkTransform>();

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

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
            _bombAudioSource.rolloffMode = AudioRolloffMode.Linear;
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

    private void LateUpdate()
    {
        if (bombState.Value == BombState.Carried)
        {
            UpdateCarriedPosition();
        }
    }

    private void UpdateCarriedPosition()
    {
        ulong currentCarrier = carrierClientId.Value;
        if (currentCarrier == UNASSIGNED_CARRIER_ID) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(currentCarrier);
            if (playerObject != null)
            {
                Transform playerTransform = playerObject.transform;
                Vector3 targetPosition = playerTransform.position + (Vector3.up * CARRIER_OFFSET_Y) - (playerTransform.forward * CARRIER_OFFSET_Z);
                Quaternion targetRotation = playerTransform.rotation * Quaternion.Euler(0f, 180f, 0f);

                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }
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
        bool isVisible = currentState != BombState.Exploded;
        UpdateRendererVisibility(isVisible);

        if (_bombCollider != null)
        {
            _bombCollider.enabled = currentState == BombState.Dropped || currentState == BombState.Planted || currentState == BombState.Defusing;
        }

        if (_networkTransform != null)
        {
            _networkTransform.enabled = currentState != BombState.Carried;
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

        ApplyPhysicsAndVisualState(BombState.Carried);
        UpdateCarriedPosition();
    }

    [Rpc(SendTo.Server)]
    public void RequestDropServerRpc(ulong requestingClientId)
    {
        if (bombState.Value != BombState.Carried || carrierClientId.Value != requestingClientId) return;

        Vector3 dropPosition = transform.position;
        Vector3 forwardVector = Vector3.forward;

        if (NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(requestingClientId);
            if (playerObj != null)
            {
                Transform pTransform = playerObj.transform;
                forwardVector = pTransform.forward;
                forwardVector.y = 0f;
                forwardVector.Normalize();

                dropPosition = pTransform.position + (Vector3.up * 0.5f) + (forwardVector * DROP_FORWARD_OFFSET);
            }
        }

        Vector3 rayStart = dropPosition + (Vector3.up * GROUND_CHECK_RAY_ORIGIN_HEIGHT);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, GROUND_CHECK_RAY_DISTANCE, ~0, QueryTriggerInteraction.Ignore))
        {
            dropPosition.y = groundHit.point.y + GROUND_OFFSET_Y;
        }

        Quaternion dropRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;
        bombState.Value = BombState.Dropped;

        transform.SetPositionAndRotation(dropPosition, dropRotation);

        if (_networkTransform != null)
        {
            _networkTransform.enabled = true;
            _networkTransform.Teleport(dropPosition, dropRotation, transform.localScale);
        }

        ApplyPhysicsAndVisualState(BombState.Dropped);
        SynchronizeDropClientRpc(dropPosition, dropRotation);
        SpawnDropVfxClientRpc(dropPosition);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SynchronizeDropClientRpc(Vector3 synchronizedPosition, Quaternion synchronizedRotation)
    {
        transform.SetPositionAndRotation(synchronizedPosition, synchronizedRotation);
        ApplyPhysicsAndVisualState(BombState.Dropped);
    }

    [Rpc(SendTo.Server)]
    public void RequestPlantServerRpc(ulong requestingClientId, NetworkBehaviourReference bombSiteReference, Vector3 targetPlantPosition, Quaternion targetPlantRotation)
    {
        if (bombState.Value != BombState.Carried || carrierClientId.Value != requestingClientId) return;

        if (bombSiteReference.TryGet(out BombSite site))
        {
            if (!site.IsPositionInside(targetPlantPosition)) return;
        }
        else
        {
            return;
        }

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;
        transform.SetPositionAndRotation(targetPlantPosition, targetPlantRotation);

        if (_networkTransform != null)
        {
            _networkTransform.enabled = true;
            _networkTransform.Teleport(targetPlantPosition, targetPlantRotation, transform.localScale);
        }

        plantedServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        bombState.Value = BombState.Planted;

        ApplyPhysicsAndVisualState(BombState.Planted);
        SynchronizePlantedTransformClientRpc(targetPlantPosition, targetPlantRotation);
        SpawnPlantVfxAndGlobalAudioClientRpc(targetPlantPosition);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SynchronizePlantedTransformClientRpc(Vector3 synchronizedPosition, Quaternion synchronizedRotation)
    {
        transform.SetPositionAndRotation(synchronizedPosition, synchronizedRotation);
        ApplyPhysicsAndVisualState(BombState.Planted);
    }

    [Rpc(SendTo.Server)]
    public void RequestStartDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Planted)
        {
            bombState.Value = BombState.Defusing;
            defuserClientId.Value = requestingClientId;
            _defuseStartTime = NetworkManager.Singleton.ServerTime.Time;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestStopDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Defusing && defuserClientId.Value == requestingClientId)
        {
            bombState.Value = BombState.Planted;
            defuserClientId.Value = UNASSIGNED_CARRIER_ID;
            _defuseStartTime = 0;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDefuseServerRpc(ulong requestingClientId)
    {
        if (bombState.Value == BombState.Defusing && defuserClientId.Value == requestingClientId)
        {
            double elapsedDefuse = NetworkManager.Singleton.ServerTime.Time - _defuseStartTime;
            if (elapsedDefuse < (_defuseDuration - DEFUSE_TOLERANCE_MARGIN)) return;

            bombState.Value = BombState.Defused;
            defuserClientId.Value = UNASSIGNED_CARRIER_ID;
            SpawnDefuseVfxClientRpc(transform.position);
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

        carrierClientId.Value = UNASSIGNED_CARRIER_ID;
        defuserClientId.Value = UNASSIGNED_CARRIER_ID;
        plantedServerTime.Value = 0;
        _defuseStartTime = 0;

        Vector3 groundSafePosition = targetResetPosition;
        Vector3 rayStart = targetResetPosition + (Vector3.up * 1f);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            groundSafePosition.y = hit.point.y + GROUND_OFFSET_Y;
        }

        transform.SetPositionAndRotation(groundSafePosition, Quaternion.identity);

        if (_networkTransform != null)
        {
            _networkTransform.enabled = true;
            _networkTransform.Teleport(groundSafePosition, Quaternion.identity, transform.localScale);
        }

        bombState.Value = BombState.Dropped;
        ApplyPhysicsAndVisualState(BombState.Dropped);

        ResetBombClientRpc(groundSafePosition);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetBombClientRpc(Vector3 targetResetPosition)
    {
        StopBeepRoutine();
        transform.SetPositionAndRotation(targetResetPosition, Quaternion.identity);
        ApplyPhysicsAndVisualState(BombState.Dropped);
    }
}