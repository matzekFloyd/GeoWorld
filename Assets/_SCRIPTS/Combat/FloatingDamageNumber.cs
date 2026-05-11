using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One pooled uGUI damage pop: world-anchored drift, fade, then returns to <see cref="FloatingDamageNumberPool"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloatingDamageNumber : MonoBehaviour
{
    FloatingDamageNumberPool _pool;
    Text _text;
    Outline _outline;
    RectTransform _rt;
    Coroutine _routine;
    bool _returned;

    public void Initialize(FloatingDamageNumberPool pool, Text text, Outline outline, RectTransform rt)
    {
        _pool = pool;
        _text = text;
        _outline = outline;
        _rt = rt;
    }

    public void Show(RectTransform hostRt, Vector3 worldStart, string value, Color color, float duration, float worldRisePerSec, float fontSize)
    {
        if (_routine != null)
        {
            if (isActiveAndEnabled)
                StopCoroutine(_routine);
            _routine = null;
        }
        _returned = false;
        transform.SetParent(hostRt, false);
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        _routine = StartCoroutine(Run(hostRt, worldStart, value, color, duration, worldRisePerSec, fontSize));
    }

    IEnumerator Run(RectTransform hostRt, Vector3 worldStart, string value, Color color, float duration, float worldRisePerSec, float fontSize)
    {
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.localScale = Vector3.one * 0.72f;

        _text.text = value;
        _text.fontSize = Mathf.RoundToInt(fontSize);
        _text.color = color;
        _text.canvasRenderer.SetAlpha(color.a);

        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null || hostRt == null)
        {
            ReturnToPool();
            yield break;
        }

        Vector3 world = worldStart;
        var startCol = color;
        Vector2 jitter = Random.insideUnitCircle * 12f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (cam == null)
                cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null || hostRt == null)
                break;

            world += Vector3.up * (worldRisePerSec * Time.deltaTime);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRt, screen, null, out var local))
                _rt.anchoredPosition = local + jitter;

            float u = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(startCol.a, 0f, u * u);
            var c = _text.color;
            _text.color = new Color(c.r, c.g, c.b, a);
            if (_outline != null)
            {
                var oc = _outline.effectColor;
                _outline.effectColor = new Color(oc.r, oc.g, oc.b, Mathf.Clamp01(a * 0.92f));
            }

            float popT = Mathf.Clamp01(t / 0.1f);
            _rt.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, popT * popT);

            yield return null;
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (_returned)
            return;
        _returned = true;
        _routine = null;
        _pool?.Release(this);
    }
}
