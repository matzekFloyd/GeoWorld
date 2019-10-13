using UnityEngine;
using System.Collections;

public class HomingMissileAI : MonoBehaviour {

    public GameObject target;
    public float moveSpeed;
    public float rotationSpeed;
    public float dmgDistance;

    public float smallMissileDmg;
    public float bigMissileDmg;

    // Use this for initialization
    void Start () {
        target = GameObject.FindGameObjectWithTag("Player1");

        dmgDistance = 5;

        Destroy(this.gameObject, 7.5f);

        moveSpeed = 50;
        rotationSpeed = 10;
    }

    // Update is called once per frame
    void Update () {
        smallMissileDmg = target.GetComponent<PlayerCharacter>().getCurLevel() * 30;
        bigMissileDmg = target.GetComponent<PlayerCharacter>().getCurLevel() * 100;
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
                Destroy(this.gameObject);

            }

            if (target.GetComponent<PlayerCharacter>().iAmDead())
            {
                Destroy(this.gameObject);
            }
        
    }
}
