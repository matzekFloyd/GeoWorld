using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mid-round pause via the same <see cref="GameInput.PauseOrQuitUp"/> binding as end-of-run quit (Escape release).
/// Freezes <see cref="Time.timeScale"/> while active; dismisses automatically when <see cref="GameSession.IsRunActive"/> becomes false.
/// Does not run during the Game Over / win screen — <see cref="GameOver"/> still owns quit there.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayPause : MonoBehaviour
{
    bool _paused;
    float _savedTimeScale = 1f;
    GameObject _overlayRoot;
    Text _subtitleText;
    static Sprite s_blockerSprite;
    GameOver _gameOver;

    void Awake()
    {
        _gameOver = GetComponent<GameOver>();
    }

    void Start()
    {
        if (_gameOver == null)
        {
#if UNITY_2023_1_OR_NEWER
            _gameOver = Object.FindAnyObjectByType<GameOver>();
#else
            _gameOver = FindObjectOfType<GameOver>();
#endif
        }
    }

    /// <summary>Call every frame before early-outs so run-end still clears a stuck pause overlay.</summary>
    public void SyncIfRunInactive()
    {
        if (!_paused)
            return;
        var s = GameSession.Instance;
        bool stillMidRun = s != null && s.IsRunActive && !RoundHasEndedOnController();
        if (stillMidRun)
            return;

        _paused = false;
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
    }

    /// <summary>Toggle pause when Escape (PauseOrQuit) is released and the run is still active.</summary>
    public void TryToggleRunPause()
    {
        if (!GameInput.PauseOrQuitUp)
            return;
        var s = GameSession.Instance;
        if (s == null || !s.IsRunActive)
            return;
        if (RoundHasEndedOnController())
            return;
        var pc = s.Player;
        if (pc != null && pc.iAmDead())
            return;

        if (_paused)
            Resume();
        else
            Pause();
    }

    bool RoundHasEndedOnController()
    {
        return _gameOver != null && (_gameOver.playerDied || _gameOver.gameTimeIsOver);
    }

    void Pause()
    {
        float ts = Time.timeScale;
        _savedTimeScale = ts > 0.05f ? ts : 1f;
        _paused = true;
        Time.timeScale = 0f;
        EnsureOverlay();
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
            RefreshSubtitle();
        }
    }

    void Resume()
    {
        _paused = false;
        Time.timeScale = _savedTimeScale > 0.05f ? _savedTimeScale : 1f;
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
    }

    void RefreshSubtitle()
    {
        if (_subtitleText == null)
            return;
#if UNITY_WEBGL && !UNITY_EDITOR
        _subtitleText.text = "Press Escape to resume (close the tab from the browser UI to leave).";
#else
        _subtitleText.text = "Press Escape to resume";
#endif
    }

    void EnsureOverlay()
    {
        if (_overlayRoot != null)
            return;

        var hud = GameplayHudView.Instance;
        var parent = hud != null ? hud.HudCanvasRect : null;
        if (parent == null)
            return;

        _overlayRoot = new GameObject("PauseOverlay", typeof(RectTransform));
        var rootRt = (RectTransform)_overlayRoot.transform;
        rootRt.SetParent(parent, false);
        StretchFullScreen(rootRt);
        rootRt.SetAsLastSibling();

        var dimGo = new GameObject("PauseDim", typeof(RectTransform));
        var dimRt = (RectTransform)dimGo.transform;
        dimRt.SetParent(rootRt, false);
        StretchFullScreen(dimRt);
        var dim = dimGo.AddComponent<Image>();
        dim.sprite = BlockerSprite();
        dim.type = Image.Type.Simple;
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        var titleGo = new GameObject("PauseTitle", typeof(RectTransform));
        var titleRt = (RectTransform)titleGo.transform;
        titleRt.SetParent(rootRt, false);
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.56f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(1400f, 100f);
        titleRt.anchoredPosition = Vector2.zero;
        var title = titleGo.AddComponent<Text>();
        title.font = GameplayHudView.HudUiFont;
        title.text = "PAUSED";
        title.fontSize = 48;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 1f, 1f, 0.96f);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        title.raycastTarget = false;
        title.alignByGeometry = true;
        var titleOutline = titleGo.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var subGo = new GameObject("PauseSubtitle", typeof(RectTransform));
        var subRt = (RectTransform)subGo.transform;
        subRt.SetParent(rootRt, false);
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.44f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.sizeDelta = new Vector2(1400f, 80f);
        subRt.anchoredPosition = Vector2.zero;
        _subtitleText = subGo.AddComponent<Text>();
        _subtitleText.font = GameplayHudView.HudUiFont;
        _subtitleText.fontSize = 22;
        _subtitleText.alignment = TextAnchor.MiddleCenter;
        _subtitleText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        _subtitleText.raycastTarget = false;
        _subtitleText.alignByGeometry = true;
        RefreshSubtitle();

        _overlayRoot.SetActive(false);
    }

    static void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static Sprite BlockerSprite()
    {
        if (s_blockerSprite != null)
            return s_blockerSprite;
        var tex = Texture2D.whiteTexture;
        s_blockerSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return s_blockerSprite;
    }

    void OnDestroy()
    {
        if (_paused)
        {
            _paused = false;
            if (Time.timeScale < 0.05f)
                Time.timeScale = _savedTimeScale > 0.05f ? _savedTimeScale : 1f;
        }
    }
}
