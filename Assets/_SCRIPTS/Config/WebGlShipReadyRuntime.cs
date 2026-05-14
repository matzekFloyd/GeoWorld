using UnityEngine;

/// <summary>
/// WebGL-only helpers: tab visibility (pause audio) without scene wiring, and optional simulation pause
/// during an active run so the round timer and combat do not advance in the background.
/// See README → WebGL: ship-ready behavior and hosting.
/// </summary>
public sealed class WebGlShipReadyRuntime : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    bool _simPausedForTabBlur;
    float _timeScaleBeforeTabBlur = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject(nameof(WebGlShipReadyRuntime));
        DontDestroyOnLoad(go);
        go.AddComponent<WebGlShipReadyRuntime>();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // pauseStatus true = hidden (tab background).
        ApplyTabVisibility(!pauseStatus);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        ApplyTabVisibility(hasFocus);
    }

    void ApplyTabVisibility(bool visible)
    {
        bool hidden = !visible;
        AudioListener.pause = hidden;
        if (hidden)
            TryPauseSimulationForHiddenTab();
        else
            TryResumeSimulationAfterTabFocus();
    }

    void OnDestroy()
    {
        if (_simPausedForTabBlur)
            TryResumeSimulationAfterTabFocus();
    }

    void TryPauseSimulationForHiddenTab()
    {
        if (_simPausedForTabBlur)
            return;

        var session = GameSession.Instance;
        if (session == null || !session.IsRunActive)
            return;

        _timeScaleBeforeTabBlur = Time.timeScale > 0.0001f ? Time.timeScale : 1f;
        _simPausedForTabBlur = true;
        Time.timeScale = 0f;
    }

    void TryResumeSimulationAfterTabFocus()
    {
        if (!_simPausedForTabBlur)
            return;

        _simPausedForTabBlur = false;

        var session = GameSession.Instance;
        if (session != null && session.IsRunActive)
            Time.timeScale = _timeScaleBeforeTabBlur > 0.0001f ? _timeScaleBeforeTabBlur : 1f;
        // If the run ended while hidden, leave Time.timeScale to GameOver (0) or whatever the scene set.
    }
#endif
}
