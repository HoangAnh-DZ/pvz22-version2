using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PvZ2.Foundation
{
    public sealed class WaveManager : MonoBehaviour
    {
        struct ScheduledSpawn
        {
            public float time;
            public ZombieData data;
            public LaneMode laneMode;
            public int lane;
        }

        [SerializeField] LevelData level;
        [SerializeField] BasicZombieSpawner spawner;
        [SerializeField] PlantPlacementController placement;

        readonly List<ScheduledSpawn> schedule = new();
        readonly HashSet<ZombieController> alive = new();
        float stateTimer;
        float waveTime;
        int nextSpawn;
        bool started;

        public LevelData Level => level;
        public GameState State { get; private set; } = GameState.Preparing;
        public int CurrentWaveIndex { get; private set; } = -1;
        public int CurrentWaveNumber => Mathf.Max(0, CurrentWaveIndex + 1);
        public int TotalWaves => level != null ? level.waves.Count : 0;
        public int AliveEnemies => alive.Count;
        public bool IsFinalWave => level != null && CurrentWaveIndex == level.waves.Count - 1;
        public bool AllCurrentWaveSpawned => nextSpawn >= schedule.Count;

        public event Action<GameState> OnStateChanged;
        public event Action<int, int> OnWaveChanged;
        public event Action<int> OnAliveEnemiesChanged;
        public event Action OnFinalWaveStarted;

        void Start()
        {
            if (!started) BeginLevel();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(LevelData levelData, BasicZombieSpawner zombieSpawner, PlantPlacementController placementController)
        {
            level = levelData;
            spawner = zombieSpawner;
            placement = placementController;
        }

        public bool BeginLevel()
        {
            if (level == null || spawner == null || level.waves == null || level.waves.Count == 0) return false;
            UnsubscribeAll();
            started = true;
            CurrentWaveIndex = -1;
            schedule.Clear();
            nextSpawn = 0;
            placement?.SetPlacementBlocked(false);
            SetState(GameState.Preparing);
            stateTimer = Mathf.Max(0f, level.preparationDelay);
            if (stateTimer <= 0f) StartNextWave();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!started || State == GameState.Victory || State == GameState.Defeat) return;
            float dt = Mathf.Max(0f, deltaTime);
            if (State == GameState.Preparing || State == GameState.WaveTransition)
            {
                stateTimer -= dt;
                if (stateTimer <= 0f) StartNextWave();
                return;
            }
            if (State != GameState.Playing) return;

            waveTime += dt;
            while (nextSpawn < schedule.Count && schedule[nextSpawn].time <= waveTime)
            {
                Spawn(schedule[nextSpawn]);
                nextSpawn++;
            }
            EvaluateWaveCompletion();
        }

        void StartNextWave()
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            CurrentWaveIndex++;
            if (CurrentWaveIndex >= level.waves.Count)
            {
                ResolveVictory();
                return;
            }

            BuildSchedule(level.waves[CurrentWaveIndex]);
            waveTime = 0f;
            nextSpawn = 0;
            SetState(GameState.Playing);
            OnWaveChanged?.Invoke(CurrentWaveNumber, TotalWaves);
            if (IsFinalWave) OnFinalWaveStarted?.Invoke();
            while (nextSpawn < schedule.Count && schedule[nextSpawn].time <= 0f)
            {
                Spawn(schedule[nextSpawn]);
                nextSpawn++;
            }
            EvaluateWaveCompletion();
        }

        void BuildSchedule(WaveData wave)
        {
            schedule.Clear();
            if (wave?.spawns == null) return;
            foreach (ZombieSpawnEntry entry in wave.spawns)
            {
                if (entry == null || entry.zombie == null) continue;
                int count = Mathf.Max(1, entry.amount);
                for (int i = 0; i < count; i++)
                {
                    schedule.Add(new ScheduledSpawn
                    {
                        time = Mathf.Max(0f, entry.startDelay) + i * Mathf.Max(.01f, entry.spawnInterval),
                        data = entry.zombie,
                        laneMode = entry.laneMode,
                        lane = entry.fixedLane
                    });
                }
            }
            schedule.Sort((a, b) => a.time.CompareTo(b.time));
        }

void Spawn(ScheduledSpawn item)
        {
            int laneCount = spawner != null && spawner.Grid != null ? spawner.Grid.Rows : 0;
            if (laneCount <= 0) return;
            int lane = item.laneMode == LaneMode.FixedLane
                ? Mathf.Clamp(item.lane, 0, laneCount - 1)
                : UnityEngine.Random.Range(0, laneCount);
            ZombieController zombie = spawner.SpawnZombie(item.data, lane);
            if (zombie == null) return;
            alive.Add(zombie);
            zombie.Health.OnDied += HandleZombieDied;
            zombie.OnReachedHome += HandleReachedHome;
            OnAliveEnemiesChanged?.Invoke(alive.Count);
        }

        void HandleZombieDied(CombatHealth health)
        {
            ZombieController match = null;
            foreach (ZombieController zombie in alive)
            {
                if (zombie != null && zombie.Health == health) { match = zombie; break; }
            }
            if (match != null) RemoveAlive(match);
            EvaluateWaveCompletion();
        }

        void HandleReachedHome(ZombieController zombie)
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            RemoveAlive(zombie);
            SetState(GameState.Defeat);
            placement?.SetPlacementBlocked(true);
        }

        void RemoveAlive(ZombieController zombie)
        {
            if (zombie == null || !alive.Remove(zombie)) return;
            zombie.Health.OnDied -= HandleZombieDied;
            zombie.OnReachedHome -= HandleReachedHome;
            OnAliveEnemiesChanged?.Invoke(alive.Count);
        }

        void EvaluateWaveCompletion()
        {
            if (State != GameState.Playing || !AllCurrentWaveSpawned || alive.Count != 0) return;
            if (IsFinalWave) ResolveVictory();
            else
            {
                SetState(GameState.WaveTransition);
                stateTimer = Mathf.Max(0f, level.waveTransitionDelay);
                if (stateTimer <= 0f) StartNextWave();
            }
        }

        void ResolveVictory()
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;
            SetState(GameState.Victory);
            placement?.SetPlacementBlocked(true);
        }

        void SetState(GameState value)
        {
            State = value;
            OnStateChanged?.Invoke(value);
        }

        void UnsubscribeAll()
        {
            foreach (ZombieController zombie in alive)
            {
                if (zombie == null) continue;
                zombie.Health.OnDied -= HandleZombieDied;
                zombie.OnReachedHome -= HandleReachedHome;
            }
            alive.Clear();
            OnAliveEnemiesChanged?.Invoke(0);
        }

        public void RestartLevel()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        void OnDestroy()
        {
            UnsubscribeAll();
        }
    }
}