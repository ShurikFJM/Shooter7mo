using System.Collections;
using Unity.Netcode;
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
    [Header("Teams & Detonation")]
    [SerializeField] private Team _bombCarrierTeam = Team.Red;
    [SerializeField] private Team _defuseTeam = Team.Blue;
    [SerializeField] private float _detonationDuration = 45f;

    [Header("Visual Effects (VFX)")]
    [SerializeField] private GameObject _dropVfxPrefab;
    [SerializeField] private GameObject _plantVfxPrefab;
    [SerializeField] private GameObject _defuseVfxPrefab;
    [SerializeField] private GameObject _explosionVfxPrefab;

    [Header("Global Audio Announcement")]
    [SerializeField] private AudioClip _globalPlantedClip;

    [Header("Spatial Audio (Rhythmic Planted Beep)")]
    [SerializeField] private AudioSource _bombAudioSource;
    [SerializeField] private AudioClip _plantedBeepClip;
    [SerializeField] private float _initialBeepInterval = 1.0f;
    [SerializeField] private float _fastestBeepInterval = 0.08f;
    [SerializeField] private float _audioMinDistance = 5f;
    [SerializeField] private float _audioMaxDistance = 80f;

    public NetworkVariable<BombState> State = new NetworkVariable<BombState>(
        BombState.Dropped,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> CarrierClientId = new NetworkVariable<ulong>(
        9999,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<double> PlantedServerTime = new NetworkVariable<double>(
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

    private void Awake()
    {
        _bombCollider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>(true);

        ConfigureAudioSource();
    }

    private void ConfigureAudioSource()
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
        State.OnValueChanged += HandleStateChanged;
        ApplyStatePhysicsAndVisuals(State.Value);

        if (State.Value == BombState.Planted && _beepCoroutine == null)
        {
            _beepCoroutine = StartCoroutine(PlantedBeepRoutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= HandleStateChanged;
        StopBeeping();
    }

    private void HandleStateChanged(BombState previousState, BombState currentState)
    {
        ApplyStatePhysicsAndVisuals(currentState);

        switch (currentState)
        {
            case BombState.Carried:
            case BombState.Dropped:
                StopBeeping();
                break;

            case BombState.Planted:
                if (_beepCoroutine == null)
                {
                    _beepCoroutine = StartCoroutine(PlantedBeepRoutine());
                }
                break;

            case BombState.Defused:
            case BombState.Exploded:
                StopBeeping();
                break;
        }
    }

    private void ApplyStatePhysicsAndVisuals(BombState currentState)
    {
        bool isDropped = currentState == BombState.Dropped;
        bool isPlanted = currentState == BombState.Planted || currentState == BombState.Defusing;

        SetBombVisuals(isDropped || isPlanted);

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

    private void SetBombVisuals(bool visible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null) _renderers[i].enabled = visible;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPickupServerRpc(ulong clientId)
    {
        if (State.Value != BombState.Dropped) return;

        CarrierClientId.Value = clientId;
        State.Value = BombState.Carried;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
        {
            transform.position = client.PlayerObject.transform.position;
            transform.SetParent(client.PlayerObject.transform);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDropServerRpc(ulong clientId, Vector3 dropOrigin, Vector3 throwDirection)
    {
        if (State.Value != BombState.Carried || CarrierClientId.Value != clientId) return;

        transform.SetParent(null);
        CarrierClientId.Value = 9999;

        Vector3 targetDropPos = dropOrigin + throwDirection * 0.9f;

        if (Physics.Raycast(targetDropPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            targetDropPos.y = Mathf.Max(targetDropPos.y, hit.point.y + 0.2f);
        }

        transform.position = targetDropPos;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        State.Value = BombState.Dropped;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
        {
            Collider playerCollider = client.PlayerObject.GetComponent<Collider>();
            if (playerCollider != null && _bombCollider != null)
            {
                StartCoroutine(IgnorePlayerCollisionTemporarily(playerCollider, _bombCollider));
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            Vector3 forceVector = (throwDirection * 4f) + (Vector3.up * 2f);
            _rigidbody.AddForce(forceVector, ForceMode.Impulse);
        }

        TriggerDropVfxClientRpc(targetDropPos);
    }

    private IEnumerator IgnorePlayerCollisionTemporarily(Collider playerCol, Collider bombCol)
    {
        Physics.IgnoreCollision(playerCol, bombCol, true);
        yield return new WaitForSeconds(0.4f);
        if (playerCol != null && bombCol != null)
        {
            Physics.IgnoreCollision(playerCol, bombCol, false);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPlantServerRpc(ulong clientId, NetworkBehaviourReference siteRef, Vector3 plantPosition, Quaternion plantRotation)
    {
        if (State.Value != BombState.Carried || CarrierClientId.Value != clientId) return;

        transform.SetParent(null);
        CarrierClientId.Value = 9999;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(plantPosition, plantRotation);
        PlantedServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
        State.Value = BombState.Planted;

        TriggerPlantVfxAndGlobalAudioClientRpc(plantPosition);
    }

    [Rpc(SendTo.Server)]
    public void RequestDefuseServerRpc(ulong clientId)
    {
        if (State.Value != BombState.Planted && State.Value != BombState.Defusing) return;

        State.Value = BombState.Defused;
        TriggerDefuseVfxClientRpc(transform.position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerDropVfxClientRpc(Vector3 position)
    {
        if (_dropVfxPrefab != null)
        {
            Instantiate(_dropVfxPrefab, position, Quaternion.identity);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerPlantVfxAndGlobalAudioClientRpc(Vector3 position)
    {
        if (_plantVfxPrefab != null)
        {
            Instantiate(_plantVfxPrefab, position, Quaternion.identity);
        }

        if (_globalPlantedClip != null)
        {
            GameObject sfxHolder = new GameObject("GlobalBombPlantedAudio");
            AudioSource source = sfxHolder.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.clip = _globalPlantedClip;
            source.Play();
            Destroy(sfxHolder, _globalPlantedClip.length + 0.1f);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerDefuseVfxClientRpc(Vector3 position)
    {
        if (_defuseVfxPrefab != null)
        {
            Instantiate(_defuseVfxPrefab, position, Quaternion.identity);
        }
    }

    private IEnumerator PlantedBeepRoutine()
    {
        while (State.Value == BombState.Planted || State.Value == BombState.Defusing)
        {
            double elapsed = NetworkManager.Singleton.ServerTime.Time - PlantedServerTime.Value;
            float remaining = _detonationDuration - (float)elapsed;

            // Cortar inmediatamente en cuanto el tiempo llegue a cero
            if (remaining <= 0f)
            {
                StopBeeping();

                if (IsServer && State.Value != BombState.Defused)
                {
                    State.Value = BombState.Exploded;
                    TriggerExplosionClientRpc(transform.position);
                }
                yield break;
            }

            float progress = Mathf.Clamp01(1f - (remaining / _detonationDuration));
            float currentInterval = Mathf.Lerp(_initialBeepInterval, _fastestBeepInterval, progress * progress);

            if (_plantedBeepClip != null && _bombAudioSource != null)
            {
                _bombAudioSource.PlayOneShot(_plantedBeepClip);
            }

            yield return new WaitForSeconds(currentInterval);
        }

        _beepCoroutine = null;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerExplosionClientRpc(Vector3 position)
    {
        SetBombVisuals(false);

        if (_explosionVfxPrefab != null)
        {
            Instantiate(_explosionVfxPrefab, position, Quaternion.identity);
        }
    }

    private void StopBeeping()
    {
        if (_beepCoroutine != null)
        {
            StopCoroutine(_beepCoroutine);
            _beepCoroutine = null;
        }

        if (_bombAudioSource != null)
        {
            _bombAudioSource.Stop();
        }
    }

    public void ServerResetBomb(Vector3 resetPosition)
    {
        if (!IsServer) return;

        StopBeeping();
        transform.SetParent(null);
        CarrierClientId.Value = 9999;
        PlantedServerTime.Value = 0;
        transform.position = resetPosition;
        State.Value = BombState.Dropped;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}