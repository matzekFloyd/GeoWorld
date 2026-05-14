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
    //System.DateTime timeDmgWasApplied;

    void OnEnable()
    {
        state = State.Spawn;

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        target = GameObject.FindGameObjectWithTag("Player1");

        spawnTimer = 5;
        moveTimer = 3;
        shootSmallTimer = 2;
        shootBigTimer = 10;
        spawnFinished = false;
        moveFinished = false;
        shootSmallFinished = false;
        shootBigFinished = false;
        damagedFinished = false;
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
        this.gameObject.GetComponent<Rigidbody>().AddForce(direction * 5, ForceMode.Impulse);
        moveTimer = 3;

        state = State.Idle;
    }

    private void shootSmall()
    {
        var pools = GeoWorldObjectPools.Instance;
        if (pools != null && smallHomingMissile != null)
            pools.Acquire(smallHomingMissile, transform.position, transform.rotation, null);
        else if (smallHomingMissile != null)
            Instantiate(smallHomingMissile, transform.position, transform.rotation);
        GameplaySfx.Instance?.PlayEnemyRangedAttack();
        shootSmallTimer = 3;

        state = State.Idle;


    }

    private void shootBig()
    {
        var pools = GeoWorldObjectPools.Instance;
        if (pools != null && bigHomingMissile != null)
            pools.Acquire(bigHomingMissile, transform.position, transform.rotation, null);
        else if (bigHomingMissile != null)
            Instantiate(bigHomingMissile, transform.position, transform.rotation);

        GameplaySfx.Instance?.PlayEnemyRangedAttack();
        shootBigTimer = 15;

        state = State.Idle;

    }

    public void getDamaged(float damage, Vector3? hitOrigin = null, CombatHitSeverity severity = CombatHitSeverity.Light)
    {
        damagedTimer = 0.2f;
        damagedFinished = false;
        this.gameObject.GetComponent<Renderer>().material.color = Color.red;

        this.gameObject.GetComponent<EnemyCharacter>().curHealth -= damage;
        state = State.Damaged;

        var fx = CombatFeedback.Instance;
        if (fx != null)
            fx.NotifyEnemyHit(transform, damage, hitOrigin, severity);
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
        var boss = ec != null && ec.isBoss;
        GameplaySfx.Instance?.PlayEnemyDie(boss);

        enemyGenerator.GetComponent<EnemyGenerator>().targets.Remove(this.transform);

        float gainedExp = ec.expOnKill;
        target.GetComponent<PlayerCharacter>().AddExp(gainedExp + (boss ? GameBalanceHelper.BossBonusXpFlat : 0f));

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
