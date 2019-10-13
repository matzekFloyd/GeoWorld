using UnityEngine;
using System.Collections;

public class MeteorProjectile : Meteor {

    public Transform explosionPrefab;
    public float explosionRange;

    // Use this for initialization
    void Start () {
        Destroy(this.gameObject, 10f);
    }

    // Update is called once per frame
    void Update () {
        explosionRange = player.GetComponent<PlayerCharacter>().getCurLevel() * 2f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player1")
        {

        }
        else
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;
            Instantiate(explosionPrefab, pos, rot);

            Collider[] colliders;
            colliders = Physics.OverlapSphere(pos, explosionRange);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.tag == "Enemy")
                {

                    float distanceFromCenter = (colliders[i].transform.position - pos).magnitude;
                    float distanceRatio = distanceFromCenter / explosionRange;
                    float distanceMultiplier = distanceRatio * 0.75f + 0.25f;

                    colliders[i].gameObject.GetComponent<EnemyAI>().getDamaged(player.GetComponent<PlayerCharacter>().getCurLevel() * 100 * distanceMultiplier);

                    if (colliders[i].transform.position != pos)
                    {
                        Vector3 directionFromCenter = (colliders[i].transform.position - pos).normalized;
                        colliders[i].GetComponent<Rigidbody>().AddForce(directionFromCenter * 100 * distanceMultiplier, ForceMode.Impulse);
                    }
                }

            }
            Destroy(gameObject);
        }
    }
}