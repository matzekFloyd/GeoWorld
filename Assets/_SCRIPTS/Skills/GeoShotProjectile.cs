using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoShotProjectile : GeoShot
{
    /// <summary>Pooled projectile must not run <see cref="GeoShot.Update"/> (fire input, <see cref="GeoShot.camPos"/>).</summary>
    void Update() { }

    GeoShot _ownerGeoShot;
    PlayerCharacter _ownerPlayer;
    bool _mania;
    int _bounceCount;
    int _maxBounces;
    float _speedRetention = 0.9f;
    Coroutine _lifetimeRoutine;
    /// <summary>Enemy roots already damaged by this projectile.</summary>
    readonly HashSet<EntityId> _damagedEnemyRootIds = new HashSet<EntityId>();

    static PhysicsMaterial s_bounceMaterial;

    void OnEnable()
    {
        _bounceCount = 0;
        _damagedEnemyRootIds.Clear();
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }

        if (player != null)
        {
            _ownerGeoShot = player.GetComponent<GeoShot>();
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
        }

        ApplyBounceMaterial();
    }

    void OnDisable()
    {
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }
    }

    public void Configure(GeoShot owner, int playerLevel, bool mania)
    {
        _ownerGeoShot = owner;
        _mania = mania;
        if (_ownerGeoShot == null)
            return;

        _maxBounces = _ownerGeoShot.GetMaxBounces(playerLevel, mania);
        _speedRetention = _ownerGeoShot.BounceSpeedRetention;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(_ownerGeoShot.GetLifetimeSeconds(playerLevel, mania)));
    }

    void ApplyBounceMaterial()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            return;

        if (s_bounceMaterial == null)
        {
            s_bounceMaterial = new PhysicsMaterial("GeoShotBounce")
            {
                bounciness = 0.88f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.04f,
                staticFriction = 0.04f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
        }

        col.material = s_bounceMaterial;
    }

    IEnumerator LifetimeThenRelease(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _lifetimeRoutine = null;
        Despawn();
    }

    void Despawn()
    {
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }
        GeoWorldObjectPools.Release(gameObject);
    }

    void OnCollisionEnter(Collision collision) => HandleCollision(collision, countEnvironmentBounce: true);

    /// <summary>Backup for fast shots that tunnel through thin colliders between physics steps.</summary>
    void OnCollisionStay(Collision collision) => HandleCollision(collision, countEnvironmentBounce: false);

    void HandleCollision(Collision collision, bool countEnvironmentBounce)
    {
        if (_ownerPlayer == null || _ownerGeoShot == null)
            return;

        if (TryDamageEnemy(collision))
        {
            if (countEnvironmentBounce)
                Reflect(collision);
            return;
        }

        if (!countEnvironmentBounce)
            return;

        if (IsEnvironmentCollision(collision))
        {
            _bounceCount++;
            Reflect(collision);
            if (_bounceCount >= _maxBounces)
                Despawn();
        }
    }

    static bool IsEnvironmentCollision(Collision collision)
    {
        if (TryResolveEnemyFromCollider(collision.collider, out _, out _, out _))
            return false;
        var go = collision.gameObject;
        if (go.CompareTag("Player1"))
            return false;
        return true;
    }

    /// <summary>
    /// Enemy meshes often use child colliders without the Enemy tag or AI on the same GameObject.
    /// Walk parents and support <see cref="GreaterEnemyAI"/> like <see cref="MeteorProjectile"/>.
    /// </summary>
    static bool TryResolveEnemyFromCollider(Component hit, out GameObject enemyRoot, out EnemyAI enemyAi, out GreaterEnemyAI greaterAi)
    {
        enemyRoot = null;
        enemyAi = null;
        greaterAi = null;
        if (hit == null)
            return false;

        enemyAi = hit.GetComponent<EnemyAI>() ?? hit.GetComponentInParent<EnemyAI>();
        greaterAi = hit.GetComponent<GreaterEnemyAI>() ?? hit.GetComponentInParent<GreaterEnemyAI>();

        Transform t = hit.transform;
        while (t != null)
        {
            if (t.CompareTag("Enemy"))
            {
                enemyRoot = t.gameObject;
                if (enemyAi == null)
                    enemyAi = enemyRoot.GetComponent<EnemyAI>();
                if (greaterAi == null)
                    greaterAi = enemyRoot.GetComponent<GreaterEnemyAI>();
                return enemyAi != null || greaterAi != null;
            }
            t = t.parent;
        }

        if (enemyAi != null)
        {
            enemyRoot = enemyAi.gameObject;
            return true;
        }

        if (greaterAi != null)
        {
            enemyRoot = greaterAi.gameObject;
            return true;
        }

        return false;
    }

    bool TryDamageEnemy(Collision collision)
    {
        if (!TryResolveEnemyFromCollider(collision.collider, out var enemyRoot, out var enemyAi, out var greaterAi))
            return false;

        EntityId rootId = enemyRoot.GetEntityId();
        if (_damagedEnemyRootIds.Contains(rootId))
            return false;

        _damagedEnemyRootIds.Add(rootId);

        float damagePerHit = _ownerGeoShot.getGeoShotDmg();
        float d = damagePerHit;
        bool crit = PlayerCritHelper.TryApplyGeoManiaCrit(
            _ownerPlayer, ref d, _ownerGeoShot.manacost, _ownerGeoShot.maxCooldown);

        var hitPos = transform.position;
        if (enemyAi != null)
            enemyAi.getDamaged(d, hitPos, CombatHitSeverity.Light, crit);
        else
            greaterAi.getDamaged(d, hitPos, CombatHitSeverity.Light, crit);

        if (_mania)
        {
            _ownerPlayer.changeCurrentHealth(damagePerHit / 10f);
            _ownerPlayer.changeCurrentMana(_ownerGeoShot.manacost / 6f);
        }

        return true;
    }

    void Reflect(Collision collision)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null || collision.contactCount == 0)
            return;

        Vector3 n = collision.GetContact(0).normal;
        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, n) * _speedRetention;
    }
}
