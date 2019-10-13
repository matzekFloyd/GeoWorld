using UnityEngine;
using System.Collections;

public class Meteor : SkillBasic {

    public GameObject meteor;
    public Transform camPos;

    protected float meteorDamage;

    // Use this for initialization
    void Start()
    {
        curCooldown = 0;
    }

    // Update is called once per frame
    void Update()
    {
        maxCooldown = 40f / player.GetComponent<PlayerCharacter>().getCurLevel();
        manacost = player.GetComponent<PlayerCharacter>().getCurLevel() * 12.5f;
        meteorDamage = player.GetComponent<PlayerCharacter>().getCurLevel() * 50f;
        updateCoolDown();

        if (player.GetComponent<PlayerCharacter>().skillAvailable(4))
        {
            if (Input.GetKeyUp(KeyCode.E) && requiredMana() && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
            {
                if (curCooldown == 0)
                {
                    shootFireBall();
                    curCooldown = maxCooldown;

                }
            }
        }
    }

    public void shootFireBall()
    {
        player.GetComponent<PlayerCharacter>().curMana -= manacost;
        GameObject shot = (GameObject)Instantiate(meteor, camPos.position + camPos.forward * 5, camPos.rotation);

        shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 50, ForceMode.Impulse);
    }

    public float getMeteorDamage()
    {
        return meteorDamage;
    }
}