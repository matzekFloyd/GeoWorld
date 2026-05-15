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

    public static Texture2D SoftParticleTexture
    {
        get
        {
            if (s_SoftTexture != null)
                return s_SoftTexture;

            s_SoftTexture = Resources.GetBuiltinResource<Texture2D>("Default-Particle.png");
            if (s_SoftTexture == null)
                s_SoftTexture = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");

#if UNITY_EDITOR
            if (s_SoftTexture == null)
            {
                s_SoftTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Standard Assets/ParticleSystems/Textures/ParticleCloudWhite.png");
            }
#endif
            return s_SoftTexture;
        }
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
