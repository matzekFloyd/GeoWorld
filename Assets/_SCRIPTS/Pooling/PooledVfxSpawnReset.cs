using UnityEngine;

/// <summary>
/// Resets visual FX on pooled (or freshly instantiated) objects so particles do not carry state
/// from the previous use. Called from <see cref="GeoWorldObjectPools.Acquire"/>; optionally add
/// <see cref="PooledVfxResetOnEnable"/> for prefabs spawned only via <c>Instantiate</c>.
/// </summary>
/// <remarks>
/// <see cref="ParticleSystem.Clear(bool)"/> uses <c>false</c> so sub-emitter-linked systems are not cleared
/// in an order that breaks sibling emitters. Trail renderers are handled separately via
/// <see cref="ClearTrailRenderers"/> so trail-only projectiles are not wiped on every pool acquire.
/// </remarks>
public static class PooledVfxSpawnReset
{
    /// <summary>
    /// Clears and replays every <see cref="ParticleSystem"/> under <paramref name="root"/> (including inactive).
    /// </summary>
    public static void Apply(GameObject root)
    {
        if (root == null)
            return;

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Clear(false);
            ps.Play();
        }
    }

    /// <summary>
    /// Clears <see cref="TrailRenderer"/> history (e.g. for GeoShot / GeoBlast). Call from a prefab-local
    /// component if needed; not part of <see cref="Apply"/> to avoid breaking trail-only visuals.
    /// </summary>
    public static void ClearTrailRenderers(GameObject root)
    {
        if (root == null)
            return;

        var trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
            trails[i].Clear();
    }
}

/// <summary>
/// Runs <see cref="PooledVfxSpawnReset.Apply"/> on enable (e.g. for <c>Instantiate</c> paths that bypass the pool).
/// Pool acquire already applies the same reset; avoid duplicating this component on strictly pool-only prefabs
/// unless double Clear/Play on spawn is acceptable.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class PooledVfxResetOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        PooledVfxSpawnReset.Apply(gameObject);
    }
}

/// <summary>
/// Clears pooled trail history on enable. Add to GeoShot / GeoBlast prefab roots if trail streaks should reset.
/// </summary>
[DefaultExecutionOrder(-999)]
public sealed class PooledTrailClearOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        PooledVfxSpawnReset.ClearTrailRenderers(gameObject);
    }
}
