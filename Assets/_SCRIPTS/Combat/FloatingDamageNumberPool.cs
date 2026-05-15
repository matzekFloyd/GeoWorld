using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stack pool of uGUI <see cref="FloatingDamageNumber"/> under <see cref="GameplayHudView.DamageNumbersHost"/>.
/// No per-spawn Instantiate; optional prewarm after HUD build.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloatingDamageNumberPool : MonoBehaviour
{
    public static FloatingDamageNumberPool Instance { get; private set; }

    [SerializeField] int _prewarm = 28;
    [SerializeField] int _maxActive = 72;

    Transform _storage;
    RectTransform _hostRt;
    bool _bound;
    readonly Stack<FloatingDamageNumber> _free = new Stack<FloatingDamageNumber>();
    int _active;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        var storageGo = new GameObject("DamageNumberPoolStorage");
        storageGo.transform.SetParent(transform, false);
        _storage = storageGo.transform;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BindAndPrewarm(RectTransform damageNumbersHost)
    {
        if (damageNumbersHost == null || _bound)
            return;
        _hostRt = damageNumbersHost;
        _bound = true;
        while (_free.Count < _prewarm)
            _free.Push(CreateInstance());
    }

    public static Vector3 GetDamagePopupWorldPosition(Transform enemy)
    {
        if (enemy == null)
            return Vector3.zero;
        var r = enemy.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            var b = r.bounds;
            return b.center + Vector3.up * (b.extents.y + 0.35f);
        }
        return enemy.position + Vector3.up * 1.9f;
    }

    public void SpawnEnemyDamage(Transform enemyRoot, float damage, CombatHitSeverity severity, bool isBoss, bool isCritical = false)
    {
        if (!_bound || _hostRt == null)
            return;
        if (_active >= _maxActive)
            return;

        StyleEnemy(damage, severity, isBoss, isCritical, out var txt, out var col, out float font, out float rise, out float life);
        var world = GetDamagePopupWorldPosition(enemyRoot);
        SpawnInternal(world, txt, col, life, rise, font);
    }

    public void SpawnPlayerDamageTaken(Transform playerRoot, float damage, CombatHitSeverity severity, bool isEnemyCritical = false)
    {
        if (!_bound || _hostRt == null || playerRoot == null)
            return;
        if (_active >= _maxActive)
            return;

        var world = playerRoot.position + Vector3.up * 1.7f;
        int d = Mathf.Max(1, Mathf.RoundToInt(damage));
        string txt = isEnemyCritical ? "-" + d + "!" : "-" + d.ToString();
        StylePlayer(severity, isEnemyCritical, out var col, out float font, out float rise, out float life);
        SpawnInternal(world, txt, col, life, rise, font);
    }

    void SpawnInternal(Vector3 world, string text, Color color, float life, float rise, float font)
    {
        if (CombatFeedback.ReducedMotion)
        {
            life *= 0.78f;
            rise *= 0.55f;
            font *= 0.92f;
        }

        EnsureDamageNumbersHostActive();
        var n = _free.Count > 0 ? _free.Pop() : CreateInstance();
        _active++;
        n.Show(_hostRt, world, text, color, life, rise, font);
    }

    /// <summary>Activates the damage-numbers host and canvas ancestors (HUD may be hidden until the first <see cref="UserInterface"/> refresh).</summary>
    internal void EnsureDamageNumbersHostActive()
    {
        if (_hostRt == null)
            return;
        var t = _hostRt.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    internal Coroutine StartNumberRoutine(IEnumerator routine) => StartCoroutine(routine);

    internal void StopNumberRoutine(Coroutine routine)
    {
        if (routine != null)
            StopCoroutine(routine);
    }

    static void StyleEnemy(float damage, CombatHitSeverity severity, bool isBoss, bool isCritical, out string text, out Color color, out float font, out float rise, out float life)
    {
        int d = Mathf.Max(1, Mathf.RoundToInt(damage));
        text = isCritical ? d + "!" : d.ToString();
        life = severity == CombatHitSeverity.Heavy ? 1.05f : severity == CombatHitSeverity.Medium ? 0.92f : 0.82f;
        rise = severity == CombatHitSeverity.Heavy ? 3.1f : severity == CombatHitSeverity.Medium ? 2.35f : 2f;
        font = severity == CombatHitSeverity.Heavy ? 30f : severity == CombatHitSeverity.Medium ? 26f : 22f;
        if (isCritical)
        {
            color = new Color(1f, 0.92f, 0.2f, 1f);
            font *= 1.14f;
            rise *= 1.08f;
            life *= 1.05f;
        }
        else
        {
            color = severity switch
            {
                CombatHitSeverity.Heavy => new Color(1f, 0.42f, 0.1f, 1f),
                CombatHitSeverity.Medium => new Color(1f, 0.78f, 0.32f, 1f),
                _ => new Color(1f, 0.94f, 0.72f, 1f),
            };
        }
        if (isBoss)
            color = Color.Lerp(color, new Color(0.92f, 0.4f, 1f, 1f), isCritical ? 0.18f : 0.32f);
    }

    static void StylePlayer(CombatHitSeverity severity, bool isEnemyCritical, out Color color, out float font, out float rise, out float life)
    {
        life = severity == CombatHitSeverity.Heavy ? 1f : severity == CombatHitSeverity.Medium ? 0.88f : 0.78f;
        rise = severity == CombatHitSeverity.Heavy ? 2.9f : 2.1f;
        font = severity == CombatHitSeverity.Heavy ? 29f : severity == CombatHitSeverity.Medium ? 25f : 21f;
        if (isEnemyCritical)
        {
            color = new Color(1f, 0.55f, 0.12f, 1f);
            font *= 1.12f;
            rise *= 1.08f;
            life *= 1.05f;
        }
        else
        {
            color = severity switch
            {
                CombatHitSeverity.Heavy => new Color(1f, 0.2f, 0.15f, 1f),
                CombatHitSeverity.Medium => new Color(1f, 0.45f, 0.38f, 1f),
                _ => new Color(1f, 0.65f, 0.58f, 1f),
            };
        }
    }

    internal void Release(FloatingDamageNumber n)
    {
        if (n == null)
            return;
        n.NotifyReturnedToPool();
        n.gameObject.SetActive(false);
        n.transform.SetParent(_storage, false);
        _active = Mathf.Max(0, _active - 1);
        _free.Push(n);
    }

    FloatingDamageNumber CreateInstance()
    {
        var go = new GameObject("FloatingDamageNumber", typeof(RectTransform));
        go.transform.SetParent(_storage, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(240f, 80f);

        var text = go.AddComponent<Text>();
        text.font = GameplayHudView.HudUiFont;
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.alignByGeometry = true;
        text.supportRichText = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;

        var fn = go.AddComponent<FloatingDamageNumber>();
        fn.Initialize(this, text, outline, rt);
        go.SetActive(false);
        return fn;
    }
}
