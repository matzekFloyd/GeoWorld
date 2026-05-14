using System.Collections;
using UnityEngine;

public class GeoBlastProjectile : GeoBlast
{
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

    void OnCollisionEnter(Collision something)
    {
        if (_hit || _ownerGeoBlast == null || _ownerPlayer == null)
            return;

        float lifestealPerHit = _ownerGeoBlast.getGeoBlastDmg() / 5f;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            var ai = something.gameObject.GetComponent<EnemyAI>();
            if (ai != null)
            {
                float d = _ownerGeoBlast.getGeoBlastDmg();
                bool crit = PlayerCritHelper.TryApplyGeoManiaCrit(_ownerPlayer, ref d, _ownerGeoBlast.manacost, _ownerGeoBlast.maxCooldown);
                ai.getDamaged(d, transform.position, CombatHitSeverity.Light, crit);
            }
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
            var ai = something.gameObject.GetComponent<EnemyAI>();
            if (ai != null)
            {
                float d = _ownerGeoBlast.getGeoBlastDmg();
                bool crit = PlayerCritHelper.TryApplyGeoManiaCrit(_ownerPlayer, ref d, _ownerGeoBlast.manacost, _ownerGeoBlast.maxCooldown);
                ai.getDamaged(d, transform.position, CombatHitSeverity.Light, crit);
            }
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
