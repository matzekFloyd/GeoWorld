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

    // Use this for initialization
    void Start() {

        state = State.Spawn;
        target = GameObject.FindGameObjectWithTag("Player1");
        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");

        spawnTimer = Random.Range(10f, 15f);
        attDistance = 10;
        huntDistance = Random.Range(150f, 300f);

        attackTimer = 0;
        coolDown = Random.Range(1.5f, 3f);
    }
        
    // Update is called once per frame
    void Update() {

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
        if(this.GetComponentInParent<EnemyCharacter>().curHealth <= 0)
        {
            state = State.Die;
        }

    }

    private void spawn()
    {
        if (spawnFinished) state = State.Idle;
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
            PlayerCharacter pcHealth = (PlayerCharacter)target.GetComponent("PlayerCharacter");
            pcHealth.changeCurrentHealth(-damage);
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

    public void getDamaged(float damage)
    {
        damagedTimer = 0.2f;
        this.gameObject.GetComponent<Renderer>().material.color = Color.red;

        this.gameObject.GetComponent<EnemyCharacter>().curHealth -= damage;
        state = State.Damaged;
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
        this.GetComponent<Renderer>().material.color = this.gameObject.GetComponent<EnemyCharacter>().originalColor + Color.red* calculateRedMultiplier();
        state = State.Idle;
    }

    private float calculateRedMultiplier()
    {
        return 1 - this.gameObject.GetComponent<EnemyCharacter>().curHealth / this.gameObject.GetComponent<EnemyCharacter>().maxHealth;
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
        this.GetComponent<Renderer>().material.color = this.gameObject.GetComponent<EnemyCharacter>().originalColor;

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
        enemyGenerator.GetComponent<EnemyGenerator>().targets.Remove(this.transform);
        target.GetComponent<FreezeTime>().enemiesToFreeze.Remove(this.gameObject);

        float gainedExp = this.gameObject.GetComponent<EnemyCharacter>().getExpOnKill();
        target.GetComponent<PlayerCharacter>().AddExp(gainedExp);

        ++target.GetComponent<GameOver>().enemyKillCounter;

        Destroy(this.gameObject, 0);

    }
}