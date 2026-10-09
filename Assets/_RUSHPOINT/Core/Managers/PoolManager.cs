using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> _impactPools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Queue<LineRenderer> _tracerPool = new Queue<LineRenderer>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void SpawnImpact(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return;

        if (!_impactPools.ContainsKey(prefab))
        {
            _impactPools[prefab] = new Queue<GameObject>();
        }

        GameObject impact;
        if (_impactPools[prefab].Count > 0)
        {
            impact = _impactPools[prefab].Dequeue();
        }
        else
        {
            impact = Instantiate(prefab);
            impact.transform.SetParent(transform);
        }

        impact.transform.SetPositionAndRotation(position, rotation);
        impact.SetActive(true);

        StartCoroutine(ReturnImpactRoutine(impact, prefab, lifetime));
    }

    private IEnumerator ReturnImpactRoutine(GameObject impact, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        impact.SetActive(false);
        _impactPools[prefab].Enqueue(impact);
    }

    public LineRenderer GetTracer(Material defaultMaterial)
    {
        LineRenderer tracer;
        if (_tracerPool.Count > 0)
        {
            tracer = _tracerPool.Dequeue();
        }
        else
        {
            GameObject tracerObj = new GameObject("PooledTracer");
            tracerObj.transform.SetParent(transform);
            tracer = tracerObj.AddComponent<LineRenderer>();
            tracer.startWidth = 0.02f;
            tracer.endWidth = 0.005f;
            tracer.startColor = Color.yellow;
            tracer.endColor = new Color(1f, 0.4f, 0f, 0f);
        }

        if (tracer.material == null || tracer.material.name == "Default-Material")
        {
            tracer.material = defaultMaterial != null ? defaultMaterial : new Material(Shader.Find("Sprites/Default"));
        }

        tracer.gameObject.SetActive(true);
        return tracer;
    }

    public void ReturnTracer(LineRenderer tracer)
    {
        tracer.gameObject.SetActive(false);
        _tracerPool.Enqueue(tracer);
    }
}