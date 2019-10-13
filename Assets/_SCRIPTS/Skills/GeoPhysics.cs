using UnityEngine;
using System.Collections;

public class GeoPhysics : SkillBasic {

    private int curPlayerLevel;
    private float geoHealthreg = 0.0f;

    // Use this for initialization
    void Start () {
        player = GameObject.FindGameObjectWithTag("Player1");
    }

    // Update is called once per frame
    void Update () {

        curPlayerLevel = player.GetComponent<PlayerCharacter>().getCurLevel();

        if (player.GetComponent<PlayerCharacter>().skillAvailable(1))
        {
            enhanceCharacterStatistics();
        }
    }

    public void enhanceCharacterStatistics()
    {
        //MOVEMENTSPEED
        player.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().m_WalkSpeed = calculateMovementSpeedBuff(curPlayerLevel);

        //JUMPSPEED
        player.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().m_JumpSpeed = calculateJumpSpeedBuff(curPlayerLevel);

        //GRAVITY
        player.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().m_GravityMultiplier = calculateGravityMultiplier(curPlayerLevel);

        //HP-REGENARATION
        player.GetComponent<PlayerCharacter>().changeCurrentHealth(calculateHealthRegeneration(curPlayerLevel) *Time.deltaTime);              

    }



    public float calculateMovementSpeedBuff(int playerLevel)
    {
        float movementSpeed = 10;

        for (int i = 0; i <= player.GetComponent<PlayerCharacter>().getMaxLevel(); i++)
        {
            if (playerLevel == i) movementSpeed = i + 10;
        }

        if (movementSpeed >= 30) movementSpeed = 30;

        return movementSpeed;
    }

    public float calculateJumpSpeedBuff(int playerLevel)
    {
        float jumpSpeed = 8;

        for(int i = 0; i <= player.GetComponent<PlayerCharacter>().getMaxLevel(); i++)
        {
            if (playerLevel == i) jumpSpeed = i + 10;
        }

        if (jumpSpeed >= 50) jumpSpeed = 50;

        return jumpSpeed;
    }

    public float calculateGravityMultiplier(int playerLevel)
    {
        float gravityMultiplier = 1.85f;

        for (int i = 0; i <= player.GetComponent<PlayerCharacter>().getMaxLevel(); i++)
        {
            if (playerLevel == i) gravityMultiplier = 2f - i * 0.125f;
        }

        if (gravityMultiplier <= 0.75f) gravityMultiplier = 0.75f;

            return gravityMultiplier;
    }

    public float calculateHealthRegeneration(int playerLevel)
    {

        for (int i = 0; i <= player.GetComponent<PlayerCharacter>().getMaxLevel(); i++)
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