using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public class GeoPhysics : SkillBasic {

    // Tuned movement / jump / gravity (applied while GeoPhysics skill is active).
    const float WalkSpeedBase = 10f;
    const float WalkSpeedPerLevel = 1.02f;
    const float WalkSpeedMax = 31f;

    const float JumpSpeedAtLevel1 = 11f;
    const float JumpSpeedPerLevelAfter1 = 1.02f;
    const float JumpSpeedMax = 46f;

    const float GravityAtLowLevel = 2f;
    const float GravityReductionPerLevel = 0.125f;
    const float GravityMultiplierMin = 0.75f;

    private int curPlayerLevel;

    FirstPersonController _firstPerson;

    void Start () {
        if (player != null)
            _firstPerson = player.GetComponent<FirstPersonController>();
    }

    void Update () {
        if (m_Player == null || _firstPerson == null)
            return;

        curPlayerLevel = m_Player.getCurLevel();

        if (m_Player.skillAvailable(1))
        {
            enhanceCharacterStatistics();
        }
    }

    public void enhanceCharacterStatistics()
    {
        if (_firstPerson == null || m_Player == null)
            return;

        _firstPerson.m_WalkSpeed = calculateMovementSpeedBuff(curPlayerLevel);
        _firstPerson.m_JumpSpeed = calculateJumpSpeedBuff(curPlayerLevel);
        _firstPerson.m_GravityMultiplier = calculateGravityMultiplier(curPlayerLevel);
    }



    public float calculateMovementSpeedBuff(int playerLevel)
    {
        return Mathf.Min(WalkSpeedBase + playerLevel * WalkSpeedPerLevel, WalkSpeedMax);
    }

    public float calculateJumpSpeedBuff(int playerLevel)
    {
        int lv = Mathf.Max(1, playerLevel);
        float fromOne = JumpSpeedAtLevel1 + (lv - 1) * JumpSpeedPerLevelAfter1;
        return Mathf.Min(fromOne, JumpSpeedMax);
    }

    public float calculateGravityMultiplier(int playerLevel)
    {
        return Mathf.Max(GravityMultiplierMin, GravityAtLowLevel - playerLevel * GravityReductionPerLevel);
    }

    public float getGeoPhysicsHealthReg()
    {
        return m_Player != null ? m_Player.GetPassiveHealthRegenPerSecond() : 0f;
    }
}
