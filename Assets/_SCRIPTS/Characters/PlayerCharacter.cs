using UnityEngine;
using System.Collections;

public class PlayerCharacter : BaseCharacter {

    private GameObject enemy;

    public float curMana;
    public float maxMana;

    public float curExp;
    public float expNeededForLevelUp;

    private GameObject enemyGenerator;

    [Header("Health regen (out of combat)")]
    [Tooltip("Seconds with no incoming damage before passive HP regen can begin.")]
    [SerializeField] float healthRegenLockoutAfterHitSeconds = 3f;

    [Tooltip("After lockout, seconds of not being hit to approach full regen rate.")]
    [SerializeField] float healthRegenRampToFullSeconds = 18f;

    [Tooltip("Minimum HP/s right when lockout ends (before ramp).")]
    [SerializeField] float healthRegenPerSecondFloor = 0.18f;

    [Tooltip("Added to floor/scaling: maxHealth × this per second at full ramp.")]
    [SerializeField] float healthRegenMaxFractionOfMaxPerSecond = 0.0065f;

    [Tooltip("Extra HP/s at full ramp from player level.")]
    [SerializeField] float healthRegenPerSecondPerLevelAtFull = 0.55f;

    /// <summary>Time.time of last enemy/missile damage via <see cref="ApplyIncomingDamage"/>. Negative = never damaged this run.</summary>
    float _lastIncomingDamageTime = -1f;




    // Use this for initialization
    void Start () {

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        if (enemyGenerator == null)
            Debug.LogWarning("GeoWorld: No GameObject with tag 'Spawn' found. Enemy wave scaling (LevelUp → EnemyGenerator) will not work.");
        setInitialPlayerStatistics();
    }

    /// <summary>Enemy/missile damage with world origin for directional HUD feedback.</summary>
    public void ApplyIncomingDamage(float amount, Vector3 worldSource, bool hasWorldSource, CombatHitSeverity severity)
    {
        if (amount <= 0f)
            return;
        ApplyHealthChange(-Mathf.Abs(amount));
        _lastIncomingDamageTime = Time.time;
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
        maxHealth = 110;
        curHealth = maxHealth;
        baseHealthRegeneration = 0f;
        maxMana = 75;
        curMana = maxMana;

    }
	
	// Update is called once per frame
	void Update () {

        changeCurrentHealth(0);
        changeCurrentMana(calculateManaRegeneration(curLevel) * Time.deltaTime);

        regnerateHealth(GetPassiveHealthRegenPerSecond() * Time.deltaTime);

        //FÜR TESTZWECKE
        if (GameInput.DebugInstantLevelUpUp && GameSession.Instance != null && GameSession.Instance.IsRunActive)
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

            maxHealth += Mathf.RoundToInt(45f + curLevel * 22f);
            curHealth = maxHealth;

            maxMana += Mathf.RoundToInt(18f + curLevel * 12f);
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
        // Stronger per-level growth than the post-nerf linear-only curve; mild L² so high levels keep pace with skill costs.
        return 0.5f + curLevel * 0.7f + curLevel * curLevel * 0.005f;
    }

    /// <summary>Passive HP/s from staying safe (0 during post-hit lockout, then ramps up).</summary>
    public float GetPassiveHealthRegenPerSecond()
    {
        float now = Time.time;
        if (_lastIncomingDamageTime < 0f)
            return GetPeakPassiveHealthRegenPerSecond();

        float sinceHit = now - _lastIncomingDamageTime;
        if (sinceHit < healthRegenLockoutAfterHitSeconds)
            return 0f;

        float rampT = sinceHit - healthRegenLockoutAfterHitSeconds;
        float u = healthRegenRampToFullSeconds > 0.001f
            ? Mathf.Clamp01(rampT / healthRegenRampToFullSeconds)
            : 1f;
        u = u * u * (3f - 2f * u);
        float minR = healthRegenPerSecondFloor + maxHealth * 0.00035f;
        float maxR = GetPeakPassiveHealthRegenPerSecond();
        return Mathf.Lerp(minR, maxR, u);
    }

    /// <summary>HP/s when fully ramped; used for GeoPhysics HUD and internal cap.</summary>
    public float GetPeakPassiveHealthRegenPerSecond()
    {
        return maxHealth * healthRegenMaxFractionOfMaxPerSecond
               + curLevel * healthRegenPerSecondPerLevelAtFull;
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




