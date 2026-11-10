using UnityEngine;
using System.Collections.Generic;
using INab.Common;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace INab.Demo
{
    [ExecuteInEditMode]
    public class ShowcaseSpawnerTrail : MonoBehaviour
    {
        public List<GameObject> trailPrefabs = new List<GameObject>();
        public GameObject mesh;
        public Vector3 axis = Vector3.right;
        public float stepDistance = 2f;
        public Vector3 defaulLocalRotation = Vector3.zero;
        public Transform parentTransform;
        public float trailLength = 2f;
        public float rotationSpeed = 90f;

        [SerializeField]private List<GameObject> spawnedObjects = new List<GameObject>();

        public void OnEnable()
        {
            PlayAll();
        }

        public void OnValidate()
        {
            ChangleLengthAll();
            ChangleRotationSpeedAll();
        }

        public void SpawnPrefabs()
        {
            for (int i = 0; i < trailPrefabs.Count; i++)
            {
                GameObject spawned = Instantiate(mesh, Vector3.zero, Quaternion.Euler(defaulLocalRotation), parentTransform);
                spawned.GetComponent<WeaponTrailEffect>().SetNewTrailPrefab(trailPrefabs[i]);
                spawned.transform.localPosition = axis.normalized * stepDistance * i;
                spawned.name = trailPrefabs[i].name + "";
                spawned.SetActive(true);
                
                spawnedObjects.Add(spawned);
            }
        }

        public void AddTestPrefab()
        {
            GameObject spawned = Instantiate(mesh, Vector3.zero, Quaternion.Euler(defaulLocalRotation), parentTransform);
            spawned.GetComponent<WeaponTrailEffect>().SetNewTrailPrefab(trailPrefabs[0]);
            spawned.transform.localPosition = axis.normalized * stepDistance * spawnedObjects.Count;
            spawned.name = trailPrefabs[0].name + "";
            spawned.SetActive(true);

            spawnedObjects.Add(spawned);
        }

        public void DestroyPrefabs()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            spawnedObjects.Clear();
        }

        public void PlayAll()
        {
            ChangleRotationSpeedAll();

            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<WeaponTrailEffect>().SetTrailLength(trailLength);
                    obj.GetComponent<WeaponTrailEffect>().StartTrail(0.5f);
                }
            }
        }

        public void StopAll()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<WeaponTrailEffect>().StopTrail(0.1f);
                }
            }
        }

        public void ChangleLengthAll()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<WeaponTrailEffect>().SetTrailLength(trailLength);
                }
            }
        }

        public void ChangleRotationSpeedAll()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<RotateAroundAxisTrail>().rotationSpeed = rotationSpeed;
                }
            }
        }

        public void PauseAll()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<RotateAroundAxisTrail>().rotationSpeed = 0;
                    obj.GetComponent<WeaponTrailEffect>().vfxComponent.pause = true;
                }
            }
        }

        public void GetPrefabsFromChildren()
        {
            var comps = GetComponentsInChildren<WeaponTrailEffect>();
            foreach (var obj in comps)
            {
                if (obj != null)
                {
                    var prefab = obj.GetComponent<WeaponTrailEffect>().trailPrefab;
                    trailPrefabs.Add(prefab);
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ShowcaseSpawnerTrail))]
    public class ShowcaseSpawnerTrailEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            ShowcaseSpawnerTrail spawner = (ShowcaseSpawnerTrail)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Play All Trails"))
            {
                spawner.PlayAll();
            }

            if (GUILayout.Button("Pause All Trails"))
            {
                spawner.PauseAll();
            }

            if (GUILayout.Button("Stop All Trails"))
            {
                spawner.StopAll();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Test Prefabs"))
            {
                spawner.AddTestPrefab();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Spawn New Prefabs"))
            {
                spawner.DestroyPrefabs();
                spawner.SpawnPrefabs();
            }

            if (GUILayout.Button("Destroy Prefabs"))
            {
                spawner.DestroyPrefabs();
            }

            if (GUILayout.Button("Get Prefabs From Children"))
            {
                spawner.GetPrefabsFromChildren();
            }

        }
    }
#endif
}