using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyGenerator : MonoBehaviour {

    public enum State
    {
        Idle,
        Initialize,
        Setup,
        SpawnEnemy,
    }

    public GameObject[] enemyPrefabs;
    public GameObject[] spawnPoints;

    public GameObject endBossSpawnPoint;
    public GameObject[] greaterEnemySpawnPoints;


    public GameObject endBossPrefab;
    private GameObject player;
    private PlayerCharacter m_Player;
    private FreezeTime m_FreezeTime;

    public List<Transform> targets;

    public State state; //lokale Variable für den aktuellen State

	// Use this for initialization
	void Start () {
        targets = new List<Transform>();
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
        {
            m_Player = player.GetComponent<PlayerCharacter>();
            m_FreezeTime = player.GetComponent<FreezeTime>();
        }
        AddAllEnemies();
        state = EnemyGenerator.State.Initialize;
    }

    // Update is called once per frame
    void Update () {
        
        switch (state)
        {
            case State.Initialize:
                Initialize();
                break;
            case State.Setup:
                Setup();
                break;
            case State.SpawnEnemy:
                SpawnEnemy();
                break;
        }
    }

    private void Initialize()
    {
        if (!CheckForEnemyPrefabs())
        {
            return;
        }
        if (!CheckForSpawnpoints())
        {
            return;
        }
        state = EnemyGenerator.State.Setup;
    }

    private void Setup()
    {
        state = EnemyGenerator.State.SpawnEnemy;
    }

    private void SpawnEnemy()
    {
        if (m_Player == null) return;

        bool greaterEnemySpawnEnabled = m_Player.getCurLevel() >= GameBalanceHelper.GreaterEnemiesMinPlayerLevel;

        if (greaterEnemySpawnEnabled)
        {

            if (m_Player.getCurLevel() % GameBalanceHelper.BossSpawnLevelMultiple == 0) spawnEndBoss();            
        }

        int desiredEnemyCount = m_Player.getCurLevel() * GameBalanceHelper.EnemiesPerPlayerLevel;
        int currentEnemyCount = targets.Count;
        int enemiesToSpawn = desiredEnemyCount - currentEnemyCount;

            GameObject[] gos = AvailableSpawnpoints();
            int spawnPointCount = gos.Length;
            
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                int selectedSpawnPoint = i % spawnPointCount;

                GameObject enemy = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)],
                                            gos[selectedSpawnPoint].transform.position,
                                            Quaternion.identity
                                            ) as GameObject;
                enemy.transform.parent = gos[selectedSpawnPoint].transform;

                AddTarget(enemy.transform);
                if (m_FreezeTime != null)
                    m_FreezeTime.AddTarget(enemy);
            }
        
        state = EnemyGenerator.State.Idle;
    }

    public void AddAllEnemies()
    {
        GameObject[] go = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in go)
        {
            AddTarget(enemy.transform);
        }
    }

    public void AddTarget(Transform enemy)
    {
        targets.Add(enemy);
    }

    private bool CheckForEnemyPrefabs()
    {
        if(enemyPrefabs.Length > 0)
        {
            return true;
        }else
        {
            return false;
        }
    }

    private bool CheckForSpawnpoints()
    {
        if(spawnPoints.Length > 0)
        {
            return true;
        }else
        {
            return false;
        }
    }


    private GameObject[] AvailableSpawnpoints()
    {
        List<GameObject> y = new List<GameObject>();

        for(int i = 0; i < spawnPoints.Length; i++)
        {
            y.Add(spawnPoints[i]);
        }

        return y.ToArray();
    }

    public void spawnEndBoss()
    {
        int randomizeSpawnpointValue = Random.Range(1, 4);

        GameObject endBoss = Instantiate(endBossPrefab, greaterEnemySpawnPoints[randomizeSpawnpointValue].transform.position, Quaternion.identity) as GameObject;
        endBoss.transform.parent = greaterEnemySpawnPoints[randomizeSpawnpointValue].transform;
        EnemyCharacter bossChar = endBoss.GetComponent<EnemyCharacter>();
        if (bossChar != null)
            bossChar.isBoss = true;
        AddTarget(endBoss.transform);

    }
}