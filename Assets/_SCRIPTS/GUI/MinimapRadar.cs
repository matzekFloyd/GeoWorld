using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if !(UNITY_WEBGL && !UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER)
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lightweight bottom-left radar: world XZ projected onto uGUI (no extra RenderTexture or scene camera).
/// Uses <see cref="EnemyGenerator.targets"/> as the hostile list; bounds from <see cref="EnemyGenerator.TryGetArenaBoundsXZ"/>.
/// Throttled updates for WebGL; optional visibility/opacity via PlayerPrefs; <b>M</b> toggles minimap when input is available.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinimapRadar : MonoBehaviour
{
    public const string VisiblePlayerPrefsKey = "GeoWorld.MinimapVisible";
    public const string OpacityPlayerPrefsKey = "GeoWorld.MinimapOpacityPct";

    [SerializeField, Min(0.02f)] float _refreshIntervalUnscaled = 0.066f;
    [SerializeField, Range(8, 128)] int _maxBlips = 80;
    [SerializeField] Vector2 _radarSize = new Vector2(132f, 132f);
    [Tooltip("Equal inset from the screen bottom-left corner: same pixels from the left edge and from the bottom edge.")]
    [SerializeField] float _cornerInsetFromBottomLeft = 16f;

    /// <summary>Same inset used for the skill strip bottom (see <see cref="GameplayHudView"/>).</summary>
    public float CornerInsetFromBottomLeft => Mathf.Max(0f, _cornerInsetFromBottomLeft);

    GameObject _root;
    CanvasGroup _canvasGroup;
    RectTransform _mapAreaRt;
    RectTransform _playerDotRt;
    RectTransform _playerNeedleRt;
    Image[] _blips;
    Text _title;
    bool _uiBuilt;
    bool _boundsReady;
    Vector3 _worldCenter;
    Vector2 _halfExtents = new Vector2(160f, 160f);

    EnemyGenerator _generator;
    GameObject _playerGo;
    Transform _playerTransform;
    bool _gameplayHudVisible = true;

    float _nextRefreshUnscaled;
    readonly List<Transform> _scratch = new List<Transform>(128);
    readonly DistComparer _distComparer = new DistComparer();

    sealed class DistComparer : IComparer<Transform>
    {
        public Vector3 Reference;
        public int Compare(Transform a, Transform b)
        {
            var da = (a.position - Reference).sqrMagnitude;
            var db = (b.position - Reference).sqrMagnitude;
            return da.CompareTo(db);
        }
    }

    /// <summary>1 = show minimap when HUD is visible (default). Set via settings or <see cref="SetMinimapUserVisible"/>.</summary>
    public static bool UserWantsMinimapVisible =>
        PlayerPrefs.GetInt(VisiblePlayerPrefsKey, 1) != 0;

    public static void SetMinimapUserVisible(bool visible) =>
        PlayerPrefs.SetInt(VisiblePlayerPrefsKey, visible ? 1 : 0);

    /// <summary>0–100 alpha contribution for the whole radar panel (default 90).</summary>
    public static int MinimapOpacityPercent
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(OpacityPlayerPrefsKey, 90), 0, 100);
        set => PlayerPrefs.SetInt(OpacityPlayerPrefsKey, Mathf.Clamp(value, 0, 100));
    }

    public void SetGameplayHudVisible(bool visible) => _gameplayHudVisible = visible;

    public void SetMinimapOpacityPercent(int zeroTo100) => MinimapOpacityPercent = zeroTo100;

    /// <summary>Called from <see cref="GameplayHudView.EnsureBuilt"/> after the gameplay canvas exists.</summary>
    public void BuildUi(RectTransform canvasRt)
    {
        if (_uiBuilt || canvasRt == null)
            return;

        _root = new GameObject("MinimapRadar", typeof(RectTransform));
        var rootRt = (RectTransform)_root.transform;
        rootRt.SetParent(canvasRt, false);
        rootRt.anchorMin = new Vector2(0f, 0f);
        rootRt.anchorMax = new Vector2(0f, 0f);
        rootRt.pivot = new Vector2(0f, 0f);
        rootRt.sizeDelta = _radarSize;
        var inset = CornerInsetFromBottomLeft;
        rootRt.anchoredPosition = new Vector2(inset, inset);

        _canvasGroup = _root.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        var bgGo = CreateChild(rootRt, "Background");
        var bgRt = bgGo.GetComponent<RectTransform>();
        StretchFull(bgRt);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = GameplayHudView.HudSolidSprite;
        bgImg.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);
        bgImg.raycastTarget = false;

        var borderGo = CreateChild(rootRt, "Border");
        var borderRt = borderGo.GetComponent<RectTransform>();
        StretchFullWithMargin(borderRt, 1f);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.sprite = GameplayHudView.HudSolidSprite;
        borderImg.color = new Color(0.35f, 0.55f, 0.75f, 0.55f);
        borderImg.raycastTarget = false;

        var mapGo = CreateChild(rootRt, "MapArea");
        _mapAreaRt = mapGo.GetComponent<RectTransform>();
        StretchFullWithMargin(_mapAreaRt, 10f);

        var blipHost = CreateChild(_mapAreaRt, "Blips");
        var blipHostRt = blipHost.GetComponent<RectTransform>();
        StretchFull(blipHostRt);

        _blips = new Image[_maxBlips];
        for (var i = 0; i < _maxBlips; i++)
        {
            var bGo = CreateChild(blipHostRt, "Blip" + i);
            var brt = bGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(7f, 7f);
            var img = bGo.AddComponent<Image>();
            img.sprite = GameplayHudView.HudSolidSprite;
            img.raycastTarget = false;
            img.color = new Color(1f, 0.45f, 0.2f, 0.95f);
            bGo.SetActive(false);
            _blips[i] = img;
        }

        var playerGo = CreateChild(_mapAreaRt, "Player");
        _playerDotRt = playerGo.GetComponent<RectTransform>();
        _playerDotRt.anchorMin = _playerDotRt.anchorMax = new Vector2(0.5f, 0.5f);
        _playerDotRt.sizeDelta = new Vector2(10f, 10f);
        var pImg = playerGo.AddComponent<Image>();
        pImg.sprite = GameplayHudView.HudSolidSprite;
        pImg.color = new Color(0.25f, 0.95f, 1f, 1f);
        pImg.raycastTarget = false;

        var needleGo = CreateChild(_playerDotRt, "Facing");
        _playerNeedleRt = needleGo.GetComponent<RectTransform>();
        _playerNeedleRt.anchorMin = new Vector2(0.5f, 1f);
        _playerNeedleRt.anchorMax = new Vector2(0.5f, 1f);
        _playerNeedleRt.pivot = new Vector2(0.5f, 0f);
        _playerNeedleRt.sizeDelta = new Vector2(4f, 12f);
        _playerNeedleRt.anchoredPosition = new Vector2(0f, 2f);
        var nImg = needleGo.AddComponent<Image>();
        nImg.sprite = GameplayHudView.HudSolidSprite;
        nImg.color = new Color(1f, 0.95f, 0.35f, 0.95f);
        nImg.raycastTarget = false;

        var titleGo = CreateChild(rootRt, "Title");
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 16f);
        titleRt.anchoredPosition = new Vector2(0f, -2f);
        _title = titleGo.AddComponent<Text>();
        _title.font = GameplayHudView.HudUiFont;
        _title.fontSize = 11;
        _title.fontStyle = FontStyle.Bold;
        _title.alignment = TextAnchor.MiddleCenter;
        _title.color = new Color(0.85f, 0.9f, 1f, 0.75f);
        _title.horizontalOverflow = HorizontalWrapMode.Overflow;
        _title.verticalOverflow = VerticalWrapMode.Overflow;
        _title.raycastTarget = false;
        _title.alignByGeometry = true;
        _title.text = "RADAR";

        rootRt.SetAsLastSibling();
        _uiBuilt = true;
    }

    void Start()
    {
        ResolveReferences();
        TryInitBounds();
    }

    void Update()
    {
        if (!_uiBuilt || _root == null)
            return;

        if (WasMinimapTogglePressed())
            SetMinimapUserVisible(!UserWantsMinimapVisible);

        ApplyOpacityFromPrefs();

        var show = _gameplayHudVisible && UserWantsMinimapVisible;
        _root.SetActive(show);
        if (!show)
            return;

        if (Time.unscaledTime < _nextRefreshUnscaled)
            return;
        _nextRefreshUnscaled = Time.unscaledTime + _refreshIntervalUnscaled;

        if (_playerTransform == null || _mapAreaRt == null)
            ResolveReferences();

        if (_playerTransform == null || _mapAreaRt == null)
            return;

        if (!_boundsReady)
            TryInitBounds();

        var innerHalf = 0.46f * Mathf.Min(_mapAreaRt.rect.width, _mapAreaRt.rect.height);
        if (innerHalf < 4f)
            return;

        var ppos = _playerTransform.position;
        UpdatePlayerMarker(ppos, innerHalf);
        UpdateEnemyBlips(ppos, innerHalf);
    }

    void ResolveReferences()
    {
        if (_playerGo == null)
            _playerGo = GameObject.FindGameObjectWithTag("Player1");
        _playerTransform = _playerGo != null ? _playerGo.transform : null;

        if (_generator == null)
        {
            var spawnGo = GameObject.FindGameObjectWithTag("Spawn");
            if (spawnGo != null)
                _generator = spawnGo.GetComponent<EnemyGenerator>();
        }
    }

    void TryInitBounds()
    {
        _boundsReady = false;
        if (_generator != null && _generator.TryGetArenaBoundsXZ(out var c, out var h, 24f, 100f))
        {
            _worldCenter = c;
            _halfExtents.x = Mathf.Max(h.x, 40f);
            _halfExtents.y = Mathf.Max(h.y, 40f);
            _boundsReady = true;
            return;
        }

        if (_playerTransform != null)
        {
            var p = _playerTransform.position;
            _worldCenter = new Vector3(p.x, 0f, p.z);
            _halfExtents = new Vector2(180f, 180f);
            _boundsReady = true;
        }
    }

    void ApplyOpacityFromPrefs()
    {
        if (_canvasGroup == null)
            return;
        var pct = MinimapOpacityPercent / 100f;
        _canvasGroup.alpha = Mathf.Clamp01(pct);
    }

    void UpdatePlayerMarker(Vector3 worldPos, float innerHalf)
    {
        var local = WorldToRadarLocal(worldPos, innerHalf);
        _playerDotRt.anchoredPosition = local;
        _playerDotRt.localEulerAngles = Vector3.zero;

        var fwd = _playerTransform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();
        var yawDeg = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        if (_playerNeedleRt != null)
            _playerNeedleRt.localEulerAngles = new Vector3(0f, 0f, -yawDeg);
    }

    void UpdateEnemyBlips(Vector3 playerWorld, float innerHalf)
    {
        _scratch.Clear();
        if (_generator != null && _generator.targets != null)
        {
            for (var i = 0; i < _generator.targets.Count; i++)
            {
                var t = _generator.targets[i];
                if (t == null || !t.gameObject.activeInHierarchy)
                    continue;
                if (t.GetComponentInChildren<EnemyCharacter>(true) == null)
                    continue;
                _scratch.Add(t);
            }
        }

        if (_scratch.Count > _blips.Length)
        {
            _distComparer.Reference = playerWorld;
            _scratch.Sort(_distComparer);
        }

        var n = Mathf.Min(_scratch.Count, _blips.Length);
        for (var i = 0; i < n; i++)
        {
            var tr = _scratch[i];
            var img = _blips[i];
            var ec = tr.GetComponent<EnemyCharacter>();
            var boss = ec != null && ec.isBoss;
            img.color = boss ? new Color(0.95f, 0.35f, 1f, 0.98f) : new Color(1f, 0.42f, 0.18f, 0.95f);
            img.gameObject.SetActive(true);
            img.rectTransform.anchoredPosition = WorldToRadarLocal(tr.position, innerHalf);
        }
        for (var i = n; i < _blips.Length; i++)
            _blips[i].gameObject.SetActive(false);
    }

    Vector2 WorldToRadarLocal(Vector3 world, float innerHalf)
    {
        var nx = (world.x - _worldCenter.x) / _halfExtents.x;
        var nz = (world.z - _worldCenter.z) / _halfExtents.y;
        nx = Mathf.Clamp(nx, -1f, 1f);
        nz = Mathf.Clamp(nz, -1f, 1f);
        return new Vector2(nx, nz) * innerHalf;
    }

    static bool WasMinimapTogglePressed()
    {
#if UNITY_WEBGL && !UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.Input.GetKeyDown(KeyCode.M);
#else
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            return true;
#if ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.Input.GetKeyDown(KeyCode.M);
#else
        return false;
#endif
#endif
    }

    static GameObject CreateChild(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchFullWithMargin(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        var m = new Vector2(margin, margin);
        rt.offsetMin = m;
        rt.offsetMax = -m;
    }
}
