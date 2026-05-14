using System.Collections;
using UnityEngine;

public class MeteorProjectile : Meteor {

    public Transform explosionPrefab;
    public float explosionRange;

    PlayerCharacter _ownerPlayer;
    Meteor _ownerMeteor;
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
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(10f));
        if (player != null)
        {
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
            _ownerMeteor = player.GetComponent<Meteor>();
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

    void Update () {
        if (_ownerPlayer == null)
            return;
        explosionRange = _ownerPlayer.getCurLevel() * 2f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hit || _ownerPlayer == null)
            return;

        if (collision.gameObject.tag == "Player1")
        {

        }
        else
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;
            if (explosionPrefab != null)
            {
                var pools = GeoWorldObjectPools.Instance;
                var fxRoot = explosionPrefab.gameObject;
                if (pools != null)
                    pools.Acquire(fxRoot, pos, rot, null);
                else
                {
                    var fx = Instantiate(explosionPrefab, pos, rot);
                    PooledVfxSpawnReset.Apply(fx.gameObject);
                }
            }

            Collider[] colliders;
            colliders = Physics.OverlapSphere(pos, explosionRange);

            float levelDamageScale = _ownerPlayer.getCurLevel() * 100f;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.tag == "Enemy")
                {
                    float distanceFromCenter = (colliders[i].transform.position - pos).magnitude;
                    float distanceRatio = distanceFromCenter / explosionRange;
                    float distanceMultiplier = distanceRatio * 0.75f + 0.25f;

                    float dmg = levelDamageScale * distanceMultiplier;
                    bool crit = _ownerPlayer != null && _ownerMeteor != null &&
                        PlayerCritHelper.TryApplyGeoManiaCrit(_ownerPlayer, ref dmg, _ownerMeteor.manacost, _ownerMeteor.maxCooldown);

                    var enemyAi = colliders[i].gameObject.GetComponent<EnemyAI>();
                    if (enemyAi != null)
                        enemyAi.getDamaged(dmg, pos, CombatHitSeverity.Heavy, crit);
                    else
                    {
                        var greaterAi = colliders[i].gameObject.GetComponent<GreaterEnemyAI>();
                        if (greaterAi != null)
                            greaterAi.getDamaged(dmg, pos, CombatHitSeverity.Heavy, crit);
                    }

                    if (colliders[i].transform.position != pos)
                    {
                        var rb = colliders[i].GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 directionFromCenter = (colliders[i].transform.position - pos).normalized;
                            rb.AddForce(directionFromCenter * 100 * distanceMultiplier, ForceMode.Impulse);
                        }
                    }
                }

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
