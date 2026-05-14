using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreezeTime : SkillBasic
{

    public Texture2D freezeTexture;
    private float freezeTextureTimer;
    private float freezeTextureTimerCooldown;
    private bool showFreezeTexture;

    public List<GameObject> enemiesToFreeze;
    public float duration;

    [Header("Freeze duration vs distance")]
    [Tooltip("Horizontal distance at which freeze length reaches the minimum fraction (still affects all enemies in the list).")]
    [SerializeField] float freezeFalloffMaxDistance = 38f;

    [Tooltip("Freeze length at max falloff distance as a fraction of the nominal max (at the player's feet).")]
    [Range(0.05f, 1f)]
    [SerializeField] float freezeMinDurationFraction = 0.22f;

    [Header("Close-range max freeze (nominal)")]
    [Tooltip("Freeze duration at 0 horizontal distance from the player ≈ level × this (seconds per level).")]
    [SerializeField] float closeRangeFreezeSecondsPerLevel = 0.58f;

    [Tooltip("Floor for nominal max freeze duration (seconds), after level scaling.")]
    [SerializeField] float closeRangeFreezeMinimumSeconds = 0.4f;

    [Header("Freeze de-sync")]
    [Tooltip("± half of this value (seconds) is added per enemy/missile from a stable seed so similar distances do not unfreeze on the same frame.")]
    [SerializeField] float freezeDurationInstanceJitterSeconds = 0.28f;


    // Use this for initialization
    void Start()
    {
        curCooldown = 0;
        freezeTextureTimer = 1;
        showFreezeTexture = false;

        enemiesToFreeze = new List<GameObject>();

        GameObject[] go = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in go)
        {
            AddTarget(enemy);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Player == null) return;
        maxCooldown = 100 / m_Player.getCurLevel();
        freezeTextureTimerCooldown = 1.5f;
        int lvl = Mathf.Max(1, m_Player.getCurLevel());
        duration = Mathf.Max(closeRangeFreezeMinimumSeconds, lvl * closeRangeFreezeSecondsPerLevel);
        manacost = 48f + m_Player.getCurLevel() * 38f;
        updateCoolDown();


        if (m_Player.skillAvailable(8))
        {
            if (GameInput.SkillFreezeTimeHeld && requiredMana() && CanUseSkills())
            {
                if (curCooldown == 0)
                {
                    showFreezeTexture = true;
                    freezeTime();
                    curCooldown = maxCooldown;
                    freezeTextureTimer = freezeTextureTimerCooldown;
                }

            }

        }
        if (showFreezeTexture) calculateFreezeTextureCooldown();
    }

    public void freezeTime()
    {

        m_Player.changeCurrentMana(-manacost);
        GameplaySfx.Instance?.PlayFreezeTimeCast();

        Vector3 playerPos = m_Player.transform.position;
        float maxDur = Mathf.Max(0.05f, duration);
        float falloff = Mathf.Max(1f, freezeFalloffMaxDistance);
        float minFrac = Mathf.Clamp(freezeMinDurationFraction, 0.05f, 1f);

        for (int i = 0; i < enemiesToFreeze.Count; i++)
        {
            GameObject go = enemiesToFreeze[i];
            if (go == null)
                continue;
            var ai = go.GetComponent<EnemyAI>();
            if (ai == null)
                continue;

            float distH = HorizontalDistance(playerPos, go.transform.position);
            int jitterSeed = FreezeJitterSeed(go, i);
            float perEnemyDur = ComputeFreezeSecondsForHorizontalDistance(
                distH, maxDur, falloff, minFrac, jitterSeed, freezeDurationInstanceJitterSeconds);
            ai.freeze(perEnemyDur);
        }

        ApplyFreezeToHomingMissiles(playerPos, maxDur, falloff, minFrac, freezeDurationInstanceJitterSeconds);
            
    }

    static void ApplyFreezeToHomingMissiles(Vector3 playerPos, float maxDur, float falloff, float minFrac, float jitterSpan)
    {
        var appliedMissiles = new HashSet<HomingMissileAI>();
        ApplyFreezeToHomingMissilesWithTag("SmallHomingMissile", playerPos, maxDur, falloff, minFrac, jitterSpan, appliedMissiles);
        ApplyFreezeToHomingMissilesWithTag("BigHomingMissile", playerPos, maxDur, falloff, minFrac, jitterSpan, appliedMissiles);

        HomingMissileAI[] loose = Object.FindObjectsByType<HomingMissileAI>(FindObjectsInactive.Exclude);
        for (int i = 0; i < loose.Length; i++)
        {
            HomingMissileAI missile = loose[i];
            if (missile == null || !missile.gameObject.activeInHierarchy)
                continue;
            if (!TransformHasHomingMissileTag(missile.transform))
                continue;
            if (!appliedMissiles.Add(missile))
                continue;
            int jitterSeed = FreezeJitterSeed(missile, i);
            float distM = HorizontalDistance(playerPos, missile.transform.position);
            float perMissileDur = ComputeFreezeSecondsForHorizontalDistance(
                distM, maxDur, falloff, minFrac, jitterSeed, jitterSpan);
            missile.ApplyFreeze(perMissileDur);
        }
    }

    static void ApplyFreezeToHomingMissilesWithTag(string tag, Vector3 playerPos, float maxDur, float falloff, float minFrac, float jitterSpan, HashSet<HomingMissileAI> appliedMissiles)
    {
        GameObject[] arr;
        try
        {
            arr = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            arr = null;
        }

        if (arr == null)
            return;

        for (int i = 0; i < arr.Length; i++)
        {
            GameObject go = arr[i];
            if (go == null || !go.activeInHierarchy)
                continue;
            var missile = go.GetComponent<HomingMissileAI>() ?? go.GetComponentInChildren<HomingMissileAI>(true);
            if (missile == null)
                continue;
            if (!appliedMissiles.Add(missile))
                continue;

            int jitterSeed = FreezeJitterSeed(missile, i);
            float distM = HorizontalDistance(playerPos, missile.transform.position);
            float perMissileDur = ComputeFreezeSecondsForHorizontalDistance(
                distM, maxDur, falloff, minFrac, jitterSeed, jitterSpan);
            missile.ApplyFreeze(perMissileDur);
        }
    }

    static bool TransformHasHomingMissileTag(Transform t)
    {
        for (Transform x = t; x != null; x = x.parent)
        {
            if (x.CompareTag("SmallHomingMissile") || x.CompareTag("BigHomingMissile"))
                return true;
        }
        return false;
    }

    static int FreezeJitterSeed(UnityEngine.Object obj, int salt)
    {
        int h = obj != null ? obj.GetEntityId().GetHashCode() : 0;
        return h ^ (salt * unchecked((int)0x9E3779B9));
    }

    static float ComputeFreezeSecondsForHorizontalDistance(
        float horizontalDistance,
        float maxDur,
        float falloff,
        float minFrac,
        int jitterSeed,
        float jitterSpan)
    {
        float u = Mathf.Clamp01(horizontalDistance / falloff);
        u = u * u * (3f - 2f * u);
        float baseSeconds = maxDur * Mathf.Lerp(1f, minFrac, u);
        return Mathf.Max(0.03f, baseSeconds + InstanceJitter(jitterSeed, jitterSpan));
    }

    static float InstanceJitter(int jitterSeed, float span)
    {
        span = Mathf.Max(0f, span);
        if (span <= 0f)
            return 0f;
        int h = (jitterSeed * 374761393) ^ (jitterSeed >> 13);
        float n01 = (h & 0xFFFF) / 65535f;
        return (n01 * 2f - 1f) * (span * 0.5f);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = b.x - a.x;
        float dz = b.z - a.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public void AddTarget(GameObject targetsToFreeze)
    {
        enemiesToFreeze.Add(targetsToFreeze);
    }

    void LateUpdate()
    {
        var hud = GameplayHudView.Instance;
        if (hud == null)
            return;
        bool on = showFreezeTexture && CanUseSkills();
        hud.ConfigureFreezeFx(on, freezeTexture);
    }

    protected void calculateFreezeTextureCooldown()
    {
        if (freezeTextureTimer > 0)
        {
            freezeTextureTimer -= Time.deltaTime;
        }
        if (freezeTextureTimer < 0)
        {
            freezeTextureTimer = 0;
            showFreezeTexture = false;
        }
        if (freezeTextureTimer == 0)
        {
            showFreezeTexture = false;
        }

    }
}