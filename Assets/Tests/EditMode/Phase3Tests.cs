using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PvZ2.Foundation.Tests
{
    public sealed class Phase3Tests
    {
        private readonly List<Object> cleanup = new();
        private GridManager grid;
        private LaneCombatRegistry registry;
        private PeaProjectile projectilePrefab;

        [SetUp]
        public void SetUp()
        {
            GameObject gridObject = Track(new GameObject("Phase3 Grid"));
            grid = gridObject.AddComponent<GridManager>();
            grid.BuildGrid(5, 9, Vector2.zero, Vector2.one);

            GameObject registryObject = Track(new GameObject("Lane Registry"));
            registry = registryObject.AddComponent<LaneCombatRegistry>();
            registry.Configure(5);

            GameObject projectileObject = Track(new GameObject("Pea Projectile Prefab"));
            projectilePrefab = projectileObject.AddComponent<PeaProjectile>();
            projectileObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--)
            {
                if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
            }

            cleanup.Clear();
        }

        [Test]
        public void CombatHealth_ConfigureAndDamageAreClamped()
        {
            CombatHealth health = Track(new GameObject("Health")).AddComponent<CombatHealth>();
            health.Configure(10);
            Assert.That(health.MaxHealth, Is.EqualTo(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
            health.TakeDamage(4);
            Assert.That(health.CurrentHealth, Is.EqualTo(6));
            health.TakeDamage(99);
            Assert.That(health.CurrentHealth, Is.Zero);
        }

        [Test]
        public void CombatHealth_InvalidDamageDoesNothing()
        {
            CombatHealth health = Track(new GameObject("Health")).AddComponent<CombatHealth>();
            health.Configure(10);
            Assert.That(health.TakeDamage(0), Is.False);
            Assert.That(health.TakeDamage(-5), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void CombatHealth_DeathFiresExactlyOnce()
        {
            CombatHealth health = Track(new GameObject("Health")).AddComponent<CombatHealth>();
            health.Configure(5);
            int deaths = 0;
            health.OnDied += _ => deaths++;
            health.TakeDamage(5);
            health.TakeDamage(5);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void Registry_RegistersPlantAndZombieByLane()
        {
            PlantBase plant = CreatePlant(2, 1);
            ZombieController zombie = CreateZombie(2, 5f);
            Assert.That(registry.GetPlantCount(2), Is.EqualTo(1));
            Assert.That(registry.GetZombieCount(2), Is.EqualTo(1));
            Assert.That(registry.GetNearestZombieAhead(2, plant.transform.position.x), Is.SameAs(zombie));
        }

        [Test]
        public void Registry_IgnoresWrongLaneAndZombieBehind()
        {
            PlantBase plant = CreatePlant(2, 3);
            CreateZombie(3, 6f);
            CreateZombie(2, 1f);
            Assert.That(registry.GetNearestZombieAhead(2, plant.transform.position.x), Is.Null);
        }

        [Test]
        public void Registry_ReturnsNearestPlantAheadForZombie()
        {
            PlantBase left = CreatePlant(1, 1);
            PlantBase nearest = CreatePlant(1, 3);
            Assert.That(registry.GetNearestPlantAhead(1, 5f), Is.SameAs(nearest));
            Assert.That(registry.GetNearestPlantAhead(1, 0.5f), Is.Null);
            Assert.That(left, Is.Not.Null);
        }

        [Test]
        public void Registry_UnregistersCleanly()
        {
            PlantBase plant = CreatePlant(0, 0);
            ZombieController zombie = CreateZombie(0, 4f);
            plant.ReleaseFromCell();
            registry.UnregisterZombie(zombie);
            Assert.That(registry.GetPlantCount(0), Is.Zero);
            Assert.That(registry.GetZombieCount(0), Is.Zero);
        }

        [Test]
        public void Peashooter_NoZombieDoesNotFire()
        {
            PeashooterController shooter = CreatePeashooter(2, 1);
            Assert.That(shooter.Tick(1f), Is.Null);
            Assert.That(shooter.FireCount, Is.Zero);
        }

        [Test]
        public void Peashooter_WrongLaneOrBehindDoesNotFire()
        {
            PeashooterController shooter = CreatePeashooter(2, 3);
            CreateZombie(3, 6f);
            CreateZombie(2, 1f);
            Assert.That(shooter.Tick(1f), Is.Null);
            Assert.That(shooter.FireCount, Is.Zero);
        }

        [Test]
        public void Peashooter_SameLaneAheadFiresAndRespectsInterval()
        {
            PeashooterController shooter = CreatePeashooter(2, 1, 1f);
            CreateZombie(2, 6f);
            Assert.That(shooter.Tick(0f), Is.Not.Null);
            Assert.That(shooter.Tick(0.5f), Is.Null);
            Assert.That(shooter.Tick(0.5f), Is.Not.Null);
            Assert.That(shooter.FireCount, Is.EqualTo(2));
        }

        [Test]
        public void Projectile_MovesRightAndCleansAtBoundary()
        {
            PeaProjectile projectile = CreateProjectile(2, 0f, 10, 2f, 1f);
            projectile.Tick(0.25f);
            Assert.That(projectile.transform.position.x, Is.EqualTo(0.5f).Within(0.001f));
            projectile.Tick(0.3f);
            Assert.That(projectile.Resolved, Is.True);
            Assert.That(projectile.Expired, Is.True);
        }

        [Test]
        public void Projectile_HitsFirstZombieInSegmentExactlyOnce()
        {
            ZombieController first = CreateZombie(2, 2f, 100);
            ZombieController second = CreateZombie(2, 3f, 100);
            PeaProjectile projectile = CreateProjectile(2, 0f, 25, 10f, 8f);
            Assert.That(projectile.Tick(0.4f), Is.SameAs(first));
            Assert.That(first.Health.CurrentHealth, Is.EqualTo(75));
            Assert.That(second.Health.CurrentHealth, Is.EqualTo(100));
            projectile.Tick(1f);
            Assert.That(first.Health.CurrentHealth, Is.EqualTo(75));
        }

        [Test]
        public void Projectile_DoesNotHitWrongLane()
        {
            ZombieController zombie = CreateZombie(3, 2f, 100);
            PeaProjectile projectile = CreateProjectile(2, 0f, 25, 10f, 8f);
            Assert.That(projectile.Tick(0.4f), Is.Null);
            Assert.That(zombie.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Zombie_MovesLeftAndKeepsLane()
        {
            ZombieController zombie = CreateZombie(4, 5f, 100, 1f);
            zombie.Tick(1f);
            Assert.That(zombie.transform.position.x, Is.EqualTo(4f).Within(0.001f));
            Assert.That(zombie.Lane, Is.EqualTo(4));
        }

        [Test]
        public void Zombie_StopsAtNearestPlantAndDoesNotOverlap()
        {
            CreatePlant(2, 1);
            ZombieController zombie = CreateZombie(2, 4f, 100, 10f, 0.5f);
            zombie.Tick(1f);
            Assert.That(zombie.transform.position.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(zombie.State, Is.EqualTo(ZombieController.ZombieState.Moving));
            zombie.Tick(0f);
            Assert.That(zombie.State, Is.EqualTo(ZombieController.ZombieState.Attacking));
        }

        [Test]
        public void Zombie_AttacksNearestPlantAndRespectsInterval()
        {
            PlantBase rear = CreatePlant(1, 1, 100);
            PlantBase nearest = CreatePlant(1, 2, 100);
            ZombieController zombie = CreateZombie(1, 2.5f, 100, 0f, 0.6f, 20, 1f);
            zombie.Tick(0.5f);
            Assert.That(nearest.Health.CurrentHealth, Is.EqualTo(100));
            zombie.Tick(0.5f);
            Assert.That(nearest.Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(rear.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void PlantDeath_ReleasesCellAndZombieResumes()
        {
            PlantBase wallNut = CreatePlant(2, 1, 20);
            GridCell cell = wallNut.CurrentCell;
            ZombieController zombie = CreateZombie(2, 1.5f, 100, 1f, 0.5f, 20, 0.1f);
            zombie.Tick(0.1f);
            Assert.That(wallNut.Health.IsDead, Is.True);
            Assert.That(cell.IsOccupied, Is.False);
            float before = zombie.transform.position.x;
            zombie.Tick(0.5f);
            Assert.That(zombie.transform.position.x, Is.LessThan(before));
        }

        [Test]
        public void ZombieDeath_UnregistersExactlyOnce()
        {
            ZombieController zombie = CreateZombie(0, 3f, 20);
            zombie.Health.TakeDamage(20);
            zombie.Health.TakeDamage(20);
            Assert.That(zombie.State, Is.EqualTo(ZombieController.ZombieState.Dead));
            Assert.That(registry.GetZombieCount(0), Is.Zero);
        }

        [Test]
        public void ReachedHome_IsNotHealthDeath()
        {
            ZombieController zombie = CreateZombie(0, 0f, 100, 1f, 0.5f, 20, 1f, -0.1f);
            int reached = 0;
            int died = 0;
            zombie.OnReachedHome += _ => reached++;
            zombie.Health.OnDied += _ => died++;
            zombie.Tick(1f);
            Assert.That(reached, Is.EqualTo(1));
            Assert.That(died, Is.Zero);
            Assert.That(zombie.ReachedHome, Is.True);
            Assert.That(zombie.Health.IsDead, Is.False);
        }

        [Test]
        public void WallNut_BlocksTakesDamageDiesAndReleasesCell()
        {
            PlantBase wallNut = CreatePlant(3, 2, 40);
            GridCell cell = wallNut.CurrentCell;
            ZombieController zombie = CreateZombie(3, 2.5f, 100, 1f, 0.6f, 20, 0.5f);
            zombie.Tick(0.5f);
            Assert.That(zombie.transform.position.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(wallNut.Health.CurrentHealth, Is.EqualTo(20));
            zombie.Tick(0.5f);
            Assert.That(wallNut.Health.IsDead, Is.True);
            Assert.That(cell.IsOccupied, Is.False);
            float before = zombie.transform.position.x;
            zombie.Tick(0.5f);
            Assert.That(zombie.transform.position.x, Is.LessThan(before));
        }

        private PeashooterController CreatePeashooter(int lane, int column, float interval = 1f)
        {
            PlantBase plant = CreatePlant(lane, column, 100, 20, interval, true);
            PeashooterController controller = plant.GetComponent<PeashooterController>();
            controller.Configure(projectilePrefab, registry, null, 5f, 8f);
            return controller;
        }

        private PlantBase CreatePlant(int lane, int column, int health = 100,
            int damage = 0, float interval = 1f, bool peashooter = false)
        {
            GameObject go = Track(new GameObject(peashooter ? "Peashooter" : "Plant"));
            PlantBase plant = go.AddComponent<PlantBase>();
            if (peashooter) go.AddComponent<PeashooterController>();
            PlantData data = Track(ScriptableObject.CreateInstance<PlantData>());
            data.displayName = peashooter ? "Peashooter" : "Plant";
            data.maxHealth = health;
            data.attackDamage = damage;
            data.attackInterval = interval;
            Assert.That(plant.Initialize(data, grid.GetCell(lane, column), registry), Is.True);
            return plant;
        }

        private ZombieController CreateZombie(int lane, float x, int health = 100,
            float speed = 1f, float range = 0.5f, int damage = 20, float interval = 1f,
            float boundary = -10f)
        {
            GameObject go = Track(new GameObject("Zombie"));
            go.transform.position = new Vector3(x, lane, 0f);
            ZombieController zombie = go.AddComponent<ZombieController>();
            ZombieData data = Track(ScriptableObject.CreateInstance<ZombieData>());
            data.displayName = "Zombie";
            data.maxHealth = health;
            data.movementSpeed = speed;
            data.attackRange = range;
            data.attackDamage = damage;
            data.attackInterval = interval;
            Assert.That(zombie.Initialize(data, lane, registry, boundary), Is.True);
            return zombie;
        }

        private PeaProjectile CreateProjectile(int lane, float x, int damage, float speed, float boundary)
        {
            GameObject go = Track(new GameObject("Projectile"));
            go.transform.position = new Vector3(x, lane, 0f);
            PeaProjectile projectile = go.AddComponent<PeaProjectile>();
            projectile.Initialize(lane, damage, speed, registry, boundary);
            return projectile;
        }

        private T Track<T>(T value) where T : Object
        {
            cleanup.Add(value);
            return value;
        }
    }
}