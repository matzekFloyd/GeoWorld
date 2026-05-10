using UnityEngine;
using System.Collections;

public class GeoShotProjectile : GeoShot{

    private float lifestealPerHit;
    private float manaGainPerHit;
    private float damagePerHit;

    GeoShot _ownerGeoShot;
    PlayerCharacter _ownerPlayer;

    void Start () {
        Destroy(this.gameObject, 2);
        if (player != null)
        {
            _ownerGeoShot = player.GetComponent<GeoShot>();
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
        }
    }

    void Update () {
        if (_ownerGeoShot == null || _ownerPlayer == null)
            return;
        lifestealPerHit = _ownerGeoShot.getGeoShotDmg() / 10;
        manaGainPerHit = _ownerGeoShot.manacost / 6;
        damagePerHit = _ownerGeoShot.getGeoShotDmg();
    }

    void OnCollisionEnter(Collision something)
    {
        if (_ownerPlayer == null)
            return;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
            Destroy(this.gameObject);

            _ownerPlayer.changeCurrentHealth(lifestealPerHit);
            _ownerPlayer.changeCurrentMana(manaGainPerHit);

        }
        else if (something.gameObject.tag == "Enemy")
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
            Destroy(this.gameObject);
        }
    }

}
