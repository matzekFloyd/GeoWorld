using UnityEngine;

/// <summary>
/// Marks an instance as pool-managed. Added automatically by <see cref="GeoWorldObjectPools.Acquire"/>.
/// Call <see cref="ReleaseToPool"/> (or <see cref="GeoWorldObjectPools.Release"/>) instead of <see cref="Object.Destroy"/> when done.
/// </summary>
[DisallowMultipleComponent]
public sealed class PooledObject : MonoBehaviour
{
    GeoWorldObjectPools _owner;
    GameObject _prefab;

    internal void Initialize(GeoWorldObjectPools owner, GameObject prefab)
    {
        _owner = owner;
        _prefab = prefab;
    }

    internal bool IsManaged => _owner != null && _prefab != null;
    internal GameObject PrefabReference => _prefab;
    internal GeoWorldObjectPools Owner => _owner;

    public void ReleaseToPool()
    {
        GeoWorldObjectPools.Release(gameObject);
    }
}
