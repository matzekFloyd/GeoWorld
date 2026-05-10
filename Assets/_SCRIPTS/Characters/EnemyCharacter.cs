using UnityEngine;
using System.Collections;

public class EnemyCharacter : BaseCharacter {
    
    private GameObject player;
    public float expOnKill;
    public Color originalColor;
    public bool iAmGreaterEnemy;

    [Tooltip("Enable on boss prefabs for extra health/EXP and scoreboard bonus on death.")]
    public bool isBoss;
    
    // Use this for initialization
    void Start () {

        originalColor = this.gameObject.GetComponent<Renderer>().material.color;
        player = GameObject.FindGameObjectWithTag("Player1");

        setEnemyStatistics(player.GetComponent<PlayerCharacter>().getCurLevel(), player.GetComponent<PlayerCharacter>().getExpNeededForLevelUp());
    }

    // Update is called once per frame
    void Update () {
        changeCurrentHealth(0);
    }

    public void setEnemyStatistics(float curPlayerLevel, float expNeededForLevelUp)
    {
        iAmGreaterEnemy = this.gameObject.GetComponent<GreaterEnemyAI>() != null;

        if (iAmGreaterEnemy)
        {
            maxHealth = curPlayerLevel * 250 * Random.Range(20f, 30f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 300;
        }
        else
        {
            maxHealth = curPlayerLevel * 25 * Random.Range(2f, 3f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 15;
        }

        if (isBoss)
        {
            maxHealth *= GameBalanceHelper.BossHealthMultiplier;
            curHealth = maxHealth;
            expOnKill *= GameBalanceHelper.BossExpMultiplier;
        }

        //Bewegungsgeschwindigkeit der Gegner
        EnemyAI enemyAI = this.gameObject.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.moveSpeed = Random.Range(7.5f, 20f);
            enemyAI.rotationSpeed = Random.Range(5f, 15f);
            enemyAI.damage = Random.Range(player.GetComponent<PlayerCharacter>().getCurLevel() * 7.5f, player.GetComponent<PlayerCharacter>().getCurLevel() * 12.5f);
        }

    }

    public float getExpOnKill()
    {
        return expOnKill;
    }

}