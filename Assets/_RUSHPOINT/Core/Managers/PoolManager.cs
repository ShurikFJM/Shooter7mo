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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SpawnImpact(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return;

        if (!_impactPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _impactPools[prefab] = pool;
        }

        GameObject impact = null;
        while (pool.Count > 0 && impact == null)
        {
            impact = pool.Dequeue();
        }

        if (impact == null)
        {
            impact = Instantiate(prefab);
            impact.transform.SetParent(transform, false);
        }

        ResetVisualComponents(impact);

        impact.transform.SetPositionAndRotation(position, rotation);
        impact.SetActive(true);

        PlayVisualComponents(impact);

        StartCoroutine(ReturnImpactRoutine(impact, prefab, lifetime));
    }

    private IEnumerator ReturnImpactRoutine(GameObject impact, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (impact == null) yield break;

        ResetVisualComponents(impact);
        impact.SetActive(false);

        if (prefab != null && _impactPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool.Enqueue(impact);
        }
    }

    public LineRenderer GetTracer(Material defaultMaterial)
    {
        LineRenderer tracer = null;
        while (_tracerPool.Count > 0 && tracer == null)
        {
            tracer = _tracerPool.Dequeue();
        }

        if (tracer == null)
        {
            GameObject tracerObj = new GameObject("PooledTracer");
            tracerObj.transform.SetParent(transform, false);
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

        tracer.positionCount = 0;
        tracer.enabled = true;
        tracer.gameObject.SetActive(true);
        return tracer;
    }

    public void ReturnTracer(LineRenderer tracer)
    {
        if (tracer == null) return;

        tracer.positionCount = 0;
        tracer.enabled = false;
        tracer.gameObject.SetActive(false);
        _tracerPool.Enqueue(tracer);
    }

    private static void ResetVisualComponents(GameObject target)
    {
        ParticleSystem[] particleSystems = target.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null) continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }

        TrailRenderer[] trails = target.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] != null)
            {
                trails[i].Clear();
            }
        }
    }

    private static void PlayVisualComponents(GameObject target)
    {
        ParticleSystem[] particleSystems = target.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null) continue;

            ps.Clear(true);
            ps.Play(true);
        }
    }
}