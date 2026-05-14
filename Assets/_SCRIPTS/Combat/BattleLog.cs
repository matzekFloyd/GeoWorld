using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bounded read-only combat feed (bottom-right HUD). Append from <see cref="CombatFeedback"/> and central
/// death hooks — extend by adding a method here, not by calling uGUI directly from gameplay scripts.
/// Toggle visibility with <b>L</b> (same session as minimap <b>M</b>); preference key <see cref="VisiblePlayerPrefsKey"/>.
/// </summary>
public static class BattleLog
{
    public const string VisiblePlayerPrefsKey = "GeoWorld.BattleLogVisible";

    const int BufferCapacity = 40;
    /// <summary>Shown lines when combat reduced motion is off (full buffer may be larger but capped here for layout).</summary>
    const int DisplayLinesNormal = 36;
    /// <summary>Shorter on-screen history when <see cref="CombatFeedback.ReducedMotion"/> is on.</summary>
    const int DisplayLinesReducedMotion = 20;

    static readonly string[] s_buffer = new string[BufferCapacity];
    static int s_start;
    static int s_count;

    static Text s_text;
    static GameObject s_panelRoot;
    static RectTransform s_panelRt;
    static Canvas s_canvas;
    static Vector2 s_anchoredPositionBase;
    static Rect s_lastSafeArea;
    static float s_lastCanvasPixelW = -1f;

    static readonly Dictionary<EntityId, float> s_lightEnemyHitLogTime = new Dictionary<EntityId, float>(96);
    const float LightEnemyHitLogInterval = 0.28f;

    static readonly StringBuilder s_sb = new StringBuilder(2048);

    static GameplayHudView s_boundOwner;

    public static bool UserWantsVisible =>
        PlayerPrefs.GetInt(VisiblePlayerPrefsKey, 1) != 0;

    public static void SetUserVisible(bool visible)
    {
        PlayerPrefs.SetInt(VisiblePlayerPrefsKey, visible ? 1 : 0);
        ApplyPanelActive();
    }

    /// <summary>Bind after uGUI build. Clears the ring buffer.</summary>
    internal static void Bind(GameObject panelRoot, Text text, RectTransform panelRt, Canvas canvas, Vector2 anchoredPositionBase, GameplayHudView owner)
    {
        s_boundOwner = owner;
        s_panelRoot = panelRoot;
        s_text = text;
        s_panelRt = panelRt;
        s_canvas = canvas;
        s_anchoredPositionBase = anchoredPositionBase;
        ClearBuffer();
        ApplyPanelActive();
        RefreshAnchoredPosition();
    }

    internal static void Unbind(GameplayHudView owner)
    {
        if (owner != s_boundOwner)
            return;
        s_boundOwner = null;
        s_panelRoot = null;
        s_text = null;
        s_panelRt = null;
        s_canvas = null;
        ClearBuffer();
        s_lightEnemyHitLogTime.Clear();
        s_lastCanvasPixelW = -1f;
    }

    internal static void ProcessToggleHotkey()
    {
        if (Keyboard.current == null || !Keyboard.current.lKey.wasPressedThisFrame)
            return;
        SetUserVisible(!UserWantsVisible);
    }

    internal static void TickSafeAreaLayout()
    {
        if (s_panelRt == null || s_canvas == null)
            return;
        Rect sa = Screen.safeArea;
        float cw = s_canvas.pixelRect.width;
        if (sa == s_lastSafeArea && Mathf.Approximately(cw, s_lastCanvasPixelW))
            return;
        RefreshAnchoredPosition();
    }

    static void RefreshAnchoredPosition()
    {
        Rect sa = Screen.safeArea;
        float sf = s_canvas.scaleFactor > 0.01f ? s_canvas.scaleFactor : 1f;
        float insetRight = Mathf.Max(0f, Screen.width - sa.xMax) / sf;
        float insetBottom = Mathf.Max(0f, sa.yMin) / sf;
        s_panelRt.anchoredPosition = new Vector2(s_anchoredPositionBase.x - insetRight, s_anchoredPositionBase.y + insetBottom);
        s_lastSafeArea = sa;
        s_lastCanvasPixelW = s_canvas.pixelRect.width;
    }

    static void ApplyPanelActive()
    {
        if (s_panelRoot != null)
            s_panelRoot.SetActive(UserWantsVisible);
    }

    public static void AppendPlayerDamageTaken(float amount, CombatHitSeverity severity, bool isEnemyCritical = false)
    {
        if (!UserWantsVisible)
            return;
        string sev = SeveritySuffix(severity);
        if (isEnemyCritical)
            AppendLine($"You took {FmtDamage(amount)} damage{sev} (enemy critical hit).");
        else
            AppendLine($"You took {FmtDamage(amount)} damage{sev}.");
    }

    public static void AppendEnemyHit(Transform enemyRoot, float damage, CombatHitSeverity severity, bool isCritical = false)
    {
        if (!UserWantsVisible || enemyRoot == null)
            return;
        if (severity == CombatHitSeverity.Light && !isCritical && !ShouldLogLightEnemyHitNow(enemyRoot))
            return;
        string name = CleanObjectName(enemyRoot.gameObject);
        string sev = SeveritySuffix(severity);
        if (isCritical)
            AppendLine($"{name} took {FmtDamage(damage)} damage{sev} (critical hit).");
        else
            AppendLine($"{name} took {FmtDamage(damage)} damage{sev}.");
    }

    public static void AppendEnemyDefeated(EnemyCharacter ec)
    {
        if (!UserWantsVisible)
            return;
        if (ec != null)
            ForgetEnemyThrottle(ec.gameObject);
        if (ec == null)
        {
            AppendLine("Enemy defeated.");
            return;
        }

        string name = CleanObjectName(ec.gameObject);
        if (ec.isBoss)
            AppendLine($"Boss defeated: {name}.");
        else if (ec.iAmGreaterEnemy)
            AppendLine($"Elite defeated: {name}.");
        else
            AppendLine($"Enemy defeated: {name}.");
    }

    /// <summary>Single custom line (skill procs, warnings, etc.). Prefer typed helpers above when they exist.</summary>
    public static void AppendCustom(string line)
    {
        if (!UserWantsVisible || string.IsNullOrEmpty(line))
            return;
        AppendLine(line.Trim());
    }

    static bool ShouldLogLightEnemyHitNow(Transform enemyRoot)
    {
        EntityId id = enemyRoot.gameObject.GetEntityId();
        float t = Time.unscaledTime;
        if (s_lightEnemyHitLogTime.TryGetValue(id, out float last) && t - last < LightEnemyHitLogInterval)
            return false;
        s_lightEnemyHitLogTime[id] = t;
        PruneEnemyThrottleMap();
        return true;
    }

    static void ForgetEnemyThrottle(GameObject go)
    {
        if (go == null)
            return;
        s_lightEnemyHitLogTime.Remove(go.GetEntityId());
    }

    static void PruneEnemyThrottleMap()
    {
        if (s_lightEnemyHitLogTime.Count <= 140)
            return;
        float t = Time.unscaledTime;
        var remove = new List<EntityId>(32);
        foreach (var kv in s_lightEnemyHitLogTime)
        {
            if (t - kv.Value > 4f)
                remove.Add(kv.Key);
        }

        for (int i = 0; i < remove.Count; i++)
            s_lightEnemyHitLogTime.Remove(remove[i]);
    }

    static string SeveritySuffix(CombatHitSeverity severity)
    {
        return severity switch
        {
            CombatHitSeverity.Medium => " (medium hit)",
            CombatHitSeverity.Heavy => " (heavy hit)",
            _ => "",
        };
    }

    static string FmtDamage(float amount) =>
        amount.ToString("F0", CultureInfo.InvariantCulture);

    static string CleanObjectName(GameObject go)
    {
        if (go == null)
            return "Unknown";
        string n = go.name;
        const string clone = "(Clone)";
        if (n.EndsWith(clone))
            n = n.Substring(0, n.Length - clone.Length).TrimEnd();
        return string.IsNullOrEmpty(n) ? "Unknown" : n;
    }

    static void AppendLine(string line)
    {
        if (s_text == null || string.IsNullOrEmpty(line))
            return;

        if (s_count < BufferCapacity)
        {
            int idx = (s_start + s_count) % BufferCapacity;
            s_buffer[idx] = line;
            s_count++;
        }
        else
        {
            s_buffer[s_start] = line;
            s_start = (s_start + 1) % BufferCapacity;
        }

        RebuildVisual();
    }

    static void ClearBuffer()
    {
        s_start = 0;
        s_count = 0;
        if (s_text != null)
            s_text.text = "";
    }

    static int DisplayLineBudget() =>
        CombatFeedback.ReducedMotion ? DisplayLinesReducedMotion : DisplayLinesNormal;

    static void RebuildVisual()
    {
        if (s_text == null)
            return;

        int budget = Mathf.Min(DisplayLineBudget(), BufferCapacity);
        int skip = Mathf.Max(0, s_count - budget);
        s_sb.Length = 0;
        for (int i = 0; i < s_count; i++)
        {
            if (i < skip)
                continue;
            int idx = (s_start + i) % BufferCapacity;
            if (s_sb.Length > 0)
                s_sb.Append('\n');
            s_sb.Append(s_buffer[idx]);
        }

        string built = s_sb.ToString();
        if (s_text.text != built)
            s_text.text = built;
    }
}
