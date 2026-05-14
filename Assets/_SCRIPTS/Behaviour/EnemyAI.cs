using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour {

    public enum State
    {
        Spawn,
        Idle,
        Hunt,
        Attack,
        Damaged,
        Frozen,
        Die,
    }

    public State state;

    public float moveSpeed;
    public float rotationSpeed;
    public float huntDistance;
    public float attDistance;
    public GameObject target;
    public GameObject enemyGenerator;
    public float attackTimer;
    public float coolDown;
    public float damage;

    private bool currentlyAbleToAttack;
    private float spawnTimer;
    private bool spawnFinished;
    private float damagedTimer;
    private bool damagedFinished;
    protected float freezeTimer;
    protected bool freezeFinished;

    EnemyCharacter m_EnemyCharacter;

    void EnsureEnemyCharacter()
    {
        if (m_EnemyCharacter != null)
            return;
        m_EnemyCharacter = GetComponent<EnemyCharacter>()
            ?? GetComponentInParent<EnemyCharacter>()
            ?? GetComponentInChildren<EnemyCharacter>(true);
    }

    void OnEnable()
    {
        m_EnemyCharacter = null;
        EnsureEnemyCharacter();
        state = State.Spawn;
        target = GameObject.FindGameObjectWithTag("Player1");
        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");

        spawnTimer = Random.Range(10f, 15f);
        attDistance = 10;
        huntDistance = Random.Range(150f, 300f);

        attackTimer = 0;
        coolDown = Random.Range(1.5f, 3f);
        spawnFinished = false;
        damagedFinished = false;
        freezeFinished = false;
    }
        
    // Update is called once per frame
    void Update() {

        EnsureEnemyCharacter();
        if (m_EnemyCharacter == null)
            return;

        calculateAttackCooldown();
        calculateSpawnCooldown();
        calculateFreezeCooldown();
        calculateDamagedCooldown();

        switch (state)
        {
            case State.Spawn:
                spawn();
                break;
            case State.Idle:
                idle();
                break;
            case State.Hunt:
                hunt();
                break;
            case State.Attack:
                attack();
                break;
            case State.Damaged:
                damaged();
                break;
            case State.Frozen:
                frozen();
                break;
            case State.Die:
                die();
                break;
        }
        if (m_EnemyCharacter.curHealth <= 0)
        {
            state = State.Die;
        }

    }

    private void spawn()
    {
        if (!spawnFinished)
            return;
        EnsureEnemyCharacter();
        var boss = m_EnemyCharacter != null && m_EnemyCharacter.isBoss;
        var sfx = GameplaySfx.Instance;
        if (sfx != null)
        {
            if (boss)
                sfx.PlayEnemySpawnElite();
            else
                sfx.PlayEnemySpawnNormal();
        }
        state = State.Idle;
    }

    private void idle()
    {
        //Warte bis Player in Reichweite ist
        if (!(target.GetComponent<PlayerCharacter>().iAmDead()))
        {
            if (Vector3.Distance(target.transform.position, transform.position) < huntDistance)
            {
                state = State.Hunt;
            }
        }
    }

    private void hunt()
    {

        Debug.DrawLine(target.transform.position, transform.position, Color.red);

        //Schaue dein Ziel an
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.transform.position - transform.position), rotationSpeed * Time.deltaTime);

        //Bewege dich auf dein Ziel zu

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
                  
        if (Vector3.Distance(target.transform.position, transform.position) < attDistance)
        {
            state = State.Attack;
        } else if(Vector3.Distance(target.transform.position, transform.position) < huntDistance)
        {
            state = State.Hunt;
        } else if(Vector3.Distance(target.transform.position, transform.position) > huntDistance)
        {
            state = State.Idle;
        }
        if (target.GetComponent<PlayerCharacter>().iAmDead())
        { state = State.Idle;
        }
        }

    private void attack()
    {
        if (currentlyAbleToAttack && freezeFinished)
        {
            var pcHealth = target.GetComponent<PlayerCharacter>();
            if (pcHealth != null)
            {
                pcHealth.ApplyIncomingDamage(damage, transform.position, true, CombatHitSeverity.Light);
                GameplaySfx.Instance?.PlayEnemyMeleeAttack();
            }
            attackTimer = coolDown;
        }
        
        float distance = Vector3.Distance(target.transform.position, transform.position);

        Vector3 dir = (target.transform.position - transform.position).normalized;

        float direction = Vector3.Dot(dir, transform.forward);

        if (!(distance <= 10f && direction > 0))
        {
            state = State.Hunt;
        }

        if (target.GetComponent<PlayerCharacter>().iAmDead())
        {
            state = State.Idle;
        }
    }

    private void calculateAttackCooldown()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
        if (attackTimer < 0)
        {
            attackTimer = 0;
        }

        if (attackTimer == 0)
        {
            currentlyAbleToAttack = true;
        }
        else
        {
            currentlyAbleToAttack = false;
        }
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

    public void getDamaged(float damage, Vector3? hitOrigin = null, CombatHitSeverity severity = CombatHitSeverity.Light)
    {
        EnsureEnemyCharacter();
        if (m_EnemyCharacter == null)
            return;
        damagedTimer = 0.2f;
        this.gameObject.GetComponent<Renderer>().material.color = Color.red;

        m_EnemyCharacter.curHealth -= damage;
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
        EnsureEnemyCharacter();
        if (m_EnemyCharacter == null)
            return;
        this.GetComponent<Renderer>().material.color = m_EnemyCharacter.originalColor + Color.red* calculateRedMultiplier();
        state = State.Idle;
    }

    private float calculateRedMultiplier()
    {
        if (m_EnemyCharacter == null)
            return 0f;
        return 1 - m_EnemyCharacter.curHealth / m_EnemyCharacter.maxHealth;
    }

    public void freeze(float duration)
    {

        freezeTimer = duration;

            this.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ |
                                                         RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;

            this.gameObject.GetComponent<Renderer>().material.color = Color.magenta;



        state = State.Frozen;

    }

    private void frozen()
    {
        if (freezeFinished)
        {
            unfreeze();
        }
    }

    private void unfreeze()
    {
        this.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        EnsureEnemyCharacter();
        if (m_EnemyCharacter != null)
            this.GetComponent<Renderer>().material.color = m_EnemyCharacter.originalColor;

        state = State.Idle;
    }

    private void calculateFreezeCooldown()
    {
        if (freezeTimer > 0)
        {
            freezeTimer -= Time.deltaTime;
        }
        if (freezeTimer < 0)
        {
            freezeTimer = 0;
        }

        if (freezeTimer == 0)
        {
            freezeFinished = true;
        }
        else
        {
            freezeFinished = false;
        }
    }

    private void die()
    {
        EnsureEnemyCharacter();
        var bossDie = m_EnemyCharacter != null && m_EnemyCharacter.isBoss;
        GameplaySfx.Instance?.PlayEnemyDie(bossDie);

        enemyGenerator.GetComponent<EnemyGenerator>().targets.Remove(this.transform);
        target.GetComponent<FreezeTime>().enemiesToFreeze.Remove(this.gameObject);

        EnemyCharacter ec = m_EnemyCharacter;
        if (ec == null)
        {
            var pooledOnly = GetComponent<PooledObject>();
            if (pooledOnly != null && pooledOnly.IsManaged)
                pooledOnly.ReleaseToPool();
            else
                Destroy(gameObject, 0);
            return;
        }
        float gainedExp = ec.getExpOnKill();
        var pc = target.GetComponent<PlayerCharacter>();
        pc.AddExp(gainedExp + (ec.isBoss ? GameBalanceHelper.BossBonusXpFlat : 0f));

        GameOver go = target.GetComponent<GameOver>();
        ++go.enemyKillCounter;
        if (ec.isBoss)
        {
            ++go.bossKillCounter;
            go.bossBonusScoreTotal += GameBalanceHelper.BossScoreBonusOnKill;
        }

        var pooled = GetComponent<PooledObject>();
        if (pooled != null && pooled.IsManaged)
            pooled.ReleaseToPool();
        else
            Destroy(this.gameObject, 0);

    }
}