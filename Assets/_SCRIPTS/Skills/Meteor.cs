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
        if (m_Player == null) return;
        maxCooldown = 40f / m_Player.getCurLevel();
        manacost = m_Player.getCurLevel() * 12.5f;
        meteorDamage = m_Player.getCurLevel() * 50f;
        updateCoolDown();

        if (m_Player.skillAvailable(4))
        {
            if (GameInput.SkillMeteorUp && requiredMana() && CanUseSkills())
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
        m_Player.curMana -= manacost;
        GameplaySfx.Instance?.PlayMeteorCast();
        var pools = GeoWorldObjectPools.Instance;
        GameObject shot = pools != null
            ? pools.Acquire(meteor, camPos.position + camPos.forward * 5, camPos.rotation, null)
            : Instantiate(meteor, camPos.position + camPos.forward * 5, camPos.rotation);

        shot.GetComponent<Rigidbody>().AddForce(camPos.forward * 50, ForceMode.Impulse);
    }

    public float getMeteorDamage()
    {
        return meteorDamage;
    }
}