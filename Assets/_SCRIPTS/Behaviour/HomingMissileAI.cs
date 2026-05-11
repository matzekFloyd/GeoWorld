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
        smallMissileDmg = pc.getCurLevel() * 30;
        bigMissileDmg = pc.getCurLevel() * 100;
        charge();
    }

    private void charge()
    {
            Debug.DrawLine(target.transform.position, transform.position, Color.blue);

            //Schaue dein Ziel an
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.transform.position - transform.position), rotationSpeed * Time.deltaTime);

            //Bewege dich auf dein Ziel zu

            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(target.transform.position, transform.position) < dmgDistance)
            {
                if (this.gameObject.tag == "SmallHomingMissile") target.GetComponent<PlayerCharacter>().changeCurrentHealth(-smallMissileDmg);
                if (this.gameObject.tag == "BigHomingMissile") target.GetComponent<PlayerCharacter>().changeCurrentHealth(-bigMissileDmg);
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
