using System.Collections;
using UnityEngine;

public class GeoBlastProjectile : GeoBlast
{
    private float lifestealPerHit;

    GeoBlast _ownerGeoBlast;
    PlayerCharacter _ownerPlayer;
    bool _hit;
    Coroutine _lifetimeRoutine;

    void OnEnable()
    {
        _hit = false;
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(0.5f));
        if (player != null)
        {
            _ownerGeoBlast = player.GetComponent<GeoBlast>();
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
        }
    }

    void OnDisable()
    {
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }
    }

    IEnumerator LifetimeThenRelease(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _lifetimeRoutine = null;
        if (!_hit)
        {
            _hit = true;
            GeoWorldObjectPools.Release(gameObject);
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
        if (_hit || _ownerGeoBlast == null || _ownerPlayer == null)
            return;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(_ownerGeoBlast.getGeoBlastDmg(), transform.position, CombatHitSeverity.Light);
            _hit = true;
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
                _lifetimeRoutine = null;
            }
            GeoWorldObjectPools.Release(gameObject);
            _ownerPlayer.changeCurrentHealth(lifestealPerHit);
        }
        else if (something.gameObject.tag == "Enemy")
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(_ownerGeoBlast.getGeoBlastDmg(), transform.position, CombatHitSeverity.Light);
            _hit = true;
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
                _lifetimeRoutine = null;
            }
            GeoWorldObjectPools.Release(gameObject);
        }

    }
}
