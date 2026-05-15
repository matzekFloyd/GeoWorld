using UnityEngine;

/// <summary>
/// Legacy edge kill trigger. Disabled when cube world setup runs.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArenaEdgeKillZone : MonoBehaviour
{
    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Intentionally empty; remove via GeoWorld → World → Refit Cube To Terrain.
    }
}
