using UnityEngine;
using System.Collections;

public class PlayerCharacter : BaseCharacter {

    /// <summary>With Geo Mania (level 10), HP may exceed <see cref="maxHealth"/> up to this multiple (overheal).</summary>
    public const float GeoManiaOverhealMaxMultiplier = 2f;

    /// <summary>Blood Ritual may push <see cref="curMana"/> up to this × <see cref="maxMana"/> (overmana).</summary>
    public const float BloodRitualOvermanaMaxMultiplier = 2f;

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

    [Header("Mana regen (idle ramp)")]
    [Tooltip("Seconds without spending mana to reach maximum mana regen multiplier.")]
    [SerializeField] float manaRegenRampIdleSeconds = 5f;

    [Tooltip("Mana regen multiplier at full idle (1 = no bonus). Applied on top of base regen from level.")]
    [SerializeField] float manaRegenMaxMultiplierAtFullIdle = 2.1f;

    /// <summary>Time.time of last mana spend (negative cost via <see cref="changeCurrentMana"/>). Negative = no spend yet this run.</summary>
    float _lastManaSpendTime = -1f;

    [Header("Overheal decay (Geo Mania)")]
    [Tooltip("HP removed per second only from HP above nominal max; stops when at or below maxHealth.")]
    [SerializeField] float overhealDegenerationPerSecond = 4f;

    [Header("Overmana decay (Blood Ritual)")]
    [Tooltip("Mana removed per second only from mana above nominal max; stops when at or below maxMana.")]
    [SerializeField] float overmanaDegenerationPerSecond = 4f;




    // Use this for initialization
    void Start () {

        if (GetComponent<PlayerFirstPersonRunGate>() == null)
        {
            if (GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>() != null
                || GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>() != null)
                gameObject.AddComponent<PlayerFirstPersonRunGate>();
        }

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        if (enemyGenerator == null)
            Debug.LogWarning("GeoWorld: No GameObject with tag 'Spawn' found. Enemy wave scaling (LevelUp → EnemyGenerator) will not work.");
        setInitialPlayerStatistics();
    }

    /// <summary>Enemy/missile damage with world origin for directional HUD feedback.</summary>
    /// <param name="rollEnemyCrit">When true, may crit if player level ≥ <see cref="EnemyCritHelper.MinPlayerLevelForEnemyCrit"/>.</param>
    /// <param name="enemyAttackerIsBossTier">Higher crit chance (bosses, big missiles, etc.).</param>
    public void ApplyIncomingDamage(float amount, Vector3 worldSource, bool hasWorldSource, CombatHitSeverity severity, bool rollEnemyCrit = false, bool enemyAttackerIsBossTier = false)
    {
        if (amount <= 0f)
            return;
        float dealt = Mathf.Abs(amount);
        bool enemyCrit = rollEnemyCrit && EnemyCritHelper.TryApplyEnemyCritAgainstPlayer(this, ref dealt, enemyAttackerIsBossTier);
        ApplyHealthChange(-dealt);
        _lastIncomingDamageTime = Time.time;
        var fx = CombatFeedback.Instance;
        if (fx != null)
            fx.NotifyPlayerDamaged(dealt, worldSource, hasWorldSource, severity, enemyCrit);
    }

    protected override float GetHealthUpperClamp()
    {
        if (skillAvailable(10))
            return maxHealth * GeoManiaOverhealMaxMultiplier;
        return maxHealth;
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
        float manaRegenBase = calculateManaRegeneration(curLevel);
        changeCurrentMana(manaRegenBase * GetManaRegenIdleMultiplier() * Time.deltaTime);

        regnerateHealth(GetPassiveHealthRegenPerSecond() * Time.deltaTime);

        ApplyOverhealDecay();
        ApplyOvermanaDecay();

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

            float dHealth = GameBalanceHelper.RollLevelUpMaxHealthDelta(curLevel);
            maxHealth += dHealth;
            curHealth += dHealth;
            float hpCap = GetHealthUpperClamp();
            if (curHealth > hpCap)
                curHealth = hpCap;

            float dMana = GameBalanceHelper.RollLevelUpMaxManaDelta(curLevel);
            maxMana += dMana;
            curMana += dMana;
            float manaCap = maxMana * BloodRitualOvermanaMaxMultiplier;
            if (curMana > manaCap)
                curMana = manaCap;

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
        if (valueHealthRegenaration <= 0f)
            return;
        float room = maxHealth - curHealth;
        if (room <= 0f)
            return;
        changeCurrentHealth(Mathf.Min(valueHealthRegenaration, room));
    }

    void ApplyOverhealDecay()
    {
        if (overhealDegenerationPerSecond <= 0f || !skillAvailable(10))
            return;
        float overhead = curHealth - maxHealth;
        if (overhead <= 0.0001f)
            return;
        float remove = overhealDegenerationPerSecond * Time.deltaTime;
        changeCurrentHealth(-Mathf.Min(overhead, remove));
    }

    void ApplyOvermanaDecay()
    {
        if (overmanaDegenerationPerSecond <= 0f)
            return;
        float overhead = curMana - maxMana;
        if (overhead <= 0.0001f)
            return;
        float remove = overmanaDegenerationPerSecond * Time.deltaTime;
        curMana -= Mathf.Min(overhead, remove);
        if (curMana < 0f)
            curMana = 0f;
    }

    /// <summary>Mana from Blood Ritual only: can exceed <see cref="maxMana"/> up to <see cref="BloodRitualOvermanaMaxMultiplier"/>.</summary>
    public void AddManaFromBloodRitual(float amount)
    {
        if (amount <= 0f || maxMana < 0.0001f)
            return;
        float cap = maxMana * BloodRitualOvermanaMaxMultiplier;
        curMana = Mathf.Min(curMana + amount, cap);
    }

    public void changeCurrentMana(float change)
    {
        if (change < 0f)
        {
            _lastManaSpendTime = Time.time;
            curMana += change;
            if (curMana < 0f)
                curMana = 0f;
        }
        else if (change > 0f)
        {
            if (curMana >= maxMana)
                return;
            curMana = Mathf.Min(curMana + change, maxMana);
        }

        if (maxMana < 1f)
            maxMana = 1f;
    }

    public float calculateManaRegeneration(int curLevel)
    {
        // Stronger per-level growth than the post-nerf linear-only curve; mild L² so high levels keep pace with skill costs.
        return 0.5f + curLevel * 0.7f + curLevel * curLevel * 0.005f;
    }

    /// <summary>Multiplier applied to base mana regen; rises the longer the player has not spent mana (see <see cref="changeCurrentMana"/> negative deltas).</summary>
    public float GetManaRegenIdleMultiplier()
    {
        float maxM = Mathf.Max(1f, manaRegenMaxMultiplierAtFullIdle);
        if (manaRegenRampIdleSeconds <= 0.001f)
            return maxM;

        float sinceSpend = _lastManaSpendTime < 0f
            ? manaRegenRampIdleSeconds
            : Time.time - _lastManaSpendTime;
        float u = Mathf.Clamp01(sinceSpend / manaRegenRampIdleSeconds);
        u = u * u * (3f - 2f * u);
        return Mathf.Lerp(1f, maxM, u);
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




