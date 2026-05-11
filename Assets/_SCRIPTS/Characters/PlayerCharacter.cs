using UnityEngine;
using System.Collections;

public class PlayerCharacter : BaseCharacter {

    private GameObject enemy;

    public float curMana;
    public float maxMana;

    public float curExp;
    public float expNeededForLevelUp;

    private GameObject enemyGenerator;
    private GameOver m_GameOver;




    // Use this for initialization
    void Start () {

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        if (enemyGenerator == null)
            Debug.LogWarning("GeoWorld: No GameObject with tag 'Spawn' found. Enemy wave scaling (LevelUp → EnemyGenerator) will not work.");
        m_GameOver = GetComponent<GameOver>();
        setInitialPlayerStatistics();
    }

    /// <summary>Enemy/missile damage with world origin for directional HUD feedback.</summary>
    public void ApplyIncomingDamage(float amount, Vector3 worldSource, bool hasWorldSource, CombatHitSeverity severity)
    {
        if (amount <= 0f)
            return;
        ApplyHealthChange(-Mathf.Abs(amount));
        var fx = CombatFeedback.Instance;
        if (fx != null)
            fx.NotifyPlayerDamaged(amount, worldSource, hasWorldSource, severity);
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
        if (GameInput.DebugInstantLevelUpUp && m_GameOver != null && !m_GameOver.playerDied && !m_GameOver.gameTimeIsOver)
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

            expNeededForLevelUp = curLevel * curLevel * 75;
                                
        }

        //Wenn der Spieler ein Level aufsteigt -> passe die Werte aller Gegner an
        EnemyGenerator gen = enemyGenerator.GetComponent<EnemyGenerator>();
        if (gen == null) return;

        for (int i = 0; i < gen.targets.Count; i++)
        {
            gen.targets[i].GetComponent<EnemyCharacter>().setEnemyStatistics(this.curLevel, this.expNeededForLevelUp);
        }
        gen.state = EnemyGenerator.State.SpawnEnemy;

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




