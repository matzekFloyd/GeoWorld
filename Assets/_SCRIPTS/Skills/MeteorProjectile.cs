using System.Collections;
using UnityEngine;

public class MeteorProjectile : Meteor {

    public Transform explosionPrefab;
    public float explosionRange;

    [Header("Explosion VFX vs gameplay radius")]
    [Tooltip(
        "Gameplay explosion radius at which this explosion prefab at localScale 1 matches authored art. " +
        "Lower = larger VFX. Final scale = (explosionRange ÷ this) × Global scale (matches OverlapSphere radius).")]
    [SerializeField] float explosionVfxReferenceRange = 2f;

    [Tooltip("Extra multiplier on explosion root scale (readability vs particle cost).")]
    [SerializeField] float explosionVfxGlobalScaleMultiplier = 1.5f;

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

    void Update()
    {
        if (_ownerPlayer == null)
            return;
        explosionRange = ComputeExplosionRangeForLevel(_ownerPlayer.getCurLevel());
    }

    static float ComputeExplosionRangeForLevel(int level)
    {
        return Mathf.Max(0.1f, level * 2f);
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
            float range = _ownerPlayer != null
                ? ComputeExplosionRangeForLevel(_ownerPlayer.getCurLevel())
                : Mathf.Max(0.1f, explosionRange);
            if (explosionPrefab != null)
            {
                var pools = GeoWorldObjectPools.Instance;
                var fxRoot = explosionPrefab.gameObject;
                Transform fxInst = null;
                if (pools != null)
                {
                    var fxGo = pools.Acquire(fxRoot, pos, rot, null);
                    if (fxGo != null)
                        fxInst = fxGo.transform;
                }
                else
                {
                    fxInst = Instantiate(explosionPrefab, pos, rot);
                    if (fxInst != null)
                        PooledVfxSpawnReset.Apply(fxInst.gameObject);
                }

                ApplyExplosionVisualScale(fxInst, range);
            }

            Collider[] colliders;
            colliders = Physics.OverlapSphere(pos, range);

            float levelDamageScale = _ownerPlayer.getCurLevel() * 100f;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.tag == "Enemy")
                {
                    float distanceFromCenter = (colliders[i].transform.position - pos).magnitude;
                    float distanceRatio = distanceFromCenter / range;
                    float distanceMultiplier = distanceRatio * 0.75f + 0.25f;

                    float dmg = levelDamageScale * distanceMultiplier;
                    if (_ownerPlayer != null)
                        dmg = _ownerPlayer.ScaleOutgoingDamage(dmg);
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

    void ApplyExplosionVisualScale(Transform fxRoot, float rangeWorld)
    {
        if (fxRoot == null)
            return;
        float refR = Mathf.Max(0.25f, explosionVfxReferenceRange);
        float g = Mathf.Max(0.05f, explosionVfxGlobalScaleMultiplier);
        float mul = Mathf.Max(0.05f, rangeWorld / refR) * g;
        fxRoot.localScale = Vector3.one * mul;
    }
}
