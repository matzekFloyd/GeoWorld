using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI gameplay HUD (bars, skill columns, crosshair, vignettes). Built at runtime as a child of the
/// object that hosts <see cref="UserInterface"/> unless you wire references manually in the Inspector.
/// Fullscreen skill feedback renders above the low-health vignette.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayHudView : MonoBehaviour
{
    public static GameplayHudView Instance { get; private set; }

    static readonly int[] SkillMinLevels = { 1, 1, 1, 2, 4, 6, 8, 10 };

    Canvas _canvas;
    Text _classTitleText;
    Text _levelText;
    Image _healthFill;
    Text _healthValueText;
    Image _manaFill;
    Text _manaValueText;
    Image _expFill;
    Text _expValueText;
    Image _crosshair;
    Image _bloodVignette;
    Image _fxHeal;
    Image _fxBloodA;
    Image _fxBloodB;
    Image _fxFreeze;

    bool _fxHealOn;
    Texture2D _fxHealTex;
    bool _fxBloodAOn;
    Texture2D _fxBloodATex;
    bool _fxBloodBOn;
    Texture2D _fxBloodBTex;
    bool _fxFreezeOn;
    Texture2D _fxFreezeTex;

    Image _hitTakenCenterPulse;
    Image[] _hitTakenEdges;
    Coroutine _hitTakenRoutine;

    RectTransform _damageNumbersHost;

    static readonly Dictionary<Texture2D, Sprite> SpriteCache = new Dictionary<Texture2D, Sprite>();
    static Sprite _solidWhiteSprite;

    SkillColumn[] _columns;
    RectTransform _skillsRowRt;
    Transform _skillRowRoot;
    Transform _skillsHiddenBucket;
    int _lastSkillVisibleCount = -1;
    int _skillStripLayoutMask = -1;
    bool _built;

    int _lastBloodTier = -1;
    int _lastLevel = int.MinValue;
    string _lastHealthTxt;
    string _lastManaTxt;
    string _lastExpTxt;
    float _lastHealthFill = -1f;
    float _lastManaFill = -1f;
    float _lastExpFill = -1f;
    readonly string[] _lastSkillMana = new string[8];
    readonly string[] _lastSkillDmg = new string[8];
    readonly string[] _lastSkillHeal = new string[8];
    readonly string[] _lastSkillKey = new string[8];
    readonly bool[] _lastSkillColVisible = new bool[8];

    struct SkillColumn
    {
        public GameObject Root;
        public Text KeyLabel;
        public Image Icon;
        public Image CdOverlay;
        public Image Frame;
        public Text Mana;
        public Text Damage;
        public Text Heal;
        public Text CdSecondsOnIcon;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Creates default uGUI under this GameObject if not already built.</summary>
    public void EnsureBuilt(UserInterface source)
    {
        if (_built && _healthFill != null)
            return;

        var root = new GameObject("GameplayHUD", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;
        // Snap child RectTransforms to screen pixels — reduces fuzzy uGUI text vs. sub-pixel layout.
        _canvas.pixelPerfect = true;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Reference just above 1920×1080 → ~95% scale on 1080p (tweak here for overall HUD size).
        scaler.referenceResolution = new Vector2(2020f, 1136f);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        root.AddComponent<GraphicRaycaster>();

        var canvasRt = root.transform as RectTransform;
        StretchFull(canvasRt);
        float barW = 465f;

        BuildTopLeftPanel(canvasRt, barW, out _classTitleText, out _levelText,
            out _healthFill, out _healthValueText, out _manaFill, out _manaValueText,
            out _expFill, out _expValueText);

        _columns = BuildSkillColumns(canvasRt, source, barW);

        _crosshair = CreateCrosshair(canvasRt);
        _bloodVignette = CreateFullscreenImage(canvasRt, "BloodVignette");
        _fxHeal = CreateFullscreenImage(canvasRt, "FxHeal");
        _fxBloodA = CreateFullscreenImage(canvasRt, "FxBloodA");
        _fxBloodB = CreateFullscreenImage(canvasRt, "FxBloodB");
        _fxFreeze = CreateFullscreenImage(canvasRt, "FxFreeze");

        BuildCombatHitOverlays(canvasRt);

        var minimap = GetComponent<MinimapRadar>();
        if (minimap != null)
            minimap.BuildUi(canvasRt);

        BuildDamageNumbersHost(canvasRt);

        _built = true;
        ClearStringCache();
    }

    void ClearStringCache()
    {
        _lastLevel = int.MinValue;
        _lastBloodTier = -1;
        _lastHealthTxt = _lastManaTxt = _lastExpTxt = null;
        _lastHealthFill = _lastManaFill = _lastExpFill = -1f;
        for (int i = 0; i < 8; i++)
        {
            _lastSkillMana[i] = null;
            _lastSkillDmg[i] = null;
            _lastSkillHeal[i] = null;
            _lastSkillKey[i] = null;
            _lastSkillColVisible[i] = false;
        }
    }

    public void SetHudVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    public void ApplyCrosshair(Sprite sprite)
    {
        if (_crosshair == null || sprite == null)
            return;
        _crosshair.sprite = sprite;
        _crosshair.SetNativeSize();
    }

    public void ApplyBarSprites(Sprite health, Sprite mana, Sprite exp)
    {
        if (_healthFill != null && health != null)
        {
            _healthFill.sprite = health;
            _healthFill.type = Image.Type.Filled;
            _healthFill.color = Color.white;
        }
        if (_manaFill != null && mana != null)
        {
            _manaFill.sprite = mana;
            _manaFill.type = Image.Type.Filled;
            _manaFill.color = Color.white;
        }
        if (_expFill != null && exp != null)
        {
            _expFill.sprite = exp;
            _expFill.type = Image.Type.Filled;
            _expFill.color = Color.white;
        }
    }

    /// <param name="tier">0 = none, 1 = 20–30% HP, 2 = 10–20%, 3 = 0–10%.</param>
    public void ApplyBloodVignette(int tier, Texture2D t1, Texture2D t2, Texture2D t3)
    {
        if (_bloodVignette == null)
            return;
        if (tier == _lastBloodTier)
            return;
        _lastBloodTier = tier;
        if (tier <= 0 || tier > 3)
        {
            _bloodVignette.gameObject.SetActive(false);
            return;
        }
        var tex = tier == 1 ? t1 : tier == 2 ? t2 : t3;
        if (tex == null)
        {
            _bloodVignette.gameObject.SetActive(false);
            return;
        }
        _bloodVignette.sprite = GetOrCreateSprite(tex);
        _bloodVignette.color = Color.white;
        _bloodVignette.gameObject.SetActive(true);
    }

    public void ConfigureHealFlash(bool on, Texture2D tex) => SetFullscreenFx(ref _fxHealOn, ref _fxHealTex, _fxHeal, on, tex);

    public void ConfigureBloodRitualFx(bool on, Texture2D a, Texture2D b)
    {
        SetFullscreenFx(ref _fxBloodAOn, ref _fxBloodATex, _fxBloodA, on, a);
        SetFullscreenFx(ref _fxBloodBOn, ref _fxBloodBTex, _fxBloodB, on && b != null, b);
    }

    public void ConfigureFreezeFx(bool on, Texture2D tex) => SetFullscreenFx(ref _fxFreezeOn, ref _fxFreezeTex, _fxFreeze, on, tex);

    /// <summary>Brief non-flashing edge + center tint when the player takes damage (accessibility-friendly caps).</summary>
    public void PlayHitTakenFeedback(bool hasDirection, float dirX, float dirY, float centerPeakAlpha, float edgePeakAlpha, float duration, bool reducedMotion)
    {
        if (!_built || _hitTakenCenterPulse == null)
            return;
        if (_hitTakenRoutine != null)
            StopCoroutine(_hitTakenRoutine);
        _hitTakenRoutine = StartCoroutine(HitTakenFeedbackRoutine(hasDirection, dirX, dirY, centerPeakAlpha, edgePeakAlpha, duration, reducedMotion));
    }

    IEnumerator HitTakenFeedbackRoutine(bool hasDirection, float dirX, float dirY, float centerPeak, float edgePeak, float duration, bool reducedMotion)
    {
        int edge = -1;
        if (hasDirection && _hitTakenEdges != null && _hitTakenEdges.Length == 4)
        {
            if (Mathf.Abs(dirX) >= Mathf.Abs(dirY))
                edge = dirX < 0f ? 0 : 1;
            else
                edge = dirY < 0f ? 2 : 3;
        }

        const float r = 1f;
        const float g = 0.1f;
        const float b = 0.06f;

        _hitTakenCenterPulse.gameObject.SetActive(true);
        Image edgeImg = null;
        if (edge >= 0 && edge < _hitTakenEdges.Length)
        {
            edgeImg = _hitTakenEdges[edge];
            edgeImg.gameObject.SetActive(true);
            var ec = edgeImg.color;
            edgeImg.color = new Color(ec.r, ec.g, ec.b, 0f);
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            float centerA = centerPeak * u * (reducedMotion ? 0.85f : 1f);
            _hitTakenCenterPulse.color = new Color(r, g, b, Mathf.Clamp01(centerA));

            if (edgeImg != null)
            {
                float ea = edgePeak * u * (reducedMotion ? 0.8f : 1f);
                var ec = edgeImg.color;
                edgeImg.color = new Color(ec.r, ec.g, ec.b, Mathf.Clamp01(ea));
            }
            yield return null;
        }

        _hitTakenCenterPulse.color = new Color(r, g, b, 0f);
        _hitTakenCenterPulse.gameObject.SetActive(false);
        if (edgeImg != null)
        {
            var ec = edgeImg.color;
            edgeImg.color = new Color(ec.r, ec.g, ec.b, 0f);
            edgeImg.gameObject.SetActive(false);
        }
        _hitTakenRoutine = null;
    }

    void BuildCombatHitOverlays(RectTransform canvasRt)
    {
        _hitTakenCenterPulse = CreateFullscreenImage(canvasRt, "HitTakenCenterPulse");
        _hitTakenCenterPulse.sprite = GetSolidWhiteSprite();
        _hitTakenCenterPulse.color = new Color(1f, 0.1f, 0.06f, 0f);
        _hitTakenCenterPulse.gameObject.SetActive(false);

        _hitTakenEdges = new Image[4];
        _hitTakenEdges[0] = CreateEdgeTint(canvasRt, "HitEdgeL", new Vector2(0f, 0f), new Vector2(0.14f, 1f));
        _hitTakenEdges[1] = CreateEdgeTint(canvasRt, "HitEdgeR", new Vector2(0.86f, 0f), new Vector2(1f, 1f));
        _hitTakenEdges[2] = CreateEdgeTint(canvasRt, "HitEdgeB", new Vector2(0f, 0f), new Vector2(1f, 0.12f));
        _hitTakenEdges[3] = CreateEdgeTint(canvasRt, "HitEdgeT", new Vector2(0f, 0.88f), new Vector2(1f, 1f));

        for (int i = 0; i < _hitTakenEdges.Length; i++)
            _hitTakenEdges[i].transform.SetAsLastSibling();
        _hitTakenCenterPulse.transform.SetAsLastSibling();
    }

    static Image CreateEdgeTint(RectTransform canvas, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateUIObject(name, canvas);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = GetSolidWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(1f, 0.06f, 0.02f, 0f);
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    static Sprite GetSolidWhiteSprite()
    {
        if (_solidWhiteSprite == null)
        {
            var t = Texture2D.whiteTexture;
            _solidWhiteSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return _solidWhiteSprite;
    }

    void BuildDamageNumbersHost(RectTransform canvasRt)
    {
        var go = new GameObject("DamageNumbersHost", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        go.transform.SetParent(canvasRt, false);
        StretchFull(rt);
        _damageNumbersHost = rt;
        go.transform.SetAsLastSibling();
    }

    /// <summary>Full-screen overlay parent for pooled world-anchored damage text (above most HUD widgets).</summary>
    public RectTransform DamageNumbersHost => _damageNumbersHost;

    /// <summary>Same font as HUD stat labels for floating combat numbers.</summary>
    public static Font HudUiFont => UiFont;

    /// <summary>1×1 white sprite for solid uGUI fills (minimap blips, etc.).</summary>
    public static Sprite HudSolidSprite => GetSolidWhiteSprite();

    public void RefreshGameplay(
        bool showGameplay,
        int curLevel,
        int maxLevel,
        float curHealth,
        float maxHealth,
        float curMana,
        float maxMana,
        float curExp,
        float expNeeded,
        Texture2D blood1,
        Texture2D blood2,
        Texture2D blood3,
        string[] keyLabels,
        Sprite[] icons,
        Sprite frameSprite,
        string[] mana, string[] dmg, string[] heal, string[] cdMax, string[] cdCur)
    {
        if (_canvas == null || !_built)
            return;

        if (!showGameplay)
        {
            SetHudVisible(false);
            _lastBloodTier = -1;
            return;
        }
        SetHudVisible(true);

        if (_lastLevel != curLevel)
        {
            _lastLevel = curLevel;
            if (_classTitleText != null)
                _classTitleText.text = "GeoMancer";
            if (_levelText != null)
                _levelText.text = "Level " + curLevel;
        }

        string hpTxt = (int)curHealth + "/" + (int)maxHealth;
        if (hpTxt != _lastHealthTxt && _healthValueText != null)
        {
            _lastHealthTxt = hpTxt;
            _healthValueText.text = hpTxt;
        }
        float hFill = maxHealth > 0 ? Mathf.Clamp01(curHealth / maxHealth) : 0f;
        if (Mathf.Abs(hFill - _lastHealthFill) > 0.0005f && _healthFill != null)
        {
            _lastHealthFill = hFill;
            _healthFill.fillAmount = hFill;
        }

        string mnTxt = (int)curMana + "/" + (int)maxMana;
        if (mnTxt != _lastManaTxt && _manaValueText != null)
        {
            _lastManaTxt = mnTxt;
            _manaValueText.text = mnTxt;
        }
        float mFill = maxMana > 0 ? Mathf.Clamp01(curMana / maxMana) : 0f;
        if (Mathf.Abs(mFill - _lastManaFill) > 0.0005f && _manaFill != null)
        {
            _lastManaFill = mFill;
            _manaFill.fillAmount = mFill;
        }

        if (curLevel < maxLevel)
        {
            string xpTxt = (int)curExp + "/" + (int)expNeeded;
            if (xpTxt != _lastExpTxt && _expValueText != null)
            {
                _lastExpTxt = xpTxt;
                _expValueText.text = xpTxt;
            }
            float eFill = expNeeded > 0 ? Mathf.Clamp01(curExp / expNeeded) : 0f;
            if (Mathf.Abs(eFill - _lastExpFill) > 0.0005f && _expFill != null)
            {
                _lastExpFill = eFill;
                _expFill.fillAmount = eFill;
            }
        }
        else
        {
            if (_lastExpTxt != "MAX LEVEL REACHED" && _expValueText != null)
            {
                _lastExpTxt = "MAX LEVEL REACHED";
                _expValueText.text = "MAX LEVEL REACHED";
            }
            if (Mathf.Abs(1f - _lastExpFill) > 0.0005f && _expFill != null)
            {
                _lastExpFill = 1f;
                _expFill.fillAmount = 1f;
            }
        }

        int bTier = 0;
        if (maxHealth > 0)
        {
            if (curHealth <= maxHealth * 0.1f && curHealth >= 0) bTier = 3;
            else if (curHealth <= maxHealth * 0.2f) bTier = 2;
            else if (curHealth <= maxHealth * 0.3f) bTier = 1;
        }
        ApplyBloodVignette(bTier, blood1, blood2, blood3);

        LayoutSkillStrip(curLevel);

        for (int i = 0; i < 8 && _columns != null; i++)
        {
            if (curLevel < SkillMinLevels[i])
                continue;

            string k = keyLabels != null && i < keyLabels.Length ? keyLabels[i] ?? "" : "";
            SetIfChanged(_columns[i].KeyLabel, k, _lastSkillKey, i);

            if (icons != null && i < icons.Length && _columns[i].Icon != null && icons[i] != null)
                _columns[i].Icon.sprite = icons[i];
            if (_columns[i].Frame != null && frameSprite != null)
                _columns[i].Frame.sprite = frameSprite;

            SetIfChanged(_columns[i].Mana, mana[i], _lastSkillMana, i);
            SetIfChanged(_columns[i].Damage, dmg[i], _lastSkillDmg, i);
            SetIfChanged(_columns[i].Heal, heal[i], _lastSkillHeal, i);

            ApplySkillCooldownVisual(_columns[i], cdCur[i], cdMax[i]);
        }
    }

    const float SkillColWidth = 58f;
    const float SkillColSpacing = 6f;
    const float SkillsTitleGapAboveRow = 12f;
    /// <summary>Horizontal padding inside the skill row (must match <see cref="HorizontalLayoutGroup.padding"/>).</summary>
    const float SkillsRowHorizontalPadding = 16f;
    const float SkillsRowHeight = 244f;

    void LayoutSkillStrip(int curLevel)
    {
        if (_columns == null || _skillRowRoot == null || _skillsHiddenBucket == null)
            return;

        int mask = 0;
        for (int i = 0; i < 8; i++)
        {
            if (curLevel >= SkillMinLevels[i])
                mask |= 1 << i;
        }

        if (mask == _skillStripLayoutMask)
            return;
        _skillStripLayoutMask = mask;

        int visibleCount = 0;
        for (int i = 0; i < 8; i++)
        {
            bool vis = curLevel >= SkillMinLevels[i];
            _lastSkillColVisible[i] = vis;
            var root = _columns[i].Root;
            if (root == null)
                continue;
            if (vis)
            {
                root.transform.SetParent(_skillRowRoot, false);
                root.SetActive(true);
                visibleCount++;
            }
            else
            {
                root.transform.SetParent(_skillsHiddenBucket, false);
                root.SetActive(false);
            }
        }

        int order = 0;
        for (int i = 0; i < 8; i++)
        {
            if (curLevel < SkillMinLevels[i])
                continue;
            if (_columns[i].Root != null)
                _columns[i].Root.transform.SetSiblingIndex(order++);
        }

        _lastSkillVisibleCount = visibleCount;
        UpdateSkillsRowWidth(visibleCount);
    }

    void UpdateSkillsRowWidth(int visibleCount)
    {
        if (_skillsRowRt == null)
            return;
        float w = visibleCount > 0
            ? SkillsRowHorizontalPadding + visibleCount * SkillColWidth + Mathf.Max(0, visibleCount - 1) * SkillColSpacing
            : 72f;
        _skillsRowRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        _skillsRowRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SkillsRowHeight);
    }

    static void ApplySkillCooldownVisual(SkillColumn col, string cdCurStr, string cdMaxStr)
    {
        if (col.Icon == null)
            return;

        bool hasCd = TryParseCooldown(cdCurStr, cdMaxStr, out float curCd, out float maxCd);
        bool onCd = hasCd && curCd > 0.02f;

        col.Icon.color = onCd ? new Color(0.7f, 0.72f, 0.76f, 1f) : Color.white;

        if (col.CdOverlay != null)
        {
            if (!onCd)
            {
                col.CdOverlay.fillAmount = 0f;
                col.CdOverlay.gameObject.SetActive(false);
            }
            else
            {
                col.CdOverlay.gameObject.SetActive(true);
                col.CdOverlay.fillAmount = Mathf.Clamp01(curCd / maxCd);
            }
        }

        if (col.CdSecondsOnIcon != null)
        {
            if (!onCd)
            {
                col.CdSecondsOnIcon.text = "";
                col.CdSecondsOnIcon.gameObject.SetActive(false);
            }
            else
            {
                col.CdSecondsOnIcon.gameObject.SetActive(true);
                col.CdSecondsOnIcon.text = FormatCooldownSecondsRemaining(curCd);
            }
        }
    }

    static string FormatCooldownSecondsRemaining(float secondsRemaining)
    {
        if (secondsRemaining <= 0.02f)
            return "";
        if (secondsRemaining >= 99.5f)
            return "99";
        if (secondsRemaining >= 10f)
            return Mathf.CeilToInt(secondsRemaining).ToString(CultureInfo.InvariantCulture);
        return secondsRemaining.ToString("0.0", CultureInfo.InvariantCulture);
    }

    static bool TryParseCooldown(string curStr, string maxStr, out float cur, out float max)
    {
        cur = 0f;
        max = 0f;
        if (string.IsNullOrWhiteSpace(maxStr))
            return false;
        maxStr = maxStr.Trim().Replace(',', '.');
        curStr = string.IsNullOrWhiteSpace(curStr) ? "0" : curStr.Trim().Replace(',', '.');
        if (!float.TryParse(maxStr, NumberStyles.Float, CultureInfo.InvariantCulture, out max))
            return false;
        if (max <= 0f)
            return false;
        float.TryParse(curStr, NumberStyles.Float, CultureInfo.InvariantCulture, out cur);
        cur = Mathf.Max(0f, cur);
        return true;
    }

    static void SetIfChanged(Text t, string value, string[] cache, int i)
    {
        if (t == null)
            return;
        value ??= "";
        if (cache[i] == value)
            return;
        cache[i] = value;
        t.text = value;
    }

    static void SetFullscreenFx(ref bool lastOn, ref Texture2D lastTex, Image img, bool on, Texture2D tex)
    {
        if (img == null)
            return;
        if (!on || tex == null)
        {
            if (lastOn)
                img.gameObject.SetActive(false);
            lastOn = false;
            lastTex = null;
            return;
        }
        if (!lastOn || lastTex != tex)
        {
            lastTex = tex;
            img.sprite = GetOrCreateSprite(tex);
            img.color = Color.white;
            img.gameObject.SetActive(true);
        }
        lastOn = true;
    }

    static void BuildTopLeftPanel(RectTransform canvas, float barWidth,
        out Text classText, out Text levelText,
        out Image healthFill, out Text healthVal,
        out Image manaFill, out Text manaVal,
        out Image expFill, out Text expVal)
    {
        var panel = CreateUIObject("TopLeft", canvas);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(14f, -14f);
        rt.sizeDelta = new Vector2(barWidth + 132f, 216f);

        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = 6f;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        var header = CreateUIObject("Header", panel.transform);
        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8f;
        classText = CreateText(header.transform, "GeoMancer", 24, TextAnchor.MiddleLeft, FontStyle.Bold);
        var leC = classText.gameObject.AddComponent<LayoutElement>();
        leC.preferredWidth = 142f;
        levelText = CreateText(header.transform, "Level 1", 24, TextAnchor.MiddleLeft, FontStyle.Normal);
        var leL = levelText.gameObject.AddComponent<LayoutElement>();
        leL.flexibleWidth = 1f;

        healthFill = CreateBarRow(panel.transform, "Health", "Health:", barWidth, new Color(0.85f, 0.2f, 0.2f), out healthVal);
        manaFill = CreateBarRow(panel.transform, "Mana", "Mana:", barWidth, new Color(0.25f, 0.45f, 0.95f), out manaVal);
        expFill = CreateBarRow(panel.transform, "Experience", "Experience:", barWidth, new Color(0.35f, 0.8f, 0.35f), out expVal);
    }

    static Image CreateBarRow(Transform parent, string name, string label, float barWidth, Color fillColor, out Text valueText)
    {
        var row = CreateUIObject(name + "Row", parent);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = 8f;

        var lbl = CreateText(row.transform, label, 20, TextAnchor.MiddleLeft, FontStyle.Normal);
        var leLbl = lbl.gameObject.AddComponent<LayoutElement>();
        leLbl.preferredWidth = 118f;

        var barHost = CreateUIObject("BarHost", row.transform);
        var leBar = barHost.AddComponent<LayoutElement>();
        leBar.preferredWidth = barWidth;
        leBar.preferredHeight = 22f;

        var bg = CreateUIObject("Bg", barHost.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        StretchFull(bgRt);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);

        var fillGo = CreateUIObject("Fill", barHost.transform);
        var fillRt = fillGo.GetComponent<RectTransform>();
        StretchFull(fillRt);
        var fill = fillGo.AddComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = fillColor;
        fill.fillAmount = 1f;

        valueText = CreateText(barHost.transform, "", 18, TextAnchor.MiddleCenter, FontStyle.Bold);
        var vtRt = valueText.GetComponent<RectTransform>();
        StretchFull(vtRt);
        valueText.color = Color.white;

        return fill;
    }

    SkillColumn[] BuildSkillColumns(RectTransform canvas, UserInterface source, float barWidth)
    {
        _ = barWidth;
        float bottomInset = 16f;
        var minimap = GetComponent<MinimapRadar>();
        if (minimap != null)
            bottomInset = minimap.CornerInsetFromBottomLeft;

        var titleGo = CreateUIObject("SkillsTitle", canvas);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0f);
        titleRt.pivot = new Vector2(0.5f, 0f);
        titleRt.anchoredPosition = new Vector2(0f, bottomInset + SkillsRowHeight + SkillsTitleGapAboveRow);
        titleRt.sizeDelta = new Vector2(420f, 28f);
        var title = CreateText(titleGo.transform, "Skills", 22, TextAnchor.MiddleCenter, FontStyle.Bold);
        StretchFull(title.GetComponent<RectTransform>());

        var hidden = CreateUIObject("SkillsHiddenBucket", canvas);
        var hiddenRt = hidden.GetComponent<RectTransform>();
        hiddenRt.anchorMin = hiddenRt.anchorMax = new Vector2(0.5f, 0.5f);
        hiddenRt.pivot = new Vector2(0.5f, 0.5f);
        hiddenRt.anchoredPosition = new Vector2(5000f, 5000f);
        hiddenRt.sizeDelta = new Vector2(1f, 1f);
        _skillsHiddenBucket = hidden.transform;

        var row = CreateUIObject("SkillColumns", canvas);
        _skillsRowRt = row.GetComponent<RectTransform>();
        _skillRowRoot = row.transform;
        _skillsRowRt.anchorMin = _skillsRowRt.anchorMax = new Vector2(0.5f, 0f);
        _skillsRowRt.pivot = new Vector2(0.5f, 0f);
        _skillsRowRt.anchoredPosition = new Vector2(0f, bottomInset);
        UpdateSkillsRowWidth(8);

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = SkillColSpacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childForceExpandWidth = false;
        h.childControlWidth = true;
        h.childControlHeight = true;
        var pad = Mathf.RoundToInt(0.5f * SkillsRowHorizontalPadding);
        h.padding = new RectOffset(pad, pad, 4, 8);

        Texture2D[] tex =
        {
            source.singleShotTexture, source.sprayShotTexture, source.geoPhysicsTexture, source.healTexture,
            source.fireBallTexture, source.bloodRitualTexture, source.freezeTimeTexture, source.geoManiaTexture
        };
        string[] keys = { "M1", "M2", "", "Q", "E", "R", "F", "" };

        var columns = new SkillColumn[8];
        for (int i = 0; i < 8; i++)
            columns[i] = CreateSkillColumn(row.transform, "Skill" + i, SkillColWidth, keys[i], tex[i], source.frameTexture, source.backgroundTexture);

        var initialLv = 1;
        var pgo = GameObject.FindGameObjectWithTag("Player1");
        if (pgo != null)
        {
            var pc = pgo.GetComponent<PlayerCharacter>();
            if (pc != null)
                initialLv = Mathf.RoundToInt(pc.getCurLevel());
        }
        _columns = columns;
        LayoutSkillStrip(initialLv);

        return columns;
    }

    static SkillColumn CreateSkillColumn(Transform parent, string name, float colW, string key, Texture2D iconTex, Texture2D frameTex, Texture2D bgTex)
    {
        var col = CreateUIObject(name, parent);
        var le = col.AddComponent<LayoutElement>();
        le.preferredWidth = colW;
        le.minWidth = colW;
        var v = col.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperCenter;
        v.spacing = 2f;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var keyT = CreateText(col.transform, key, 15, TextAnchor.MiddleCenter, FontStyle.Bold);
        var leK = keyT.gameObject.AddComponent<LayoutElement>();
        leK.preferredHeight = 18f;

        var iconHost = CreateUIObject("IconHost", col.transform);
        var leIh = iconHost.AddComponent<LayoutElement>();
        leIh.preferredHeight = 50f;

        var bg = CreateUIObject("SkillBg", iconHost.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        var bgImg = bg.AddComponent<Image>();
        if (bgTex != null)
            bgImg.sprite = GetOrCreateSprite(bgTex);
        else
            bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);

        var icon = CreateUIObject("Icon", iconHost.transform);
        StretchFull(icon.GetComponent<RectTransform>());
        var iconImg = icon.AddComponent<Image>();
        if (iconTex != null)
            iconImg.sprite = GetOrCreateSprite(iconTex);
        iconImg.preserveAspect = true;

        var frame = CreateUIObject("Frame", iconHost.transform);
        StretchFull(frame.GetComponent<RectTransform>());
        var fr = frame.AddComponent<Image>();
        if (frameTex != null)
            fr.sprite = GetOrCreateSprite(frameTex);
        fr.raycastTarget = false;

        var cdGo = CreateUIObject("CooldownSweep", iconHost.transform);
        StretchFull(cdGo.GetComponent<RectTransform>());
        var cdImg = cdGo.AddComponent<Image>();
        cdImg.raycastTarget = false;
        cdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillOrigin = (int)Image.Origin360.Top;
        cdImg.fillClockwise = true;
        cdImg.fillAmount = 0f;
        cdGo.SetActive(false);

        var cdSec = CreateCooldownSecondsOnIcon(iconHost.transform);

        Text Mana = CreateStatText(col.transform);
        Text Damage = CreateStatText(col.transform);
        Text Heal = CreateStatText(col.transform);

        return new SkillColumn
        {
            Root = col,
            KeyLabel = keyT,
            Icon = iconImg,
            CdOverlay = cdImg,
            Frame = fr,
            Mana = Mana,
            Damage = Damage,
            Heal = Heal,
            CdSecondsOnIcon = cdSec
        };
    }

    static Text CreateCooldownSecondsOnIcon(Transform iconHost)
    {
        var go = CreateUIObject("CooldownSeconds", iconHost);
        StretchFull(go.GetComponent<RectTransform>());
        var t = go.AddComponent<Text>();
        t.font = UiFont;
        t.text = "";
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.alignByGeometry = true;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2(1.25f, -1.25f);
        outline.useGraphicAlpha = true;
        go.SetActive(false);
        return t;
    }

    static Text CreateStatText(Transform parent)
    {
        var t = CreateText(parent, "", 14, TextAnchor.MiddleCenter, FontStyle.Normal);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 17f;
        return t;
    }

    static Image CreateCrosshair(RectTransform canvas)
    {
        var go = CreateUIObject("Crosshair", canvas);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    static Image CreateFullscreenImage(RectTransform canvas, string name)
    {
        var go = CreateUIObject(name, canvas);
        var rt = go.GetComponent<RectTransform>();
        StretchFull(rt);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.gameObject.SetActive(false);
        return img;
    }

    static GameObject CreateUIObject(string name, Transform parent)
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

    static Text CreateText(Transform parent, string text, int size, TextAnchor align, FontStyle style)
    {
        var go = CreateUIObject("Text", parent);
        var t = go.AddComponent<Text>();
        t.font = UiFont;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.fontStyle = style;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        // Crisper glyph placement vs. bounding-box alignment (Unity 5.2+).
        t.alignByGeometry = true;
        // Light outline reads closer to IMGUI default skin contrast on busy backgrounds.
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(0.5f, -0.5f);
        outline.useGraphicAlpha = true;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, Mathf.Max(22f, size + 6f));
        return t;
    }

    static Font UiFont
    {
        get
        {
            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_uiFont == null)
                    _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _uiFont;
        }
    }

    static Font _uiFont;

    public static Sprite SpriteFromTexture(Texture2D t) => GetOrCreateSprite(t);

    static Sprite GetOrCreateSprite(Texture2D t)
    {
        if (t == null)
            return null;
        if (SpriteCache.TryGetValue(t, out var existing))
            return existing;
        var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        SpriteCache[t] = s;
        return s;
    }
}
