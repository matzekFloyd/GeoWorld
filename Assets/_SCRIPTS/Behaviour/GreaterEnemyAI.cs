using UnityEngine;
using System.Collections;

public class GreaterEnemyAI : MonoBehaviour {

    public enum State
    {
        Spawn,
        Idle,
        Move,
        ShootSmall,
        ShootBig,
        Damaged,
        Die,
    }

    public State state;
    public GameObject enemyGenerator;
    public GameObject target;

    public GameObject smallHomingMissile;
    public GameObject bigHomingMissile;


    private float spawnTimer;
    private bool spawnFinished;

    private float moveTimer;
    private bool moveFinished;

    private float shootSmallTimer;
    private bool shootSmallFinished;

    private float shootBigTimer;
    private bool shootBigFinished;

    private float damagedTimer;
    private bool damagedFinished;

    float _moveImpulse = 5f;
    float _moveCooldownAfterStrafe;
    float _shootSmallCd;
    float _shootBigCd;

    void OnEnable()
    {
        state = State.Spawn;

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        target = GameObject.FindGameObjectWithTag("Player1");

        spawnFinished = false;
        moveFinished = false;
        shootSmallFinished = false;
        shootBigFinished = false;
        damagedFinished = false;
    }

    /// <summary>Called from <see cref="EnemyCharacter.setEnemyStatistics"/>.</summary>
    public void ApplyCombatTuning(int playerLevel, bool isBoss)
    {
        float lv = Mathf.Max(1, playerLevel);
        float pressure = Mathf.Clamp01((lv - 1f) / 38f);

        spawnTimer = Mathf.Max(1.8f, 4.6f - lv * 0.085f - (isBoss ? 0.4f : 0f));
        _moveCooldownAfterStrafe = Mathf.Max(0.95f, 2.65f - lv * 0.052f);
        moveTimer = _moveCooldownAfterStrafe;

        _shootSmallCd = Mathf.Max(0.55f, 2.15f - lv * 0.062f);
        _shootBigCd = Mathf.Max(5f, 13.2f - lv * 0.13f);
        if (isBoss)
        {
            _shootSmallCd *= 0.9f;
            _shootBigCd *= 0.88f;
        }

        shootSmallTimer = _shootSmallCd;
        shootBigTimer = _shootBigCd;

        _moveImpulse = 4.8f + lv * 0.2f + pressure * 3.5f;
    }

    // Update is called once per frame
    void Update () {

        calculateSpawnCooldown();
        calculateMoveCooldown();
        calculateShootSmallCooldown();
        calculateShootBigCooldown();
        calculateDamagedCooldown();


        switch (state)
        {
            case State.Spawn:
                spawn();
                break;
            case State.Idle:
                idle();
                break;
            case State.Move:
                move();
                break;
            case State.ShootSmall:
                shootSmall();
                break;
            case State.ShootBig:
                shootBig();
                break;
            case State.Damaged:
                damaged();
                break;
            case State.Die:
                die();
                break;
        }

        //damagedFinished = (timeDmgWasApplied - System.DateTime.Now).Seconds >= 2 && (timeDmgWasApplied - System.DateTime.Now).Seconds <= 3;



        if (this.gameObject.GetComponent<EnemyCharacter>().curHealth <= 0)
        {
            state = State.Die;
        }


    }

    private void spawn()
    {
        if (!spawnFinished)
            return;
        GameplaySfx.Instance?.PlayEnemySpawnElite();
        state = State.Idle;
    }

    private void idle()
    {
        if (moveFinished) state = State.Move;
        if (shootSmallFinished) state = State.ShootSmall;
        if (shootBigFinished) state = State.ShootBig;
    }

    private void move()
    {
        float x = Random.Range(-1, +1);
        float y = Random.Range(-1, +1);
        float z = Random.Range(-1, +1);
        Vector3 direction = new Vector3(x, y, z).normalized;
        this.gameObject.GetComponent<Rigidbody>().AddForce(direction * _moveImpulse, ForceMode.Impulse);
        moveTimer = _moveCooldownAfterStrafe;

        state = State.Idle;
    }

    private void shootSmall()
    {
        var pools = GeoWorldObjectPools.Instance;
        GameObject missile = null;
        if (pools != null && smallHomingMissile != null)
            missile = pools.Acquire(smallHomingMissile, transform.position, transform.rotation, null);
        else if (smallHomingMissile != null)
            missile = Instantiate(smallHomingMissile, transform.position, transform.rotation);
        GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(missile);
        GameplaySfx.Instance?.PlayEnemyRangedAttack();
        shootSmallTimer = _shootSmallCd;

        state = State.Idle;


    }

    private void shootBig()
    {
        var pools = GeoWorldObjectPools.Instance;
        GameObject missile = null;
        if (pools != null && bigHomingMissile != null)
            missile = pools.Acquire(bigHomingMissile, transform.position, transform.rotation, null);
        else if (bigHomingMissile != null)
            missile = Instantiate(bigHomingMissile, transform.position, transform.rotation);
        GeoWorldObjectPools.ApplyProjectileGravityIfApplicable(missile);

        GameplaySfx.Instance?.PlayEnemyRangedAttack();
        shootBigTimer = _shootBigCd;

        state = State.Idle;

    }

    public void getDamaged(float damage, Vector3? hitOrigin = null, CombatHitSeverity severity = CombatHitSeverity.Light, bool isCritical = false)
    {
        damagedTimer = 0.2f;
        damagedFinished = false;
        this.gameObject.GetComponent<Renderer>().material.color = Color.red;

        this.gameObject.GetComponent<EnemyCharacter>().curHealth -= damage;
        state = State.Damaged;

        var fx = CombatFeedback.Instance;
        if (fx != null)
            fx.NotifyEnemyHit(transform, damage, hitOrigin, severity, isCritical);
    }

    private void damaged()
    {
        if (damagedFinished)
        {
            undamaged();
        }
    }

    private void undamaged()
    {
        this.GetComponent<Renderer>().material.color = this.gameObject.GetComponent<EnemyCharacter>().originalColor + Color.red * calculateRedMultiplier();
        state = State.Idle;
    }

    private float calculateRedMultiplier()
    {
        return 1 - this.gameObject.GetComponent<EnemyCharacter>().curHealth / this.gameObject.GetComponent<EnemyCharacter>().maxHealth;
    }

    private void calculateSpawnCooldown()
    {
        if (spawnTimer > 0)
        {
            spawnTimer -= Time.deltaTime;
        }
        if (spawnTimer < 0)
        {
            spawnTimer = 0;
        }

        if (spawnTimer == 0)
        {
            spawnFinished = true;
        }
        else
        {
            spawnFinished = false;
        }
    }

    private void calculateMoveCooldown()
    {
        if (moveTimer > 0)
        {
            moveTimer -= Time.deltaTime;
        }
        if (moveTimer < 0)
        {
            moveTimer = 0;
        }

        if (moveTimer == 0)
        {
            moveFinished = true;
        }
        else
        {
            moveFinished = false;
        }
    }

    private void calculateShootSmallCooldown()
    {
        if (shootSmallTimer > 0)
        {
            shootSmallTimer -= Time.deltaTime;
        }
        if (shootSmallTimer < 0)
        {
            shootSmallTimer = 0;
        }

        if (shootSmallTimer == 0)
        {
            shootSmallFinished = true;
        }
        else
        {
            shootSmallFinished = false;
        }
    }

    private void calculateShootBigCooldown()
    {
        if (shootBigTimer > 0)
        {
            shootBigTimer -= Time.deltaTime;
        }
        if (shootBigTimer < 0)
        {
            shootBigTimer = 0;
        }

        if (shootBigTimer == 0)
        {
            shootBigFinished = true;
        }
        else
        {
            shootBigFinished = false;
        }
    }

    private void calculateDamagedCooldown()
    {
        if (damagedTimer > 0)
        {
            damagedTimer -= Time.deltaTime;
        }
        if (damagedTimer < 0)
        {
            damagedTimer = 0;
        }

        if (damagedTimer == 0)
        {
            damagedFinished = true;
        }
        else
        {
            damagedFinished = false;
        }
    }

    private void die()
    {
        var ec = this.gameObject.GetComponent<EnemyCharacter>();
        BattleLog.AppendEnemyDefeated(ec);
        var boss = ec != null && ec.isBoss;
        GameplaySfx.Instance?.PlayEnemyDie(boss);

        enemyGenerator.GetComponent<EnemyGenerator>().targets.Remove(this.transform);

        float gainedExp = ec.expOnKill;
        var pc = target.GetComponent<PlayerCharacter>();
        pc.AddExp(gainedExp + (boss ? GameBalanceHelper.BossBonusXpFlat : 0f));
        GameBalanceHelper.ApplyKillSustain(pc, boss, ec != null && ec.iAmGreaterEnemy && !boss);

        RunModifiers.Resolve()?.RegisterEnemyDefeated(boss, ec != null && ec.iAmGreaterEnemy && !boss);

        GameOver go = target.GetComponent<GameOver>();
        ++go.enemyKillCounter;
        if (boss)
        {
            ++go.bossKillCounter;
            go.bossBonusScoreTotal += GameBalanceHelper.BossScoreBonusOnKill;
        }
        else
            ++go.greaterEnemyKillCounter;
     
        var pooled = GetComponent<PooledObject>();
        if (pooled != null && pooled.IsManaged)
            pooled.ReleaseToPool();
        else
            Destroy(this.gameObject, 0);

    }
}
