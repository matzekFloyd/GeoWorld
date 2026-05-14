using UnityEngine;
using System.Collections;

public class GeoBlast : SkillBasic
{

    public GameObject geoBlastProjectile;
    public Transform camPos;
    protected float geoBlastDmg;
    protected int projectileCount;
    protected float shotspeed;

    // Use this for initialization
    void Start()
    {
        maxCooldown = 0.6f;
        manacost = 0;
        shotspeed = 20;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Player == null) return;
        geoBlastDmg = m_Player.getCurLevel() * 4f;
        projectileCount = m_Player.getCurLevel() * 5;
        manacost = 6f + m_Player.getCurLevel() * 7.5f;
        updateCoolDown();

        if (GameInput.SecondaryMouseDown && requiredMana() && CanUseSkills())
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
        m_Player.curMana -= manacost;
        GameplaySfx.Instance?.PlayGeoBlastCast();

        var pools = GeoWorldObjectPools.Instance;
        for(int i = 0; i <= projectileCount; i++)
        {
            double spread = Random.Range(0.1f,2);
            float randomValue = Random.Range(0f, 2 * Mathf.PI);
            float xOffset = Mathf.Cos(randomValue);
            float yOffset = Mathf.Sin(randomValue);

            var zentrum = camPos.position + camPos.forward * 15;
            var cameraRight = zentrum + camPos.right * xOffset * (float)spread;
            var shotPosition = cameraRight + camPos.up * yOffset * (float)spread;
            var shotDirection = shotPosition - camPos.position;
            var shotDirectionNormalized = shotDirection.normalized;
            var shotSpawnPosition = shotPosition + (-shotDirectionNormalized * 13);

            GameObject shot = pools != null
                ? pools.Acquire(geoBlastProjectile, shotSpawnPosition, camPos.rotation, null)
                : Instantiate(geoBlastProjectile, shotSpawnPosition, camPos.rotation);

            GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(shot);
            shot.GetComponent<Rigidbody>().AddForce(shotDirectionNormalized * shotspeed, ForceMode.Impulse);
        }


    }

    public float getGeoBlastDmg()
    {
        return geoBlastDmg;
    }
}