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

    public List<Transform> targets;

    public State state; //lokale Variable für den aktuellen State

	// Use this for initialization
	void Start () {
        targets = new List<Transform>();
        player = GameObject.FindGameObjectWithTag("Player1");
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
        bool greaterEnemySpawnEnabled = player.GetComponent<PlayerCharacter>().getCurLevel() >= 10;

        if (greaterEnemySpawnEnabled)
        {

            if ((player.GetComponent<PlayerCharacter>().getCurLevel() % 5 == 0)) spawnEndBoss();            
        }

        int desiredEnemyCount = player.GetComponent<PlayerCharacter>().getCurLevel() * 40;
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
                GameObject.FindGameObjectWithTag("Player1").GetComponent<FreezeTime>().AddTarget(enemy);
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
        AddTarget(endBoss.transform);

    }
}