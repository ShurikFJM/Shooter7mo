using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody _mainHipsRigidbody;

    private Rigidbody[] _rigidbodies;
    private Collider[] _colliders;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _rigidbodies = GetComponentsInChildren<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>();

        if (_mainHipsRigidbody == null && _rigidbodies.Length > 0)
        {
            _mainHipsRigidbody = _rigidbodies[0];
        }

        SetRagdollActive(false);
    }

    public void SetRagdollActive(bool active)
    {
        if (_animator != null)
        {
            _animator.enabled = !active;
        }

        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            if (_rigidbodies[i] == null) continue;

            _rigidbodies[i].isKinematic = !active;
            _rigidbodies[i].detectCollisions = true;

            if (active)
            {
#if UNITY_6000_0_OR_NEWER
                _rigidbodies[i].linearVelocity = Vector3.zero;
                _rigidbodies[i].angularVelocity = Vector3.zero;
#else
                _rigidbodies[i].velocity = Vector3.zero;
                _rigidbodies[i].angularVelocity = Vector3.zero;
#endif
            }
        }
    }

    public void TriggerRagdoll(Vector3 forceDirection, float forceMagnitude = 250f)
    {
        SetRagdollActive(true);

        if (_mainHipsRigidbody != null)
        {
            _mainHipsRigidbody.AddForce(forceDirection.normalized * forceMagnitude, ForceMode.Impulse);
        }
    }

    public void ResetRagdoll()
    {
        SetRagdollActive(false);
    }
}