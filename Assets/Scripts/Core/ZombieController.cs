using System;
using UnityEngine;

namespace PvZ2.Foundation
{
    [RequireComponent(typeof(CombatHealth))]
    public sealed class ZombieController : MonoBehaviour
    {
        public enum ZombieState { Moving, Attacking, Dead }

        [SerializeField] private ZombieData data;
        [SerializeField] private int lane = -1;
        [SerializeField] private float homeBoundaryX = -6.5f;
        [SerializeField] private ZombieState state = ZombieState.Moving;

        private LaneCombatRegistry registry;
        private CombatHealth health;
        private PlantBase target;
        private float attackTimer;
        private bool deathResolved;

        public ZombieData Data => data;
        public int Lane => lane;
        public ZombieState State => state;
        public CombatHealth Health => health != null ? health : health = GetComponent<CombatHealth>();
        public PlantBase Target => target;
        public bool ReachedHome { get; private set; }
        public int AttackCount { get; private set; }

        public event Action<ZombieController> OnReachedHome;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Initialize(ZombieData zombieData, int zombieLane, LaneCombatRegistry combatRegistry, float boundaryX = -6.5f)
        {
            if (zombieData == null || combatRegistry == null ||
                zombieLane < 0 || zombieLane >= combatRegistry.LaneCount)
            {
                return false;
            }

            data = zombieData;
            lane = zombieLane;
            registry = combatRegistry;
            homeBoundaryX = boundaryX;
            state = ZombieState.Moving;
            target = null;
            attackTimer = Mathf.Max(0.01f, data.attackInterval);
            deathResolved = false;
            ReachedHome = false;
            AttackCount = 0;
            Health.Configure(data.maxHealth);
            Health.OnDied -= HandleDied;
            Health.OnDied += HandleDied;
            
            GetComponent<LaneDepthVisual>()?.ApplyLane(lane, registry.LaneCount);
registry.RegisterZombie(this);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (data == null || registry == null || state == ZombieState.Dead ||
                Health.IsDead || ReachedHome)
            {
                return;
            }

            if (transform.position.x <= homeBoundaryX)
            {
                ResolveReachedHome();
                return;
            }

            if (!IsValidTarget(target))
            {
                target = registry.GetNearestPlantAhead(lane, transform.position.x);
                state = ZombieState.Moving;
                attackTimer = Mathf.Max(0.01f, data.attackInterval);
            }

            if (target == null)
            {
                MoveLeft(deltaTime, float.NegativeInfinity);
                return;
            }

            float distance = transform.position.x - target.transform.position.x;
            if (distance > data.attackRange)
            {
                state = ZombieState.Moving;
                MoveLeft(deltaTime, target.transform.position.x + data.attackRange);
                return;
            }

            state = ZombieState.Attacking;
            attackTimer -= Mathf.Max(0f, deltaTime);
            if (attackTimer > 0f) return;

            attackTimer += Mathf.Max(0.01f, data.attackInterval);
            target.Health.TakeDamage(data.attackDamage);
            AttackCount++;
            if (!IsValidTarget(target))
            {
                target = null;
                state = ZombieState.Moving;
            }
        }

        private void MoveLeft(float deltaTime, float minimumX)
        {
            float nextX = transform.position.x - data.movementSpeed * Mathf.Max(0f, deltaTime);
            if (!float.IsNegativeInfinity(minimumX)) nextX = Mathf.Max(nextX, minimumX);
            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            if (transform.position.x <= homeBoundaryX) ResolveReachedHome();
        }

        private bool IsValidTarget(PlantBase plant)
        {
            return plant != null && plant.Lane == lane && plant.CurrentCell != null &&
                   plant.Health != null && !plant.Health.IsDead &&
                   plant.transform.position.x <= transform.position.x;
        }

        private void HandleDied(CombatHealth source)
        {
            if (deathResolved) return;
            deathResolved = true;
            state = ZombieState.Dead;
            target = null;
            registry?.UnregisterZombie(this);
            if (Application.isPlaying) Destroy(gameObject);
        }

        private void ResolveReachedHome()
        {
            if (ReachedHome || Health.IsDead) return;
            ReachedHome = true;
            state = ZombieState.Dead;
            target = null;
            registry?.UnregisterZombie(this);
            OnReachedHome?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDied -= HandleDied;
            registry?.UnregisterZombie(this);
        }
    }
}