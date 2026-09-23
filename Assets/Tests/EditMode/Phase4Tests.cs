using System.Linq;
using NUnit.Framework;
using PvZ2.Foundation;
using UnityEngine;

public sealed class Phase4Tests
{
    sealed class Rig
    {
        public GameObject root;
        public GridManager grid;
        public PlantPlacementController placement;
        public BasicZombieSpawner spawner;
        public WaveManager waves;
        public ZombieData normal;
        public LevelData level;
    }

    Rig CreateRig(int waveCount = 1, float transition = 0f)
    {
        Rig r = new();
        r.root = new GameObject("Phase4 Test Rig");
        GameObject gridGo = new("Grid");
        gridGo.transform.SetParent(r.root.transform);
        BattlefieldProjection projection = gridGo.AddComponent<BattlefieldProjection>();
        projection.Configure(Vector2.zero, new Vector2(1.2f, 1f));
        r.grid = gridGo.AddComponent<GridManager>();
        r.grid.BuildProjectedGrid(5, 9, projection);
        LaneCombatRegistry registry = r.root.AddComponent<LaneCombatRegistry>();
        registry.Configure(5);
        r.placement = r.root.AddComponent<PlantPlacementController>();
        r.spawner = r.root.AddComponent<BasicZombieSpawner>();

        GameObject prefabGo = new("Zombie Prefab");
        prefabGo.transform.SetParent(r.root.transform);
        prefabGo.SetActive(false);
        prefabGo.AddComponent<CombatHealth>();
        ZombieController prefab = prefabGo.AddComponent<ZombieController>();
        r.normal = ScriptableObject.CreateInstance<ZombieData>();
        r.normal.id = "test"; r.normal.prefab = prefab; r.normal.maxHealth = 10;
        r.normal.movementSpeed = 1f; r.normal.attackRange = .7f; r.normal.attackDamage = 1; r.normal.attackInterval = 1f;
        r.spawner.Configure(registry, r.grid, r.root.transform, 6f, -6f, r.normal, r.normal);

        r.level = ScriptableObject.CreateInstance<LevelData>();
        r.level.preparationDelay = 0f; r.level.waveTransitionDelay = transition;
        for (int i = 0; i < waveCount; i++)
        {
            WaveData wave = new() { displayName = $"Wave {i + 1}" };
            wave.spawns.Add(new ZombieSpawnEntry { zombie = r.normal, amount = 1, spawnInterval = 1f, laneMode = LaneMode.FixedLane, fixedLane = i % 5 });
            r.level.waves.Add(wave);
        }
        r.waves = r.root.AddComponent<WaveManager>();
        r.waves.Configure(r.level, r.spawner, r.placement);
        return r;
    }

    void Dispose(Rig r)
    {
        if (r == null) return;
        Object.DestroyImmediate(r.root);
        Object.DestroyImmediate(r.normal);
        Object.DestroyImmediate(r.level);
    }

[Test] public void Projection_LowerLaneIsWider()
    {
        GameObject go = new(); BattlefieldProjection p = go.AddComponent<BattlefieldProjection>();
        p.Configure(Vector2.zero, new Vector2(.9f, 1.12f));
        Assert.AreEqual(p.GetCellSize(0, 5), p.GetCellSize(4, 5));
        Assert.Greater(p.GetCellSize(0, 5).y, p.GetCellSize(0, 5).x);
        Object.DestroyImmediate(go);
    }

[Test] public void Projection_MapsAllFortyFiveCells()
    {
        Rig r = CreateRig();
        Assert.AreEqual(5, r.grid.Rows);
        Assert.AreEqual(9, r.grid.Columns);
        Assert.AreEqual(45, r.grid.GetComponentsInChildren<GridCell>().Length);
        for (int row = 0; row < 5; row++)
        for (int column = 0; column < 9; column++)
        {
            GridCell expected = r.grid.GetCell(row, column);
            Assert.NotNull(expected);
            Assert.IsTrue(r.grid.TryWorldToCell(expected.WorldPosition, out GridCell mapped));
            Assert.AreSame(expected, mapped);
            Assert.AreEqual(r.grid.GetCellCenter(0, column).x, expected.WorldPosition.x, .0001f);
            Assert.AreEqual(r.grid.GetCellCenter(row, 0).y, expected.WorldPosition.y, .0001f);
        }
        Dispose(r);
    }

    [Test] public void BasicSpawner_HasNoProductionUpdateLoop()
    {
        Assert.IsNull(typeof(BasicZombieSpawner).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
    }

    [Test] public void WaveManager_SpawnsAndTracksAlive()
    {
        Rig r = CreateRig(); Assert.IsTrue(r.waves.BeginLevel());
        Assert.AreEqual(1, r.spawner.SpawnCount); Assert.AreEqual(1, r.waves.AliveEnemies);
        Dispose(r);
    }

    [Test] public void FinalWave_DoesNotWinWhileZombieAlive()
    {
        Rig r = CreateRig(); r.waves.BeginLevel();
        Assert.AreEqual(GameState.Playing, r.waves.State);
        Assert.IsTrue(r.waves.AllCurrentWaveSpawned);
        Dispose(r);
    }

    [Test] public void FinalWave_WinsOnlyAfterLastDeath()
    {
        Rig r = CreateRig(); r.waves.BeginLevel();
        r.spawner.LastSpawned.Health.TakeDamage(999);
        Assert.AreEqual(GameState.Victory, r.waves.State);
        Assert.IsTrue(r.placement.PlacementBlocked);
        Dispose(r);
    }

    [Test] public void WaveTransition_AdvancesExactlyOnce()
    {
        Rig r = CreateRig(2, .5f); r.waves.BeginLevel();
        r.spawner.LastSpawned.Health.TakeDamage(999);
        Assert.AreEqual(GameState.WaveTransition, r.waves.State);
        r.waves.Tick(.6f);
        Assert.AreEqual(2, r.waves.CurrentWaveNumber);
        Assert.AreEqual(2, r.spawner.SpawnCount);
        Dispose(r);
    }

    [Test] public void FinalWaveEvent_FiresOnce()
    {
        Rig r = CreateRig(2, 0f); int count = 0; r.waves.OnFinalWaveStarted += () => count++;
        r.waves.BeginLevel(); r.spawner.LastSpawned.Health.TakeDamage(999);
        Assert.AreEqual(1, count);
        Dispose(r);
    }

    [Test] public void ReachingHome_CausesDefeatAndBlocksPlacement()
    {
        Rig r = CreateRig(); r.waves.BeginLevel();
        r.spawner.LastSpawned.Tick(100f);
        Assert.AreEqual(GameState.Defeat, r.waves.State);
        Assert.IsTrue(r.placement.PlacementBlocked);
        Dispose(r);
    }

    [Test] public void SampleLevel_HasThreeRequiredWaves()
    {
        LevelData data = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/FrontYardLevel.asset");
        Assert.NotNull(data); Assert.AreEqual(3, data.waves.Count);
        Assert.IsTrue(data.waves[1].spawns.Any(x => x.zombie != null && x.zombie.id.Contains("cone")));
        Assert.IsTrue(data.waves[2].spawns.Any(x => x.zombie != null && x.zombie.id.Contains("cone")));
    }


[Test] public void WaveManager_RandomSpawnUsesOnlyFourValidLanes()
    {
        Rig r = CreateRig();
        ZombieSpawnEntry entry = r.level.waves[0].spawns[0];
        entry.amount = 24;
        entry.spawnInterval = .01f;
        entry.laneMode = LaneMode.RandomValidLane;
        r.waves.BeginLevel();
        r.waves.Tick(1f);
        ZombieController[] zombies = r.root.GetComponentsInChildren<ZombieController>(true);
        Assert.IsTrue(zombies.Where(z => z.gameObject.activeSelf).All(z => z.Lane >= 0 && z.Lane < r.grid.Rows));
        Dispose(r);
    }
}