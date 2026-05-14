using System.Collections;
using UnityEngine;

public class GeoShotProjectile : GeoShot{

    /// <summary>Pooled projectile must not run <see cref="GeoShot.Update"/> (fire input, <see cref="GeoShot.camPos"/>).</summary>
    void Update() { }

    GeoShot _ownerGeoShot;
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
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(2f));
        if (player != null)
        {
            _ownerGeoShot = player.GetComponent<GeoShot>();
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
        Despawn();
    }

    void Despawn()
    {
        if (_hit)
            return;
        _hit = true;
        GeoWorldObjectPools.Release(gameObject);
    }

    void OnCollisionEnter(Collision something)
    {
        if (_hit || _ownerPlayer == null || _ownerGeoShot == null)
            return;

        float damagePerHit = _ownerGeoShot.getGeoShotDmg();
        float lifestealPerHit = damagePerHit / 10f;
        float manaGainPerHit = _ownerGeoShot.manacost / 6f;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            var ai = something.gameObject.GetComponent<EnemyAI>();
            if (ai != null)
            {
                float d = damagePerHit;
                bool crit = PlayerCritHelper.TryApplyGeoManiaCrit(_ownerPlayer, ref d, _ownerGeoShot.manacost, _ownerGeoShot.maxCooldown);
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
            _ownerPlayer.changeCurrentMana(manaGainPerHit);

        }
        else if (something.gameObject.tag == "Enemy")
        {
            var ai = something.gameObject.GetComponent<EnemyAI>();
            if (ai != null)
            {
                float d = damagePerHit;
                bool crit = PlayerCritHelper.TryApplyGeoManiaCrit(_ownerPlayer, ref d, _ownerGeoShot.manacost, _ownerGeoShot.maxCooldown);
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
