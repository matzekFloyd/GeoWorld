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
        manacost = player.GetComponent<PlayerCharacter>().getCurLevel();
        geoShotDmg = player.GetComponent<PlayerCharacter>().getCurLevel() * 20f;
        updateCoolDown();
        
        if (Input.GetButtonDown("Fire1") && requiredMana() && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
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
        player.GetComponent<PlayerCharacter>().curMana -= manacost;

        GameObject shot = (GameObject)Instantiate(geoShotProjectile, camPos.position + camPos.forward * 5, camPos.rotation);
        shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 10, ForceMode.Impulse);
    }

    public void geoManiaShoot()
    {
        int randomValue = Random.Range(1, 5);
        player.GetComponent<PlayerCharacter>().curMana -= manacost;

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