using UnityEngine;

/// <summary>Runs after <see cref="EnemyAI"/> / <see cref="GreaterEnemyAI"/> OnEnable so level-based tuning is not overwritten.</summary>
[DefaultExecutionOrder(50)]
public class EnemyCharacter : BaseCharacter {
    
    private GameObject player;
    public float expOnKill;
    public Color originalColor;
    public bool iAmGreaterEnemy;

    [Tooltip("Enable on boss prefabs for extra health/EXP and scoreboard bonus on death.")]
    public bool isBoss;

    /// <summary>Stable scale for <see cref="CombatFeedback"/> hit punch (avoids compounding when coroutines overlap).</summary>
    public Vector3 HitPunchRestLocalScale { get; private set; }

    Color _paletteFromSharedMaterial;

    void Awake()
    {
        var r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
            _paletteFromSharedMaterial = r.sharedMaterial.color;

        HitPunchRestLocalScale = transform.localScale;
    }

    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player == null)
            return;

        if (GetComponent<Renderer>() != null)
        {
            originalColor = _paletteFromSharedMaterial;
            GetComponent<Renderer>().material.color = originalColor;
        }

        setEnemyStatistics(player.GetComponent<PlayerCharacter>().getCurLevel(), player.GetComponent<PlayerCharacter>().getExpNeededForLevelUp());

        HitPunchRestLocalScale = transform.localScale;
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
            maxHealth = curPlayerLevel * 215f * Random.Range(20f, 30f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 285f;
        }
        else
        {
            maxHealth = curPlayerLevel * 28f * Random.Range(2.1f, 3.1f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 15.5f;
        }

        if (isBoss)
        {
            maxHealth *= GameBalanceHelper.BossHealthMultiplier;
            curHealth = maxHealth;
            expOnKill *= GameBalanceHelper.BossExpMultiplier;
        }

        int lv = Mathf.Max(1, Mathf.RoundToInt(curPlayerLevel));

        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
            enemyAI.ApplyCombatTuning(lv, isBoss);

        var greaterAi = GetComponent<GreaterEnemyAI>();
        if (greaterAi != null)
            greaterAi.ApplyCombatTuning(lv, isBoss);
    }

    public float getExpOnKill()
    {
        return expOnKill;
    }

}
