using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// When <b>Active Input Handling</b> is <b>Input System Package (New)</b>, Unity's default
/// <see cref="StandaloneInputModule"/> still calls <see cref="UnityEngine.Input"/> and throws.
/// Ensures every <see cref="EventSystem"/> uses <see cref="InputSystemUIInputModule"/> instead.
/// </summary>
static class GeoWorldEventSystemInputBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnFirstScene()
    {
        UpgradeAllEventSystems();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpgradeAllEventSystems();
    }

    static void UpgradeAllEventSystems()
    {
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include))
        {
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone == null)
                continue;

            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();

            Object.Destroy(standalone);
        }
    }
}
