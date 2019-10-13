using UnityEngine;
using System.Collections;

public class GeoBlastProjectile : GeoBlast
{
    private float lifestealPerHit;

    // Use this for initialization
    void Start()
    {
        Destroy(this.gameObject, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        lifestealPerHit = player.GetComponent<GeoBlast>().getGeoBlastDmg() / 5;
    }

    void OnCollisionEnter(Collision something)
    {
        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(player.GetComponent<GeoBlast>().getGeoBlastDmg());
            Destroy(this.gameObject);
            player.GetComponent<PlayerCharacter>().changeCurrentHealth(lifestealPerHit);
        }
        else if (something.gameObject.tag == "Enemy")
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(player.GetComponent<GeoBlast>().getGeoBlastDmg());
            Destroy(this.gameObject);
        }

    }
}