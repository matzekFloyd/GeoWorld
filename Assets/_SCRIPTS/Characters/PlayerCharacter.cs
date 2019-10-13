using UnityEngine;
using System.Collections;

public class PlayerCharacter : BaseCharacter {

    private GameObject enemy;

    public float curMana;
    public float maxMana;

    public float curExp;
    public float expNeededForLevelUp;

    private GameObject enemyGenerator;




    // Use this for initialization
    void Start () {

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        setInitialPlayerStatistics();
    }

    private void setInitialPlayerStatistics()
    {
        curExp = 0;
        curLevel = 1;
        maxLevel = 50;
        expNeededForLevelUp = 100;
        maxHealth = 100;
        curHealth = maxHealth;
        baseHealthRegeneration = 0.5f;
        maxMana = 100;
        curMana = maxMana;

    }
	
	// Update is called once per frame
	void Update () {

        changeCurrentHealth(0);
        changeCurrentMana(calculateManaRegeneration(curLevel) * Time.deltaTime);

        if(curLevel < 5)
        {
            regnerateHealth(baseHealthRegeneration * Time.deltaTime);
        }

        //FÜR TESTZWECKE
        if (Input.GetKeyUp(KeyCode.T) && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
        {
                LevelUp();
        }
    }

    public void AddExp(float expValue)
    {
        curExp += expValue;

        CalculateLevel();
    }

    public void CalculateLevel()
    {
        if(curExp >= expNeededForLevelUp) 
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        if(curLevel < maxLevel)
        {
            curLevel += 1;

            maxHealth += curLevel * 100;
            curHealth = maxHealth;

            maxMana += curLevel * 25;
            curMana = maxMana;

            curExp = 0;

            for (int i = 0; i <= maxLevel; i++)
            {
                if (curLevel == i) expNeededForLevelUp = i*i*75;
            }
                                
        }

        //Wenn der Spieler ein Level aufsteigt -> passe die Werte aller Gegner an
        for (int i = 0; i < enemyGenerator.GetComponent<EnemyGenerator>().targets.Count; i++)
        {
            enemyGenerator.GetComponent<EnemyGenerator>().targets[i].GetComponent<EnemyCharacter>().setEnemyStatistics(this.curLevel, this.expNeededForLevelUp);
        }
        enemyGenerator.GetComponent<EnemyGenerator>().state = EnemyGenerator.State.SpawnEnemy;

     }

    public void regnerateHealth(float valueHealthRegenaration)
    {
        changeCurrentHealth(valueHealthRegenaration);
    }

    public void changeCurrentMana(float change)
    {
        curMana += change;

        if (curMana < 0)
            curMana = 0;

        if (curMana == 0)
        {

        }

        if (curMana > maxMana)
            curMana = maxMana;

        if (maxMana < 1)
            maxMana = 1;


    }

    public float calculateManaRegeneration(int curLevel)
    {
        float manaReg;
        manaReg = curLevel * 1.25f;
        return manaReg;
    }

    public bool skillAvailable(int levelNeeded)
    {
        return curLevel >= levelNeeded;
    }

    //GETTER + SETTER METHODEN
    public float getCurMana()
    {
        return curMana;
    }

    public float getMaxMana()
    {
        return maxMana;
    }

    public float getCurExp()
    {
        return curExp;
    }

    public float getExpNeededForLevelUp()
    {
        return expNeededForLevelUp;
    }
}




