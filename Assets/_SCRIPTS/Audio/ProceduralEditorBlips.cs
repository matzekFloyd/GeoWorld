using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny synthetic clips so optional SFX paths are audible in the Editor when no <see cref="AudioClip"/> is assigned.
/// In builds, methods return null so shipped audio stays data-driven.
/// </summary>
static class ProceduralEditorBlips
{
    const int SampleRate = 44100;
    const float DurationSec = 0.045f;

    static readonly Dictionary<int, AudioClip> s_ByVariant = new Dictionary<int, AudioClip>(16);

    public static AudioClip Get(int variant)
    {
#if !UNITY_EDITOR
        return null;
#else
        variant = Mathf.Abs(variant);
        if (s_ByVariant.TryGetValue(variant, out var existing) && existing != null)
            return existing;

        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * DurationSec));
        var clip = AudioClip.Create($"EditorBlip_{variant}", n, 1, SampleRate, false);
        var data = new float[n];
        float hz = 220f * Mathf.Pow(2f, (variant % 18) / 12f);
        float twoPiF = 2f * Mathf.PI * hz / SampleRate;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.PI * t);
            data[i] = env * 0.22f * Mathf.Sin(twoPiF * i);
        }

        clip.SetData(data, 0);
        clip.hideFlags = HideFlags.HideAndDontSave;
        s_ByVariant[variant] = clip;
        return clip;
#endif
    }
}
