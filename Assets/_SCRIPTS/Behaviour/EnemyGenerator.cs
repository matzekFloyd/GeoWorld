using UnityEngine;
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

    [Header("Pooling / spawn burst")]
    [Tooltip("Max normal enemies spawned per frame when catching up to level × enemiesPerLevel.")]
    [SerializeField] int maxEnemySpawnsPerFrame = 16;
    [Tooltip("Inactive instances created at scene start per enemy prefab (reduces Instantiate spikes on first big spawn).")]
    [SerializeField] int prewarmInstancesPerEnemyPrefab = 24;
    [Tooltip("Inactive boss instances to keep ready (usually 0 or 1).")]
    [SerializeField] int prewarmEndBossInstances = 1;

    int m_EnemySpawnRemaining;
    int m_EnemySpawnWaveIndex;
    int m_LastBossSpawnForPlayerLevel = -1;
    GeoWorldObjectPools m_Pools;

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
        m_Pools = GeoWorldObjectPools.Instance;
        PrewarmEnemyPools();
    }

    void PrewarmEnemyPools()
    {
        if (m_Pools == null || enemyPrefabs == null)
            return;
        foreach (var p in enemyPrefabs)
        {
            if (p != null)
                m_Pools.Prewarm(p, prewarmInstancesPerEnemyPrefab);
        }
        if (endBossPrefab != null)
            m_Pools.Prewarm(endBossPrefab, prewarmEndBossInstances);
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

        int level = m_Player.getCurLevel();
        int desiredEnemyCount;
        if (level <= 1)
            desiredEnemyCount = GameBalanceHelper.EnemiesAtPlayerLevel1;
        else if (level == 2)
            desiredEnemyCount = GameBalanceHelper.EnemiesAtPlayerLevel2;
        else
            desiredEnemyCount = level * GameBalanceHelper.EnemiesPerPlayerLevel;
        int currentEnemyCount = targets.Count;

        if (m_EnemySpawnRemaining == 0)
        {
            bool greaterEnemySpawnEnabled = level >= GameBalanceHelper.GreaterEnemiesMinPlayerLevel;
            if (greaterEnemySpawnEnabled &&
                level % GameBalanceHelper.BossSpawnLevelMultiple == 0 &&
                m_LastBossSpawnForPlayerLevel != level)
            {
                spawnEndBoss();
                m_LastBossSpawnForPlayerLevel = level;
            }

            int toSpawn = desiredEnemyCount - currentEnemyCount;
            if (toSpawn <= 0)
            {
                state = EnemyGenerator.State.Idle;
                return;
            }
            m_EnemySpawnRemaining = toSpawn;
            m_EnemySpawnWaveIndex = 0;
        }

        GameObject[] gos = AvailableSpawnpoints();
        int spawnPointCount = gos.Length;
        if (spawnPointCount == 0)
        {
            state = EnemyGenerator.State.Idle;
            return;
        }

        int spawnedThisFrame = 0;
        while (m_EnemySpawnRemaining > 0 && spawnedThisFrame < maxEnemySpawnsPerFrame)
        {
            int selectedSpawnPoint = m_EnemySpawnWaveIndex % spawnPointCount;
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = m_Pools != null
                ? m_Pools.Acquire(prefab, gos[selectedSpawnPoint].transform.position, Quaternion.identity, gos[selectedSpawnPoint].transform)
                : Instantiate(prefab, gos[selectedSpawnPoint].transform.position, Quaternion.identity, gos[selectedSpawnPoint].transform);

            AddTarget(enemy.transform);
            if (m_FreezeTime != null)
                m_FreezeTime.AddTarget(enemy);
            m_EnemySpawnWaveIndex++;
            m_EnemySpawnRemaining--;
            spawnedThisFrame++;
        }

        if (m_EnemySpawnRemaining == 0)
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
        return spawnPoints;
    }

    /// <summary>
    /// Axis-aligned bounds on the XZ plane from spawn points, player, and boss spawns (plus padding).
    /// Used by the minimap; if nothing is registered yet, returns false.
    /// </summary>
    public bool TryGetArenaBoundsXZ(out Vector3 center, out Vector2 halfExtents, float paddingWorld = 22f, float minHalfExtent = 100f)
    {
        center = Vector3.zero;
        halfExtents = Vector2.one * minHalfExtent;

        var pts = new List<Vector3>(16);
        if (spawnPoints != null)
        {
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                    pts.Add(spawnPoints[i].transform.position);
            }
        }
        if (greaterEnemySpawnPoints != null)
        {
            for (var i = 0; i < greaterEnemySpawnPoints.Length; i++)
            {
                if (greaterEnemySpawnPoints[i] != null)
                    pts.Add(greaterEnemySpawnPoints[i].transform.position);
            }
        }
        if (endBossSpawnPoint != null)
            pts.Add(endBossSpawnPoint.transform.position);
        if (player != null)
            pts.Add(player.transform.position);

        if (pts.Count == 0)
            return false;

        var min = pts[0];
        var max = pts[0];
        for (var i = 1; i < pts.Count; i++)
        {
            var p = pts[i];
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        center = new Vector3(0.5f * (min.x + max.x), 0f, 0.5f * (min.z + max.z));
        var hx = Mathf.Max((max.x - min.x) * 0.5f + paddingWorld, minHalfExtent);
        var hz = Mathf.Max((max.z - min.z) * 0.5f + paddingWorld, minHalfExtent);
        halfExtents = new Vector2(hx, hz);
        return true;
    }

    public void spawnEndBoss()
    {
        int randomizeSpawnpointValue = Random.Range(1, 4);

        GameObject endBoss = m_Pools != null
            ? m_Pools.Acquire(endBossPrefab, greaterEnemySpawnPoints[randomizeSpawnpointValue].transform.position, Quaternion.identity, greaterEnemySpawnPoints[randomizeSpawnpointValue].transform)
            : Instantiate(endBossPrefab, greaterEnemySpawnPoints[randomizeSpawnpointValue].transform.position, Quaternion.identity, greaterEnemySpawnPoints[randomizeSpawnpointValue].transform);
        EnemyCharacter bossChar = endBoss.GetComponent<EnemyCharacter>();
        if (bossChar != null)
            bossChar.isBoss = true;
        AddTarget(endBoss.transform);

    }
}
