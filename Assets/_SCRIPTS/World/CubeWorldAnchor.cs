using UnityEngine;

/// <summary>
/// Play-area bounds for the cube top face (terrain footprint). Configured by GeoWorld cube world setup.
/// </summary>
public class CubeWorldAnchor : MonoBehaviour
{
    [SerializeField] Vector3 playCenter;
    [SerializeField] float halfWidth = 260f;
    [SerializeField] float halfDepth = 235f;
    [SerializeField] float topFaceWorldY;

    public Vector3 PlayCenter => playCenter;
    public float HalfWidth => halfWidth;
    public float HalfDepth => halfDepth;
    public float TopFaceWorldY => topFaceWorldY;

    public Bounds WorldBounds => new Bounds(
        new Vector3(playCenter.x, topFaceWorldY, playCenter.z),
        new Vector3(halfWidth * 2f, 1f, halfDepth * 2f));

    public void Configure(Vector3 center, float width, float depth, float topFaceY)
    {
        playCenter = center;
        halfWidth = width * 0.5f;
        halfDepth = depth * 0.5f;
        topFaceWorldY = topFaceY;
    }

    public bool ContainsXZ(Vector3 worldPos, float margin = 0f)
    {
        var b = WorldBounds;
        return worldPos.x >= b.min.x - margin && worldPos.x <= b.max.x + margin
            && worldPos.z >= b.min.z - margin && worldPos.z <= b.max.z + margin;
    }
}
