using UnityEngine;

/// <summary>
/// Central 2D gameplay one-shots (skills, enemy lifecycle) with per-channel rate limits to avoid machine-gun audio.
/// Optional <see cref="AudioClip"/> fields: null = silent. Lives on the same object as <see cref="UserInterface"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplaySfx : MonoBehaviour
{
    static GameplaySfx s_Instance;

    /// <summary>Resolves even if this behaviour’s GameObject started inactive (Awake had not run yet).</summary>
    public static GameplaySfx Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;
#if UNITY_2023_1_OR_NEWER
            s_Instance = FindAnyObjectByType<GameplaySfx>(FindObjectsInactive.Include);
#else
            s_Instance = FindObjectOfType<GameplaySfx>();
#endif
            return s_Instance;
        }
    }

    public enum Channel
    {
        GeoShot,
        GeoBlast,
        Meteor,
        Heal,
        BloodRitual,
        FreezeTime,
        EnemySpawnNormal,
        EnemySpawnElite,
        EnemyMeleeAttack,
        EnemyRangedAttack,
        EnemyDie,
        BossIncoming,
        Count
    }

    [Header("Skills (optional)")]
    [SerializeField] AudioClip _geoShotCast;
    [SerializeField] AudioClip _geoBlastCast;
    [SerializeField] AudioClip _meteorCast;
    [SerializeField] AudioClip _healCast;
    [SerializeField] AudioClip _bloodRitualCast;
    [SerializeField] AudioClip _freezeTimeCast;

    [Header("Enemies (optional)")]
    [SerializeField] AudioClip _enemySpawnNormal;
    [SerializeField] AudioClip _enemySpawnElite;
    [SerializeField] AudioClip _enemyMeleeAttack;
    [SerializeField] AudioClip _enemyRangedAttack;
    [SerializeField] AudioClip _enemyDieNormal;
    [SerializeField] AudioClip _enemyDieBoss;
    [SerializeField] AudioClip _bossIncomingStinger;

    [Header("Levels")]
    [SerializeField, Range(0f, 1f)] float _skillVolume = 0.88f;
    [SerializeField, Range(0f, 1f)] float _enemyVolume = 0.82f;

    AudioSource _source;
    readonly float[] _lastPlayUnscaled = new float[(int)Channel.Count];
    static bool s_LoggedMissingClips;

    void Awake()
    {
        RegisterInstance();
        EnsureAudioSource();
    }

    void OnEnable()
    {
        RegisterInstance();
        EnsureAudioSource();
    }

    void Start()
    {
        EnsureSceneHasAudioListener();
        MaybeLogMissingClipsOnce();
    }

    /// <summary>Safe to call from other systems (e.g. <see cref="CombatFeedback"/>) so one-shots are audible without a custom rig.</summary>
    public static void EnsureSceneHasAudioListener() => EnsureAudioListenerExists();

    void RegisterInstance()
    {
        if (s_Instance != null && s_Instance != this)
            return;
        s_Instance = this;
    }

    void OnDestroy()
    {
        if (s_Instance == this)
            s_Instance = null;
    }

    void EnsureAudioSource()
    {
        if (_source != null)
            return;
        var host = new GameObject("GameplaySfxAudio");
        host.transform.SetParent(transform, false);
        _source = host.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.dopplerLevel = 0f;
        _source.loop = false;
        _source.volume = 1f;
        _source.mute = false;
        _source.enabled = true;
    }

    static void EnsureAudioListenerExists()
    {
#if UNITY_2023_1_OR_NEWER
        if (FindAnyObjectByType<AudioListener>(FindObjectsInactive.Exclude) != null)
            return;
#else
        if (FindObjectOfType<AudioListener>() != null)
            return;
#endif
        var cam = Camera.main != null ? Camera.main : FindFirstObjectByTypeCompat<Camera>();
        if (cam != null && cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    static Camera FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<Camera>();
#else
        return FindObjectOfType<Camera>();
#endif
    }

    void MaybeLogMissingClipsOnce()
    {
        if (s_LoggedMissingClips)
            return;
        if (HasAnyClipAssigned())
            return;
        s_LoggedMissingClips = true;
#if UNITY_EDITOR
        Debug.Log(
            "GameplaySfx: no AudioClips assigned — in the Editor, short synthetic blips are used so you can hear triggers. " +
            "Assign real clips on this component (same object as UserInterface) for proper SFX.");
#else
        Debug.LogWarning(
            "GameplaySfx: no AudioClips are assigned on this component — gameplay SFX will be silent until you assign clips " +
            "(e.g. Geo Shot Cast, Heal Cast) on the same GameObject as UserInterface / GameplayHudView.");
#endif
    }

    bool HasAnyClipAssigned()
    {
        return _geoShotCast != null || _geoBlastCast != null || _meteorCast != null || _healCast != null ||
               _bloodRitualCast != null || _freezeTimeCast != null || _enemySpawnNormal != null || _enemySpawnElite != null ||
               _enemyMeleeAttack != null || _enemyRangedAttack != null || _enemyDieNormal != null || _enemyDieBoss != null ||
               _bossIncomingStinger != null;
    }

    /// <summary>Per-skill clip (e.g. HealSelf.healSound) or fallback to library default when <paramref name="clip"/> is null.</summary>
    public void PlaySkillClipOrDefault(AudioClip clip, Channel channel, AudioClip defaultClip, float minIntervalUnscaled, float volumeScale = 1f)
    {
        var c = clip != null ? clip : defaultClip;
        TryPlay(c, channel, _skillVolume * volumeScale, minIntervalUnscaled, false);
    }

    public void PlayGeoShotCast() => TryPlay(_geoShotCast, Channel.GeoShot, _skillVolume, 0.05f, false);
    public void PlayGeoBlastCast() => TryPlay(_geoBlastCast, Channel.GeoBlast, _skillVolume, 0.12f, false);
    public void PlayMeteorCast() => TryPlay(_meteorCast, Channel.Meteor, _skillVolume, 0.2f, false);
    public void PlayHealCast(AudioClip perSkillOverride) => PlaySkillClipOrDefault(perSkillOverride, Channel.Heal, _healCast, 0.12f);
    public void PlayBloodRitualCast() => TryPlay(_bloodRitualCast, Channel.BloodRitual, _skillVolume, 0.15f, false);
    public void PlayFreezeTimeCast() => TryPlay(_freezeTimeCast, Channel.FreezeTime, _skillVolume, 0.18f, false);

    public void PlayEnemySpawnNormal() => TryPlay(_enemySpawnNormal, Channel.EnemySpawnNormal, _enemyVolume, 0.35f, false);
    public void PlayEnemySpawnElite() => TryPlay(_enemySpawnElite, Channel.EnemySpawnElite, _enemyVolume, 0.35f, false);
    public void PlayEnemyMeleeAttack() => TryPlay(_enemyMeleeAttack, Channel.EnemyMeleeAttack, _enemyVolume, 0.1f, false);
    public void PlayEnemyRangedAttack() => TryPlay(_enemyRangedAttack, Channel.EnemyRangedAttack, _enemyVolume, 0.12f, false);
    public void PlayEnemyDie(bool isBoss)
    {
        var clip = isBoss && _enemyDieBoss != null ? _enemyDieBoss : _enemyDieNormal;
        TryPlay(clip, Channel.EnemyDie, _enemyVolume, 0.08f, isBoss);
    }

    public void PlayBossIncoming() => TryPlay(_bossIncomingStinger, Channel.BossIncoming, _enemyVolume, 0.2f, false);

    void TryPlay(AudioClip clip, Channel channel, float volume, float minIntervalUnscaled, bool bossPitchJitter)
    {
        EnsureAudioSource();
        if (_source == null)
            return;

        var i = (int)channel;
        if (clip == null)
            clip = ProceduralEditorBlips.Get(i);
        if (clip == null)
            return;
        var t = Time.unscaledTime;
        if (t - _lastPlayUnscaled[i] < minIntervalUnscaled)
            return;
        _lastPlayUnscaled[i] = t;

        var v = volume;
        if (CombatFeedback.ReducedMotion)
            v *= 0.72f;

        v = Mathf.Clamp01(v);
        if (v <= 0.0001f)
            return;

        if (bossPitchJitter)
        {
            var p = _source.pitch;
            _source.pitch = Random.Range(0.94f, 1.07f);
            _source.PlayOneShot(clip, v);
            _source.pitch = p;
        }
        else
            _source.PlayOneShot(clip, v);
    }
}
