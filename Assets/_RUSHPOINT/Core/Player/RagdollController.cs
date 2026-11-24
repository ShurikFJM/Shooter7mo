using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody _mainHipsRigidbody;

    private Rigidbody[] _rigidbodies;
    private readonly List<Transform> _boneTransforms = new List<Transform>();
    private readonly List<Vector3> _defaultLocalPositions = new List<Vector3>();
    private readonly List<Quaternion> _defaultLocalRotations = new List<Quaternion>();
    private bool _hasCachedInitialPose;

    private void Awake()
    {
        CacheBonesIfNeeded();
        SetRagdollActive(false);
    }

    private void OnEnable()
    {
        CacheBonesIfNeeded();
    }

    private void CacheBonesIfNeeded()
    {
        if (_hasCachedInitialPose) return;

        if (_animator == null) _animator = GetComponent<Animator>();
        _rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        if (_mainHipsRigidbody == null && _rigidbodies.Length > 0)
        {
            _mainHipsRigidbody = _rigidbodies[0];
        }

        _boneTransforms.Clear();
        _defaultLocalPositions.Clear();
        _defaultLocalRotations.Clear();

        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            if (_rigidbodies[i] == null) continue;

            Transform bone = _rigidbodies[i].transform;
            _boneTransforms.Add(bone);
            _defaultLocalPositions.Add(bone.localPosition);
            _defaultLocalRotations.Add(bone.localRotation);
        }

        _hasCachedInitialPose = _boneTransforms.Count > 0;
    }

    public void SetRagdollActive(bool active)
    {
        CacheBonesIfNeeded();

        if (active)
        {
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                if (_rigidbodies[i] == null) continue;

                _rigidbodies[i].isKinematic = false;
                _rigidbodies[i].detectCollisions = true;
                _rigidbodies[i].linearVelocity = Vector3.zero;
                _rigidbodies[i].angularVelocity = Vector3.zero;
                _rigidbodies[i].WakeUp();
            }
        }
        else
        {
            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                if (_rigidbodies[i] == null) continue;

                if (!_rigidbodies[i].isKinematic)
                {
                    _rigidbodies[i].linearVelocity = Vector3.zero;
                    _rigidbodies[i].angularVelocity = Vector3.zero;
                }

                _rigidbodies[i].isKinematic = true;
                _rigidbodies[i].detectCollisions = true;
            }

            for (int i = 0; i < _boneTransforms.Count; i++)
            {
                if (_boneTransforms[i] == null) continue;

                _boneTransforms[i].localPosition = _defaultLocalPositions[i];
                _boneTransforms[i].localRotation = _defaultLocalRotations[i];
            }

            if (_animator != null)
            {
                _animator.enabled = true;
                if (gameObject.activeInHierarchy)
                {
                    _animator.Rebind();
                    _animator.Update(0f);
                }
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