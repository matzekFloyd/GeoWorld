using UnityEngine;

public class GeoShot : SkillBasic
{
    public GameObject geoShotProjectile;
    public Transform camPos;

    public float geoShotDmg;

    [Header("Projectile tuning")]
    [SerializeField] float damagePerLevel = 24f;
    [SerializeField] float baseLaunchImpulse = 10f;
    [SerializeField] float launchImpulsePerLevel = 0.38f;
    [SerializeField] float baseLifetimeSeconds = 2.6f;
    [SerializeField] float lifetimePerLevel = 0.09f;
    [SerializeField] int baseMaxBounces = 5;
    [SerializeField] int extraBouncesEveryNLevels = 4;

    [Header("Geo Mania (slot 10) bounce bonus")]
    [SerializeField] int maniaBonusBounces = 7;
    [SerializeField] float maniaBonusLifetimeSeconds = 2.4f;
    [SerializeField] float bounceSpeedRetention = 0.9f;

    void Start()
    {
        curCooldown = 0;
        maxCooldown = 0.24f;
    }

    void Update()
    {
        if (m_Player == null) return;
        int lv = m_Player.getCurLevel();
        manacost = (0.85f + lv * 0.38f) * GameBalanceHelper.SkillManaCostScale;
        geoShotDmg = GetDamageForLevel(lv);
        updateCoolDown();

        if (GameInput.FirePrimaryHeld && requiredMana() && CanUseSkills())
        {
            if (curCooldown == 0)
            {
                shoot();
                curCooldown = maxCooldown;
            }
        }
    }

    public void shoot()
    {
        m_Player.changeCurrentMana(-manacost);
        GameplaySfx.Instance?.PlayGeoShotCast();

        int lv = m_Player.getCurLevel();
        bool mania = geoManiaActivated();
        float impulse = GetLaunchImpulse(lv);
        var spawnPos = camPos.position + camPos.forward * 5f;
        var rot = camPos.rotation;

        var pools = GeoWorldObjectPools.Instance;
        GameObject shot = pools != null
            ? pools.Acquire(geoShotProjectile, spawnPos, rot, null)
            : Instantiate(geoShotProjectile, spawnPos, rot);

        GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(shot);

        var rb = shot.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(camPos.forward * impulse, ForceMode.Impulse);

        var projectile = shot.GetComponent<GeoShotProjectile>();
        if (projectile != null)
            projectile.Configure(this, lv, mania);
    }

    public float GetDamageForLevel(int level) => Mathf.Max(1, level) * damagePerLevel;

    public float GetLaunchImpulse(int level) =>
        baseLaunchImpulse + Mathf.Max(1, level) * launchImpulsePerLevel;

    public float GetLifetimeSeconds(int level, bool mania)
    {
        float t = baseLifetimeSeconds + Mathf.Max(1, level) * lifetimePerLevel;
        if (mania)
            t += maniaBonusLifetimeSeconds;
        return t;
    }

    public int GetMaxBounces(int level, bool mania)
    {
        int b = baseMaxBounces + Mathf.Max(0, level / Mathf.Max(1, extraBouncesEveryNLevels));
        if (mania)
            b += maniaBonusBounces;
        return b;
    }

    public float BounceSpeedRetention => bounceSpeedRetention;

    public float getGeoShotDmg() => geoShotDmg;
}
