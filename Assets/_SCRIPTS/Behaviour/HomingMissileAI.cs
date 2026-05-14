using System.Collections;
using UnityEngine;

public class HomingMissileAI : MonoBehaviour {

    public GameObject target;
    public float moveSpeed;
    public float rotationSpeed;
    public float dmgDistance;

    public float smallMissileDmg;
    public float bigMissileDmg;

    Coroutine _lifetimeRoutine;

    void OnEnable () {
        target = GameObject.FindGameObjectWithTag("Player1");

        dmgDistance = 5;

        moveSpeed = 50;
        rotationSpeed = 10;

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
        _lifetimeRoutine = StartCoroutine(LifetimeThenRelease(7.5f));
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
        GeoWorldObjectPools.Release(gameObject);
    }

    // Update is called once per frame
    void Update () {
        if (target == null)
            return;
        var pc = target.GetComponent<PlayerCharacter>();
        if (pc == null)
            return;
        int lv = Mathf.Max(1, pc.getCurLevel());

        smallMissileDmg = pc.getCurLevel() * 22f;
        bigMissileDmg = pc.getCurLevel() * 72f;

        rotationSpeed = Mathf.Clamp(9.5f + lv * 0.38f, 9.5f, 22f);
        moveSpeed = Mathf.Clamp(40f + lv * 1.15f, 40f, 82f);
        charge(pc);
    }

    private void charge(PlayerCharacter pc)
    {
            Debug.DrawLine(target.transform.position, transform.position, Color.blue);

            //Schaue dein Ziel an
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.transform.position - transform.position), rotationSpeed * Time.deltaTime);

            //Bewege dich auf dein Ziel zu

            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(target.transform.position, transform.position) < dmgDistance)
            {
                if (gameObject.CompareTag("SmallHomingMissile"))
                    pc.ApplyIncomingDamage(smallMissileDmg, transform.position, true, CombatHitSeverity.Medium, true, false);
                else if (gameObject.CompareTag("BigHomingMissile"))
                    pc.ApplyIncomingDamage(bigMissileDmg, transform.position, true, CombatHitSeverity.Heavy, true, true);
                if (_lifetimeRoutine != null)
                {
                    StopCoroutine(_lifetimeRoutine);
                    _lifetimeRoutine = null;
                }
                GeoWorldObjectPools.Release(gameObject);

            }

            if (target.GetComponent<PlayerCharacter>().iAmDead())
            {
                if (_lifetimeRoutine != null)
                {
                    StopCoroutine(_lifetimeRoutine);
                    _lifetimeRoutine = null;
                }
                GeoWorldObjectPools.Release(gameObject);
            }
        
    }
}
