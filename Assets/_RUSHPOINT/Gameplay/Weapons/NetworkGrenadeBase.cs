using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
public abstract class NetworkGrenadeBase : NetworkBehaviour
{
    private const float MIN_BOUNCE_SOUND_VELOCITY_SQR = 2.0f;

    [SerializeField] protected float _fuseTime = 2.5f;
    [SerializeField] protected bool _explodeOnContact = false;
    [SerializeField] protected LayerMask _collisionMask = ~0;

    [SerializeField] protected AudioSource _audioSource;
    [SerializeField] protected AudioClip _bounceSound;

    protected Rigidbody _rigidbody;
    protected ulong _throwerClientId;
    protected Team _throwerTeam = Team.Neutral;
    protected bool _hasExploded;
    protected float _currentFuseTimer;

    public ulong ThrowerClientId => _throwerClientId;
    public Team ThrowerTeam => _throwerTeam;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

    public virtual void InitializeGrenadeServer(ulong throwerId, Team team)
    {
        if (!IsServer) return;

        _throwerClientId = throwerId;
        _throwerTeam = team;
        _currentFuseTimer = _fuseTime;
        _hasExploded = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void LaunchGrenadeClientRpc(Vector3 throwVelocity, NetworkObjectReference throwerRef)
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = throwVelocity;
            _rigidbody.AddForce(throwVelocity, ForceMode.VelocityChange);
            _rigidbody.angularVelocity = Random.insideUnitSphere * 12f;
        }

        // Ignora colisiones con el jugador que la lanzó para que no se frene en su cabeza/manos
        if (throwerRef.TryGet(out NetworkObject throwerObj))
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol != null)
            {
                Collider[] throwerCols = throwerObj.GetComponentsInChildren<Collider>();
                for (int i = 0; i < throwerCols.Length; i++)
                {
                    if (throwerCols[i] != null)
                    {
                        Physics.IgnoreCollision(myCol, throwerCols[i], true);
                    }
                }
            }
        }
    }

    protected virtual void Update()
    {
        if (!IsServer || _hasExploded) return;

        if (!_explodeOnContact)
        {
            _currentFuseTimer -= Time.deltaTime;
            if (_currentFuseTimer <= 0f)
            {
                TriggerExplosionServer(transform.position, Vector3.up);
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.sqrMagnitude > MIN_BOUNCE_SOUND_VELOCITY_SQR)
        {
            PlayBounceSoundClientRpc();
        }

        if (IsServer && !_hasExploded && _explodeOnContact)
        {
            ContactPoint contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
            Vector3 normal = contact.normal != Vector3.zero ? contact.normal : Vector3.up;
            TriggerExplosionServer(collision.contacts.Length > 0 ? contact.point : transform.position, normal);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayBounceSoundClientRpc()
    {
        if (_audioSource != null && _bounceSound != null)
        {
            _audioSource.PlayOneShot(_bounceSound, 0.6f);
        }
    }

    protected void TriggerExplosionServer(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (_hasExploded) return;
        _hasExploded = true;

        OnExplodeServer(impactPoint, impactNormal);
        ExplodeVisualClientRpc(impactPoint, impactNormal);

        StartCoroutine(DespawnRoutine());
    }

    protected abstract void OnExplodeServer(Vector3 impactPoint, Vector3 impactNormal);

    [Rpc(SendTo.ClientsAndHost)]
    private void ExplodeVisualClientRpc(Vector3 impactPoint, Vector3 impactNormal)
    {
        PlayExplosionVisualEffect(impactPoint, impactNormal);
    }

    protected abstract void PlayExplosionVisualEffect(Vector3 impactPoint, Vector3 impactNormal);

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}