using UnityEngine;

[DefaultExecutionOrder(-50)]
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
