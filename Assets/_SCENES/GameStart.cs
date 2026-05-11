using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title screen for <c>Start.unity</c>: shows the game name and loads the gameplay scene after any key or mouse click.
/// Notifies <see cref="GeoWorldSessionStart"/> so WebGL background music can start after the same user gesture.
/// </summary>
public class GameStart : MonoBehaviour
{
    [SerializeField] string gameplaySceneName = "GeoWorldMain";
    [SerializeField] string titleCopy = "GeoWorld";
    [SerializeField] string promptCopy = "Press any key to start";
    [Tooltip("Skip input for the first N frames so stray events from loading do not skip the title.")]
    [SerializeField] int ignoreInputFrames = 4;

    int m_InputEnabledFrame;

    static Font s_UiFont;

    void Awake()
    {
        BuildTitleUi();
        m_InputEnabledFrame = Time.frameCount + Mathf.Max(0, ignoreInputFrames);
    }

    void Update()
    {
        if (Time.frameCount < m_InputEnabledFrame)
            return;

        if (UnityEngine.Input.anyKeyDown || UnityEngine.Input.GetMouseButtonDown(0) ||
            UnityEngine.Input.GetMouseButtonDown(1))
        {
            GeoWorldSessionStart.NotifyGameplayStartingFromTitleScreen();
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    void BuildTitleUi()
    {
        var root = new GameObject("TitleScreenCanvas", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.pixelPerfect = true;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2020f, 1136f);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        root.AddComponent<GraphicRaycaster>();

        var canvasRt = root.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(root.transform, false);
        var backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;
        var backdropImg = backdrop.GetComponent<Image>();
        backdropImg.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);
        backdropImg.raycastTarget = false;

        var title = CreateCenteredText(root.transform, titleCopy, 72, TextAnchor.MiddleCenter, FontStyle.Bold, 120f);
        var prompt = CreateCenteredText(root.transform, promptCopy, 28, TextAnchor.MiddleCenter, FontStyle.Normal, -80f);
        title.color = Color.white;
        prompt.color = new Color(0.9f, 0.9f, 0.92f, 1f);
    }

    static Text CreateCenteredText(Transform parent, string copy, int size, TextAnchor align, FontStyle style, float yOffset)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, 200f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        var t = go.GetComponent<Text>();
        t.font = UiFont;
        t.text = copy;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Font UiFont
    {
        get
        {
            if (s_UiFont == null)
            {
                s_UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (s_UiFont == null)
                    s_UiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return s_UiFont;
        }
    }
}
