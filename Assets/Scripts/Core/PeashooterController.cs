using UnityEngine;

namespace PvZ2.Foundation
{
    [RequireComponent(typeof(PlantBase))]
    public sealed class PeashooterController : MonoBehaviour
    {
        [SerializeField] private PeaProjectile projectilePrefab;
        [SerializeField] private Transform projectileParent;
        [SerializeField] private Vector3 spawnOffset = new(0.58f, 0.18f, 0f);
        [SerializeField, Min(0.01f)] private float projectileSpeed = 5f;
        [SerializeField] private float rightBoundaryX = 6.5f;

        private PlantBase plant;
        private LaneCombatRegistry registry;
        private float shotTimer;

        public int FireCount { get; private set; }
        public PeaProjectile LastProjectile { get; private set; }

        private void Awake()
        {
            plant = GetComponent<PlantBase>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(PeaProjectile prefab, LaneCombatRegistry combatRegistry,
            Transform parent = null, float speed = 5f, float boundaryX = 6.5f)
        {
            projectilePrefab = prefab;
            registry = combatRegistry;
            projectileParent = parent;
            projectileSpeed = Mathf.Max(0.01f, speed);
            rightBoundaryX = boundaryX;
            plant ??= GetComponent<PlantBase>();
            shotTimer = 0f;
        }

        public PeaProjectile Tick(float deltaTime)
        {
            plant ??= GetComponent<PlantBase>();
            registry ??= plant != null ? plant.Registry : LaneCombatRegistry.Active;
            if (plant == null || plant.Data == null || plant.CurrentCell == null ||
                plant.Health == null || plant.Health.IsDead || registry == null || projectilePrefab == null)
            {
                return null;
            }

            ZombieController target = registry.GetNearestZombieAhead(plant.Lane, transform.position.x);
            if (target == null)
            {
                shotTimer = 0f;
                return null;
            }

            shotTimer -= Mathf.Max(0f, deltaTime);
            if (shotTimer > 0f) return null;

            shotTimer = Mathf.Max(0.01f, plant.Data.attackInterval);
            Vector3 spawnPosition = transform.position + spawnOffset;
            PeaProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity, projectileParent);
            if (!projectile.gameObject.activeSelf) projectile.gameObject.SetActive(true);
            projectile.Initialize(plant.Lane, plant.Data.attackDamage, projectileSpeed, registry, rightBoundaryX);
            LastProjectile = projectile;
            FireCount++;
            return projectile;
        }
    }
}