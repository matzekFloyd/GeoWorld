using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public class GeoPhysics : SkillBasic {

    private int curPlayerLevel;
    private float geoHealthreg = 0.0f;

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

        m_Player.changeCurrentHealth(calculateHealthRegeneration(curPlayerLevel) * Time.deltaTime);
    }



    public float calculateMovementSpeedBuff(int playerLevel)
    {
        float movementSpeed = 10;
        int maxLevel = m_Player.getMaxLevel();

        for (int i = 0; i <= maxLevel; i++)
        {
            if (playerLevel == i) movementSpeed = i + 10;
        }

        if (movementSpeed >= 30) movementSpeed = 30;

        return movementSpeed;
    }

    public float calculateJumpSpeedBuff(int playerLevel)
    {
        float jumpSpeed = 8;
        int maxLevel = m_Player.getMaxLevel();

        for(int i = 0; i <= maxLevel; i++)
        {
            if (playerLevel == i) jumpSpeed = i + 10;
        }

        if (jumpSpeed >= 50) jumpSpeed = 50;

        return jumpSpeed;
    }

    public float calculateGravityMultiplier(int playerLevel)
    {
        float gravityMultiplier = 1.85f;
        int maxLevel = m_Player.getMaxLevel();

        for (int i = 0; i <= maxLevel; i++)
        {
            if (playerLevel == i) gravityMultiplier = 2f - i * 0.125f;
        }

        if (gravityMultiplier <= 0.75f) gravityMultiplier = 0.75f;

            return gravityMultiplier;
    }

    public float calculateHealthRegeneration(int playerLevel)
    {
        int maxLevel = m_Player.getMaxLevel();

        for (int i = 0; i <= maxLevel; i++)
        {
            if (playerLevel == i) geoHealthreg = i * 0.5f;
        }

        return geoHealthreg;
    }

    public float getGeoPhysicsHealthReg()
    {
        return geoHealthreg;
    }
}
