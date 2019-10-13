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
        geoBlastDmg = player.GetComponent<PlayerCharacter>().getCurLevel() * 4f;
        projectileCount = player.GetComponent<PlayerCharacter>().getCurLevel() * 5;
        updateCoolDown();

        if (Input.GetMouseButtonDown(1) && requiredMana() && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
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
        player.GetComponent<PlayerCharacter>().curMana -= manacost;

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

            GameObject shot = (GameObject)Instantiate(geoBlastProjectile, shotSpawnPosition, camPos.rotation);

            shot.GetComponent<Rigidbody>().AddForce(shotDirectionNormalized * shotspeed, ForceMode.Impulse);
        }


    }

    public float getGeoBlastDmg()
    {
        return geoBlastDmg;
    }
}