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

        GameObject shot = (GameObject)Instantiate(geoShotProjectile, camPos.position + camPos.forward * 5, camPos.rotation);
        shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 10, ForceMode.Impulse);
    }

    public void geoManiaShoot()
    {
        int randomValue = Random.Range(1, 5);
        m_Player.curMana -= manacost;

        for(int i = 0; i <= randomValue; i++)
        {
            GameObject shot = (GameObject)Instantiate(geoShotProjectile, camPos.position + camPos.forward * 5 * 5 * i, camPos.rotation);
            shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 10f, ForceMode.Impulse);
        }
    }

    public float getGeoShotDmg()
    {
        return geoShotDmg;
    }
}