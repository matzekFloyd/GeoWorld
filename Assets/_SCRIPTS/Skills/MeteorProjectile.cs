using System.Collections;
using UnityEngine;

public class MeteorProjectile : Meteor {

    public Transform explosionPrefab;
    public float explosionRange;

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
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(10f));
        if (player != null)
            _ownerPlayer = player.GetComponent<PlayerCharacter>();
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
                    Instantiate(explosionPrefab, pos, rot);
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

                    colliders[i].gameObject.GetComponent<EnemyAI>().getDamaged(levelDamageScale * distanceMultiplier);

                    if (colliders[i].transform.position != pos)
                    {
                        Vector3 directionFromCenter = (colliders[i].transform.position - pos).normalized;
                        colliders[i].GetComponent<Rigidbody>().AddForce(directionFromCenter * 100 * distanceMultiplier, ForceMode.Impulse);
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
