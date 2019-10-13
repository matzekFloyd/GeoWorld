using UnityEngine;
using System.Collections;

public class GeoShotProjectile : GeoShot{

    private float lifestealPerHit;
    private float manaGainPerHit;
    private float damagePerHit;

    // Use this for initialization
    void Start () {
        Destroy(this.gameObject, 2);
    }

    // Update is called once per frame
    void Update () {
        lifestealPerHit = player.GetComponent<GeoShot>().getGeoShotDmg() / 10;
        manaGainPerHit = player.GetComponent<GeoShot>().manacost / 6;
        damagePerHit = player.GetComponent<GeoShot>().getGeoShotDmg();
    }

    void OnCollisionEnter(Collision something)
    {
        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
            Destroy(this.gameObject);

            player.GetComponent<PlayerCharacter>().changeCurrentHealth(lifestealPerHit);
            player.GetComponent<PlayerCharacter>().changeCurrentMana(manaGainPerHit);

        }
        else if (something.gameObject.tag == "Enemy")
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
            Destroy(this.gameObject);
        }
    }

}
