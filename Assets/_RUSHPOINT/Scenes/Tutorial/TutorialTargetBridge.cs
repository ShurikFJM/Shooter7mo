using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTargetBridge : MonoBehaviour
{
    [SerializeField] private TutorialTarget _parentTarget;

    private void Awake()
    {
        if (_parentTarget == null)
        {
            _parentTarget = GetComponentInParent<TutorialTarget>();
        }
    }

    public void ApplyDamage(float damageAmount)
    {
        if (_parentTarget != null)
        {
            _parentTarget.TakeDamage(damageAmount);
        }
    }
}