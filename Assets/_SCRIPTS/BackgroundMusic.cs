using UnityEngine;

/// <summary>
/// Loops background music on the same GameObject as <see cref="AudioSource"/>.
/// Inherits <see cref="GameOver"/> only to reuse end-of-round flags for stopping playback (legacy layout).
/// </summary>
public class BackgroundMusic : GameOver
{
    [Tooltip("Primary track if no alternates are picked.")]
    public AudioClip backGroundMusic;

    [Tooltip("If non-empty, one clip is chosen at random (including backGroundMusic in the pool when assigned).")]
    public AudioClip[] alternateTracks;

    [Range(0f, 1f)]
    public float playbackVolume = 1f;

    AudioSource a;

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
            a.Play();
        }
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
        if (playerDied || gameTimeIsOver)
            a.Stop();
    }
}
