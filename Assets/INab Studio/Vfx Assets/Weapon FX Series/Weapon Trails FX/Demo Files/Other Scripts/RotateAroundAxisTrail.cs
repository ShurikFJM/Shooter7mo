using UnityEngine;
using INab.Common;

namespace INab.Demo
{
    [ExecuteInEditMode]
    public class RotateAroundAxisTrail : MonoBehaviour
    {
        public float rotationSpeed = 100f;
        public Vector3 axis = Vector3.up;

        public bool updateInEditor = false;

        private void Update()
        {
            if (updateInEditor || Application.isPlaying)
            {
                transform.Rotate(axis, rotationSpeed * Time.deltaTime);
            }
        }
    }
}