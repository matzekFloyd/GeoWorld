using System.Collections;
using UnityEngine;

/// <summary>
/// Runtime combat juice: player damage HUD pulse, enemy hit scale punch, optional clips, light camera shake,
/// micro hit-stop for <see cref="CombatHitSeverity.Heavy"/>, and <see cref="BattleLog"/> lines. Lives on the same object as <see cref="UserInterface"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class CombatFeedback : MonoBehaviour
{
    public const string ReducedMotionPlayerPrefsKey = "GeoWorld.CombatReducedMotion";

    public static CombatFeedback Instance { get; private set; }

    [SerializeField] AudioClip _playerHitClip;
    [SerializeField] AudioClip _enemyHitClip;
    [SerializeField, Range(0f, 0.08f)] float _cameraShakeMax = 0.035f;
    [SerializeField, Range(0f, 0.2f)] float _cameraShakeDuration = 0.12f;

    AudioSource _audio;
    Camera _cachedCamera;
    Coroutine _shakeRoutine;
    static bool s_hitStopRoutineRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        RegisterAndEnsureAudio();
    }

    void Start()
    {
        GameplaySfx.EnsureSceneHasAudioListener();
        ResolveCamera();
    }

    void OnEnable()
    {
        RegisterAndEnsureAudio();
    }

    void RegisterAndEnsureAudio()
    {
        if (Instance != null && Instance != this)
            return;
        Instance = this;
        if (_audio == null)
            _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.dopplerLevel = 0f;
        _audio.volume = 1f;
        _audio.mute = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool ReducedMotion =>
        PlayerPrefs.GetInt(ReducedMotionPlayerPrefsKey, 0) != 0;

    /// <summary>Call from a settings UI; 1 = reduced combat motion/intensity.</summary>
    public static void SetCombatReducedMotion(bool on) =>
        PlayerPrefs.SetInt(ReducedMotionPlayerPrefsKey, on ? 1 : 0);

    public void NotifyPlayerDamaged(float amount, Vector3 worldSource, bool hasWorldSource, CombatHitSeverity severity)
    {
        RegisterAndEnsureAudio();
        var hud = GameplayHudView.Instance;
        bool reduced = ReducedMotion;
        float centerPeak = severity switch
        {
            CombatHitSeverity.Heavy => 0.32f,
            CombatHitSeverity.Medium => 0.22f,
            _ => 0.14f,
        };
        float edgePeak = severity switch
        {
            CombatHitSeverity.Heavy => 0.55f,
            CombatHitSeverity.Medium => 0.42f,
            _ => 0.3f,
        };
        if (reduced)
        {
            centerPeak *= 0.4f;
            edgePeak *= 0.35f;
        }

        float dirX = 0f, dirY = 0f;
        bool hasDir = false;
        if (hasWorldSource && TryScreenDirection(worldSource, out dirX, out dirY))
            hasDir = true;

        if (hud != null)
            hud.PlayHitTakenFeedback(hasDir, dirX, dirY, centerPeak, edgePeak, 0.16f, reduced);

        var dmgPool = FloatingDamageNumberPool.Instance;
        if (dmgPool != null)
        {
            var p = GameObject.FindGameObjectWithTag("Player1");
            if (p != null)
                dmgPool.SpawnPlayerDamageTaken(p.transform, amount, severity);
        }

        BattleLog.AppendPlayerDamageTaken(amount, severity);

        var playerHit = _playerHitClip != null ? _playerHitClip : ProceduralEditorBlips.Get(110);
        if (playerHit != null && _audio != null)
            _audio.PlayOneShot(playerHit, Mathf.Clamp01(0.55f + amount / 500f));

        if (!reduced && _cameraShakeMax > 0.0001f)
        {
            float mul = severity switch
            {
                CombatHitSeverity.Heavy => 1.35f,
                CombatHitSeverity.Medium => 1f,
                _ => 0.65f,
            };
            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(CameraShakeRoutine(mul));
        }

        if (!reduced && severity == CombatHitSeverity.Heavy)
            StartCoroutine(MicroHitStopRoutine());
    }

    public void NotifyEnemyHit(Transform enemyRoot, float damage, Vector3? hitOrigin, CombatHitSeverity severity)
    {
        if (enemyRoot == null)
            return;

        RegisterAndEnsureAudio();
        var enemyHit = _enemyHitClip != null ? _enemyHitClip : ProceduralEditorBlips.Get(111);
        if (enemyHit != null && _audio != null)
            _audio.PlayOneShot(enemyHit, Mathf.Clamp01(0.45f + damage / 800f));

        float punch = severity switch
        {
            CombatHitSeverity.Heavy => 1.12f,
            CombatHitSeverity.Medium => 1.07f,
            _ => 1.05f,
        };
        float dur = severity switch
        {
            CombatHitSeverity.Heavy => 0.14f,
            _ => 0.1f,
        };
        if (ReducedMotion)
        {
            punch = 1f + (punch - 1f) * 0.45f;
            dur *= 0.75f;
        }

        var ecForPunch = enemyRoot.GetComponent<EnemyCharacter>();
        if (ecForPunch != null && ecForPunch.isBoss)
            punch = 1f + (punch - 1f) * 0.55f;

        var restScale = ecForPunch != null ? ecForPunch.HitPunchRestLocalScale : enemyRoot.localScale;
        StartCoroutine(ScalePunchRoutine(enemyRoot, restScale, punch, dur));

        var dmgPool = FloatingDamageNumberPool.Instance;
        if (dmgPool != null)
        {
            bool isBoss = ecForPunch != null && ecForPunch.isBoss;
            dmgPool.SpawnEnemyDamage(enemyRoot, damage, severity, isBoss);
        }

        BattleLog.AppendEnemyHit(enemyRoot, damage, severity);
    }

    bool TryScreenDirection(Vector3 world, out float dx, out float dy)
    {
        dx = dy = 0f;
        var cam = ResolveCamera();
        if (cam == null)
            return false;
        var vp = cam.WorldToViewportPoint(world);
        if (vp.z < 0.05f)
            return false;
        dx = (vp.x - 0.5f) * 2f;
        dy = (vp.y - 0.5f) * 2f;
        if (Mathf.Abs(dx) < 0.05f && Mathf.Abs(dy) < 0.05f)
            return false;
        return true;
    }

    Camera ResolveCamera()
    {
        if (_cachedCamera != null)
            return _cachedCamera;
        _cachedCamera = Object.FindAnyObjectByType<Camera>();
        return _cachedCamera;
    }

    IEnumerator CameraShakeRoutine(float severityMul)
    {
        var cam = ResolveCamera();
        if (cam == null)
            yield break;

        Vector3 restLocal = cam.transform.localPosition;

        float t = 0f;
        float dur = _cameraShakeDuration;
        while (t < dur)
        {
            t += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(t / dur);
            float mag = _cameraShakeMax * fade * severityMul;
            var off = new Vector3(
                (Mathf.PerlinNoise(t * 41.7f, 0f) - 0.5f) * 2f * mag,
                (Mathf.PerlinNoise(0f, t * 38.2f) - 0.5f) * 2f * mag,
                0f);
            cam.transform.localPosition = restLocal + off;
            yield return null;
        }

        cam.transform.localPosition = restLocal;
        _shakeRoutine = null;
    }

    static IEnumerator MicroHitStopRoutine()
    {
        if (s_hitStopRoutineRunning)
            yield break;
        float prev = Time.timeScale;
        if (prev < 0.08f)
            yield break;
        s_hitStopRoutineRunning = true;
        Time.timeScale = Mathf.Clamp(prev * 0.18f, 0.1f, 0.28f);
        yield return new WaitForSecondsRealtime(0.042f);
        if (Time.timeScale < 0.29f)
            Time.timeScale = prev;
        s_hitStopRoutineRunning = false;
    }

    static IEnumerator ScalePunchRoutine(Transform root, Vector3 restLocalScale, float peakScale, float duration)
    {
        if (root == null)
            yield break;
        var baseScale = restLocalScale;
        if (baseScale.sqrMagnitude < 1e-8f)
            yield break;

        float half = duration * 0.35f;
        float t = 0f;
        while (t < half)
        {
            if (root == null)
                yield break;
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            root.localScale = Vector3.Lerp(baseScale, baseScale * peakScale, u);
            yield return null;
        }

        t = 0f;
        while (t < duration - half)
        {
            if (root == null)
                yield break;
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(1e-4f, duration - half));
            root.localScale = Vector3.Lerp(baseScale * peakScale, baseScale, u);
            yield return null;
        }

        if (root != null)
            root.localScale = baseScale;
    }
}
