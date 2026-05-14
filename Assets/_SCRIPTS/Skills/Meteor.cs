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
        manacost = 28f + m_Player.getCurLevel() * 26f;
        meteorDamage = m_Player.getCurLevel() * 50f;
        updateCoolDown();

        if (m_Player.skillAvailable(4))
        {
            if (GameInput.SkillMeteorHeld && requiredMana() && CanUseSkills())
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
        m_Player.changeCurrentMana(-manacost);
        GameplaySfx.Instance?.PlayMeteorCast();
        var pools = GeoWorldObjectPools.Instance;
        GameObject shot = pools != null
            ? pools.Acquire(meteor, camPos.position + camPos.forward * 5, camPos.rotation, null)
            : Instantiate(meteor, camPos.position + camPos.forward * 5, camPos.rotation);

        GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(shot);
        var rb = shot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Impulse Δv = J/m — scale J with m so heavier meteors keep similar launch speed after pool mass tuning.
            const float forwardImpulsePerUnitMass = 50f;
            rb.AddForce(camPos.forward * (forwardImpulsePerUnitMass * rb.mass), ForceMode.Impulse);
        }
    }

    public float getMeteorDamage()
    {
        return meteorDamage;
    }
}