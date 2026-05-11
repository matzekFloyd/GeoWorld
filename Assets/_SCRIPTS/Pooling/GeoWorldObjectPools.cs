using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple prefab-keyed pools: <see cref="Acquire"/> / <see cref="Release"/>, optional <see cref="Prewarm"/>.
/// Lives for the active scene (no DontDestroyOnLoad) so pooled instances are not orphaned across scene loads.
/// </summary>
/// <remarks>
/// Profiler (before/after PR): Unity Profiler → CPU Usage + Memory (GC Alloc), capture a late-game window
/// with high <c>level × enemiesPerLevel</c>, heavy GeoBlast/GeoShot use, and a level-up spawn burst.
/// Compare worst frame ms and GC.Alloc per frame vs. baseline on the same build configuration.
/// </remarks>
public sealed class GeoWorldObjectPools : MonoBehaviour
{
    static GeoWorldObjectPools s_Instance;

    public static GeoWorldObjectPools Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;
            s_Instance = FindAnyObjectByType<GeoWorldObjectPools>();
            if (s_Instance != null)
                return s_Instance;
            var go = new GameObject("GeoWorldObjectPools");
            s_Instance = go.AddComponent<GeoWorldObjectPools>();
            return s_Instance;
        }
    }

    readonly Dictionary<EntityId, Stack<GameObject>> _inactive = new Dictionary<EntityId, Stack<GameObject>>();
    Transform _poolRoot;

    void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        _poolRoot = transform;
    }

    void OnDestroy()
    {
        if (s_Instance == this)
            s_Instance = null;
    }

    /// <summary>Spawn or reuse an instance. Resets <see cref="Rigidbody"/> velocity when present.</summary>
    public GameObject Acquire(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null)
            return null;

        EntityId key = prefab.GetEntityId();
        GameObject inst;
        if (_inactive.TryGetValue(key, out var stack) && stack.Count > 0)
            inst = stack.Pop();
        else
        {
            inst = Object.Instantiate(prefab);
            var po = inst.GetComponent<PooledObject>();
            if (po == null)
                po = inst.AddComponent<PooledObject>();
            po.Initialize(this, prefab);
        }

        inst.transform.SetParent(parent, false);
        inst.transform.SetPositionAndRotation(position, rotation);
        ResetRigidbody(inst);
        inst.SetActive(true);
        return inst;
    }

    /// <summary>Returns an instance to its pool, or destroys it if not pool-managed.</summary>
    public static void Release(GameObject instance)
    {
        if (instance == null)
            return;
        var po = instance.GetComponent<PooledObject>();
        if (po == null || !po.IsManaged || po.Owner == null)
        {
            Object.Destroy(instance);
            return;
        }

        po.Owner.ReleaseInternal(instance, po.PrefabReference);
    }

    void ReleaseInternal(GameObject instance, GameObject prefabRef)
    {
        ResetRigidbody(instance);
        instance.SetActive(false);
        instance.transform.SetParent(_poolRoot, false);

        EntityId key = prefabRef.GetEntityId();
        if (!_inactive.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            _inactive[key] = stack;
        }

        stack.Push(instance);
    }

    static void ResetRigidbody(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic)
            return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>Instantiate-count instances inactive into the pool (Editor/runtime tuning).</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;
        for (var i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab);
            var po = go.GetComponent<PooledObject>();
            if (po == null)
                po = go.AddComponent<PooledObject>();
            po.Initialize(this, prefab);
            ReleaseInternal(go, prefab);
        }
    }
}
