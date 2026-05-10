using UnityEngine;
using System.Collections;

public class GeoBlastProjectile : GeoBlast
{
    private float lifestealPerHit;

    GeoBlast _ownerGeoBlast;
    PlayerCharacter _ownerPlayer;

    void Start()
    {
        Destroy(this.gameObject, 0.5f);
        if (player != null)
        {
            _ownerGeoBlast = player.GetComponent<GeoBlast>();
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
        }
    }

    void Update()
    {
        if (_ownerGeoBlast == null)
            return;
        lifestealPerHit = _ownerGeoBlast.getGeoBlastDmg() / 5;
    }

    void OnCollisionEnter(Collision something)
    {
        if (_ownerGeoBlast == null || _ownerPlayer == null)
            return;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(_ownerGeoBlast.getGeoBlastDmg());
            Destroy(this.gameObject);
            _ownerPlayer.changeCurrentHealth(lifestealPerHit);
        }
        else if (something.gameObject.tag == "Enemy")
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(_ownerGeoBlast.getGeoBlastDmg());
            Destroy(this.gameObject);
        }

    }
}
