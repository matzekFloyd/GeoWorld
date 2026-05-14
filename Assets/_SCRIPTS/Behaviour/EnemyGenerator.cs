using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains desired living enemy counts from <see cref="GameBalanceHelper.GetDesiredLivingEnemyCount"/> vs <see cref="targets"/>.
/// </summary>
/// <remarks>
/// <para><b>Boss cadence</b> (also documented in README): when player level is at least
/// <see cref="GameBalanceHelper.GreaterEnemiesMinPlayerLevel"/> and is a multiple of
/// <see cref="GameBalanceHelper.BossSpawnLevelMultiple"/>, a boss encounter is <b>scheduled once per level</b>.
/// Example with defaults (10, 5): bosses at levels 10, 15, 20, …</para>
/// <para>A <b>telegraph</b> runs first (HUD banner + screen tint + optional SFX, real-time seconds from
/// <see cref="GameBalanceHelper.BossTelegraphDurationSeconds"/>), then the boss prefab spawns if there is still
/// no living boss in <see cref="targets"/> and <see cref="endBossPrefab"/> / spawn transforms are valid.
/// Only one living boss is allowed at a time to avoid spam.</para>
/// </remarks>
public class EnemyGenerator : MonoBehaviour
{
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
    GameObject player;
    PlayerCharacter m_Player;
    FreezeTime m_FreezeTime;

    public List<Transform> targets;

    public State state;

    [Header("Pooling / spawn burst")]
    [Tooltip("Base max normal enemies spawned per frame when catching up to the desired living count.")]
    [SerializeField] int maxEnemySpawnsPerFrameBase = 14;

    [Tooltip("Added to the base each player level (spawn budget scales up in late game).")]
    [SerializeField] int maxEnemySpawnsPerPlayerLevel = 1;
    [Tooltip("Inactive instances created at scene start per enemy prefab (reduces Instantiate spikes on first big spawn).")]
    [SerializeField] int prewarmInstancesPerEnemyPrefab = 24;
    [Tooltip("Inactive boss instances to keep ready (usually 0 or 1).")]
    [SerializeField] int prewarmEndBossInstances = 1;

    int m_EnemySpawnRemaining;
    int m_EnemySpawnWaveIndex;
    int m_LastBossSpawnForPlayerLevel = -1;
    bool m_BossSpawnRoutineActive;
    GeoWorldObjectPools m_Pools;

    void Start()
    {
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

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                MaybeRefillToDesiredCount();
                break;
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

    void Initialize()
    {
        if (!CheckForEnemyPrefabs())
            return;
        if (!CheckForSpawnpoints())
            return;
        state = EnemyGenerator.State.Setup;
    }

    void Setup()
    {
        state = EnemyGenerator.State.SpawnEnemy;
    }

    /// <summary>While idle, top up spawns when kills bring us under the level target (not only on level-up).</summary>
    void MaybeRefillToDesiredCount()
    {
        if (m_Player == null || m_BossSpawnRoutineActive)
            return;
        var session = GameSession.Instance;
        if (session != null && !session.IsRunActive)
            return;
        PruneNullTargets();
        int desired = GameBalanceHelper.GetDesiredLivingEnemyCount(m_Player.getCurLevel());
        if (targets.Count < desired)
            state = State.SpawnEnemy;
    }

    void PruneNullTargets()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null)
                targets.RemoveAt(i);
        }
    }

    void SpawnEnemy()
    {
        if (m_Player == null)
            return;

        PruneNullTargets();

        int level = m_Player.getCurLevel();
        int desiredEnemyCount = GameBalanceHelper.GetDesiredLivingEnemyCount(level);
        int currentEnemyCount = targets.Count;

        if (m_EnemySpawnRemaining == 0)
        {
            bool greaterEnemySpawnEnabled = level >= GameBalanceHelper.GreaterEnemiesMinPlayerLevel;
            int multiple = GameBalanceHelper.BossSpawnLevelMultiple;
            if (greaterEnemySpawnEnabled &&
                multiple > 0 &&
                level % multiple == 0 &&
                m_LastBossSpawnForPlayerLevel != level &&
                !m_BossSpawnRoutineActive &&
                !IsBossAliveInTargets() &&
                endBossPrefab != null &&
                TryGetBossSpawnTransform(out _, out _))
            {
                StartCoroutine(BossTelegraphAndSpawnRoutine(level));
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
        int spawnBudget = Mathf.Clamp(maxEnemySpawnsPerFrameBase + level * maxEnemySpawnsPerPlayerLevel, 8, 48);
        while (m_EnemySpawnRemaining > 0 && spawnedThisFrame < spawnBudget)
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

    IEnumerator BossTelegraphAndSpawnRoutine(int level)
    {
        m_BossSpawnRoutineActive = true;
        m_LastBossSpawnForPlayerLevel = level;
        try
        {
            GameplayHudView.Instance?.PlayBossIncomingTelegraph(
                GameBalanceHelper.BossTelegraphDurationSeconds,
                level,
                GameBalanceHelper.BossTelegraphTintAlpha);
            GameplaySfx.Instance?.PlayBossIncoming();

            yield return new WaitForSecondsRealtime(GameBalanceHelper.BossTelegraphDurationSeconds);

            if (endBossPrefab == null || IsBossAliveInTargets())
                yield break;

            if (!TryGetBossSpawnTransform(out var spawnPos, out var parentT))
                yield break;

            GameObject endBoss = m_Pools != null
                ? m_Pools.Acquire(endBossPrefab, spawnPos, Quaternion.identity, parentT)
                : Instantiate(endBossPrefab, spawnPos, Quaternion.identity, parentT);
            if (endBoss == null)
                yield break;

            var bossChar = endBoss.GetComponent<EnemyCharacter>();
            if (bossChar != null)
                bossChar.isBoss = true;
            AddTarget(endBoss.transform);
        }
        finally
        {
            m_BossSpawnRoutineActive = false;
        }
    }

    bool IsBossAliveInTargets()
    {
        if (targets == null)
            return false;
        for (var i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null)
                continue;
            var ec = t.GetComponent<EnemyCharacter>();
            if (ec != null && ec.isBoss)
                return true;
        }
        return false;
    }

    bool TryGetBossSpawnTransform(out Vector3 position, out Transform parent)
    {
        position = default;
        parent = null;
        if (endBossSpawnPoint != null)
        {
            position = endBossSpawnPoint.transform.position;
            parent = endBossSpawnPoint.transform;
            return true;
        }

        if (greaterEnemySpawnPoints == null || greaterEnemySpawnPoints.Length == 0)
            return false;

        var start = Random.Range(0, greaterEnemySpawnPoints.Length);
        for (var k = 0; k < greaterEnemySpawnPoints.Length; k++)
        {
            var sp = greaterEnemySpawnPoints[(start + k) % greaterEnemySpawnPoints.Length];
            if (sp == null)
                continue;
            position = sp.transform.position;
            parent = sp.transform;
            return true;
        }

        return false;
    }

    public void AddAllEnemies()
    {
        GameObject[] go = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in go)
            AddTarget(enemy.transform);
    }

    public void AddTarget(Transform enemy)
    {
        targets.Add(enemy);
    }

    bool CheckForEnemyPrefabs()
    {
        return enemyPrefabs != null && enemyPrefabs.Length > 0;
    }

    bool CheckForSpawnpoints()
    {
        return spawnPoints != null && spawnPoints.Length > 0;
    }

    GameObject[] AvailableSpawnpoints()
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

    /// <summary>Legacy entry point; prefer the telegraphed flow from <see cref="SpawnEnemy"/>.</summary>
    public void spawnEndBoss()
    {
        if (endBossPrefab == null || IsBossAliveInTargets())
            return;
        if (!TryGetBossSpawnTransform(out var spawnPos, out var parentT))
            return;

        GameObject endBoss = m_Pools != null
            ? m_Pools.Acquire(endBossPrefab, spawnPos, Quaternion.identity, parentT)
            : Instantiate(endBossPrefab, spawnPos, Quaternion.identity, parentT);
        if (endBoss == null)
            return;
        var bossChar = endBoss.GetComponent<EnemyCharacter>();
        if (bossChar != null)
            bossChar.isBoss = true;
        AddTarget(endBoss.transform);
    }
}
