using UnityEngine;

/// <summary>
/// Shared soft-circle particle materials (avoids untextured shader quads).
/// </summary>
public static class GeoVfxSoftParticleMaterialUtil
{
    static Texture2D s_SoftTexture;
    static Material s_AuraMaterial;
    static Material s_DustMaterial;

    public static Material AuraMaterial =>
        s_AuraMaterial ?? (s_AuraMaterial = CreateMaterial(additive: true, new Color(0.35f, 0.92f, 1f, 0.38f)));

    public static Material DustMaterial =>
        s_DustMaterial ?? (s_DustMaterial = CreateMaterial(additive: false, new Color(0.72f, 0.68f, 0.62f, 0.55f)));

    const string ParticleCloudResourcePath = "GeoWorld/ParticleCloudWhite";
    const string ParticleCloudAssetPath =
        "Assets/Standard Assets/ParticleSystems/Textures/ParticleCloudWhite.png";

    public static Texture2D SoftParticleTexture
    {
        get
        {
            if (s_SoftTexture != null)
                return s_SoftTexture;

            // Unity 6 no longer ships Default-Particle built-ins (GetBuiltinResource logs errors).
            s_SoftTexture = Resources.Load<Texture2D>(ParticleCloudResourcePath);

#if UNITY_EDITOR
            if (s_SoftTexture == null)
            {
                s_SoftTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleCloudAssetPath);
            }
#endif

            if (s_SoftTexture == null)
                s_SoftTexture = CreateProceduralSoftCircle();

            return s_SoftTexture;
        }
    }

    static Texture2D CreateProceduralSoftCircle()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "GeoWorld_SoftParticle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var center = (size - 1) * 0.5f;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var a = r >= 1f ? 0f : Mathf.Clamp01(1f - r * r);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    static Material CreateMaterial(bool additive, Color tint)
    {
        string[] shaderPaths = additive
            ? new[]
            {
                "Legacy Shaders/Particles/Additive",
                "Mobile/Particles/Additive",
                "Particles/Standard Unlit",
                "Universal Render Pipeline/Particles/Unlit"
            }
            : new[]
            {
                "Legacy Shaders/Particles/Alpha Blended",
                "Mobile/Particles/Alpha Blended",
                "Particles/Standard Unlit",
                "Universal Render Pipeline/Particles/Unlit"
            };

        Shader shader = null;
        for (int i = 0; i < shaderPaths.Length; i++)
        {
            shader = Shader.Find(shaderPaths[i]);
            if (shader != null)
                break;
        }

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader);
        var tex = SoftParticleTexture;
        if (tex != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
        }

        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", tint);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);

        if (shader != null && shader.name.Contains("Universal"))
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", additive ? 1f : 0f);
        }

        return mat;
    }
}
