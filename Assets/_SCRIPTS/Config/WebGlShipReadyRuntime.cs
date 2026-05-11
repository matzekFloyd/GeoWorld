using UnityEngine;

/// <summary>
/// WebGL-only helpers: tab visibility (pause audio) without scene wiring.
/// See README → WebGL: ship-ready behavior and hosting.
/// </summary>
public sealed class WebGlShipReadyRuntime : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject(nameof(WebGlShipReadyRuntime));
        DontDestroyOnLoad(go);
        go.AddComponent<WebGlShipReadyRuntime>();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // WebGL: true when the tab/window loses focus; mutes all listeners without touching Time.timeScale.
        AudioListener.pause = pauseStatus;
    }
#endif
}
