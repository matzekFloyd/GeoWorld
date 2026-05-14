using UnityEngine;
using System.Collections;

public class GeoShot : SkillBasic
{

    public GameObject geoShotProjectile;
    public Transform camPos;

    public float geoShotDmg;


    // Use this for initialization
    void Start()
    {
        curCooldown = 0;
        maxCooldown = 0.15f;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Player == null) return;
        manacost = m_Player.getCurLevel();
        geoShotDmg = m_Player.getCurLevel() * 20f;
        updateCoolDown();
        
        if (GameInput.FirePrimaryDown && requiredMana() && CanUseSkills())
        {
            if (curCooldown == 0)
            {
                if (geoManiaActivated())
                {
                    geoManiaShoot();
                    curCooldown = maxCooldown;
                }
                shoot();
                curCooldown = maxCooldown;
            }

        }

    }

    public void shoot()
    {
        m_Player.curMana -= manacost;
        GameplaySfx.Instance?.PlayGeoShotCast();

        var pools = GeoWorldObjectPools.Instance;
        GameObject shot = pools != null
            ? pools.Acquire(geoShotProjectile, camPos.position + camPos.forward * 5, camPos.rotation, null)
            : Instantiate(geoShotProjectile, camPos.position + camPos.forward * 5, camPos.rotation);
        GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(shot);
        shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 10, ForceMode.Impulse);
    }

    public void geoManiaShoot()
    {
        int randomValue = Random.Range(1, 5);
        m_Player.curMana -= manacost;

        var pools = GeoWorldObjectPools.Instance;
        for(int i = 0; i <= randomValue; i++)
        {
            GameObject shot = pools != null
                ? pools.Acquire(geoShotProjectile, camPos.position + camPos.forward * 5 * 5 * i, camPos.rotation, null)
                : Instantiate(geoShotProjectile, camPos.position + camPos.forward * 5 * 5 * i, camPos.rotation);
            GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(shot);
            shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 10f, ForceMode.Impulse);
        }
    }

    public float getGeoShotDmg()
    {
        return geoShotDmg;
    }
}