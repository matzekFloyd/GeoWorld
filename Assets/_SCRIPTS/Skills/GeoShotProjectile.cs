using System.Collections;
using UnityEngine;

public class GeoShotProjectile : GeoShot{

    private float lifestealPerHit;
    private float manaGainPerHit;
    private float damagePerHit;

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

    void Update () {
        if (_ownerGeoShot == null || _ownerPlayer == null)
            return;
        lifestealPerHit = _ownerGeoShot.getGeoShotDmg() / 10;
        manaGainPerHit = _ownerGeoShot.manacost / 6;
        damagePerHit = _ownerGeoShot.getGeoShotDmg();
    }

    void OnCollisionEnter(Collision something)
    {
        if (_hit || _ownerPlayer == null)
            return;

        if (something.gameObject.tag == "Enemy" && geoManiaActivated())
        {
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
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
            something.gameObject.GetComponent<EnemyAI>().getDamaged(damagePerHit);
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
