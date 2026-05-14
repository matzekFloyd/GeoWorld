using System.Collections;
using UnityEngine;

/// <summary>
/// Loops background music on the same GameObject as <see cref="AudioSource"/>.
/// Stops when <see cref="GameSession"/> reports the run has ended (player death or time over).
/// </summary>
public class BackgroundMusic : MonoBehaviour
{
    [Tooltip("Primary track if no alternates are picked.")]
    public AudioClip backGroundMusic;

    [Tooltip("If non-empty, one clip is chosen at random (including backGroundMusic in the pool when assigned).")]
    public AudioClip[] alternateTracks;

    [Range(0f, 1f)]
    public float playbackVolume = 1f;

    AudioSource a;

#if UNITY_WEBGL && !UNITY_EDITOR
    bool m_WaitingForFirstGesture;
#endif

    void Start()
    {
        a = GetComponent<AudioSource>();
        if (a == null) return;

        a.volume = playbackVolume;
        a.loop = true;

        AudioClip clipToPlay = PickTrack();
        if (clipToPlay != null)
        {
            a.clip = clipToPlay;
#if UNITY_WEBGL && !UNITY_EDITOR
            // Title screen already captured a gesture → try Play immediately; verify next frame (README).
            if (GeoWorldSessionStart.TitleScreenProvidedUserGestureForAudio)
            {
                GeoWorldSessionStart.ConsumeTitleScreenGestureForAudio();
                a.Play();
                m_WaitingForFirstGesture = false;
                StartCoroutine(VerifyWebGlBgmPlayingAfterFrame());
            }
            else
            {
                m_WaitingForFirstGesture = true;
            }
#else
            a.Play();
#endif
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    IEnumerator VerifyWebGlBgmPlayingAfterFrame()
    {
        yield return null;
        if (a != null && a.clip != null && RunStillActiveForMusic() && !a.isPlaying)
            m_WaitingForFirstGesture = true;
    }
#endif

    static bool RunStillActiveForMusic()
    {
        var s = GameSession.Instance;
        if (s == null)
            return true;
        return s.IsRunActive;
    }

    AudioClip PickTrack()
    {
        if (alternateTracks == null || alternateTracks.Length == 0)
            return backGroundMusic;

        if (backGroundMusic == null)
            return alternateTracks[Random.Range(0, alternateTracks.Length)];

        int pool = alternateTracks.Length + 1;
        int roll = Random.Range(0, pool);
        if (roll >= alternateTracks.Length)
            return backGroundMusic;
        return alternateTracks[roll];
    }

    void Update()
    {
        if (a == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (m_WaitingForFirstGesture)
        {
            if (GeoWorldInputCompat.AnyKeyOrMouseButtonDownThisFrame())
            {
                a.Play();
                m_WaitingForFirstGesture = false;
            }
        }
#endif

        if (!RunStillActiveForMusic())
            a.Stop();
    }
}
