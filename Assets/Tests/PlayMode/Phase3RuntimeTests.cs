using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PvZ2.Foundation.Tests
{
    public sealed class Phase3RuntimeTests
    {
        private GridManager grid;
        private ResourceManager resources;
        private PlantPlacementController placement;
        private BasicZombieSpawner spawner;
        private LaneCombatRegistry registry;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;
            grid = Object.FindFirstObjectByType<GridManager>();
            resources = Object.FindFirstObjectByType<ResourceManager>();
            placement = Object.FindFirstObjectByType<PlantPlacementController>();
            spawner = Object.FindFirstObjectByType<BasicZombieSpawner>();
            registry = Object.FindFirstObjectByType<LaneCombatRegistry>();
            resources.AddSun(2000);
            Assert.That(grid, Is.Not.Null);
            Assert.That(spawner, Is.Not.Null);
            Assert.That(registry, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SameLanePeashooter_FiresAndDamagesZombie()
        {
            PlantBase plant = Place("peashooter", 2, 2);
            ZombieController zombie = spawner.SpawnNormal(2);
            zombie.transform.position = new Vector3(plant.transform.position.x + 2f, plant.transform.position.y, 0f);
            int startingHealth = zombie.Health.CurrentHealth;

            float deadline = Time.time + 3f;
            while (zombie != null && zombie.Health.CurrentHealth == startingHealth && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(plant.GetComponent<PeashooterController>().FireCount, Is.GreaterThan(0));
            Assert.That(zombie, Is.Not.Null);
            Assert.That(zombie.Health.CurrentHealth, Is.LessThan(startingHealth));
        }

        [UnityTest]
        public IEnumerator Peashooter_IgnoresZombieInOtherLane()
        {
            PlantBase plant = Place("peashooter", 2, 2);
            ZombieController zombie = spawner.SpawnNormal(3);
            zombie.transform.position = new Vector3(plant.transform.position.x + 2f, grid.GetCellCenter(3, 0).y, 0f);
            PeashooterController shooter = plant.GetComponent<PeashooterController>();

            yield return new WaitForSeconds(1.25f);

            Assert.That(shooter.FireCount, Is.Zero);
            Assert.That(zombie.Health.CurrentHealth, Is.EqualTo(zombie.Health.MaxHealth));
        }

        [UnityTest]
        public IEnumerator WallNut_BlocksDiesReleasesCellAndZombieResumes()
        {
            PlantBase wallNut = Place("wall-nut", 2, 3);
            GridCell cell = wallNut.CurrentCell;
            ZombieData fastAttack = Object.Instantiate(spawner.NormalZombie);
            fastAttack.attackDamage = wallNut.Health.MaxHealth;
            fastAttack.attackInterval = 0.05f;
            ZombieController zombie = spawner.SpawnZombie(fastAttack, 2);
            zombie.transform.position = new Vector3(
                wallNut.transform.position.x + fastAttack.attackRange,
                wallNut.transform.position.y,
                0f);

            float deadline = Time.time + 1f;
            while (cell.IsOccupied && Time.time < deadline) yield return null;
            float before = zombie.transform.position.x;
            yield return new WaitForSeconds(0.25f);

            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(zombie.transform.position.x, Is.LessThan(before));
            Object.Destroy(fastAttack);
        }

        [UnityTest]
        public IEnumerator ZombieDeath_UnregistersAndStops()
        {
            ZombieController zombie = spawner.SpawnNormal(1);
            zombie.Health.TakeDamage(zombie.Health.CurrentHealth);
            yield return null;
            Assert.That(registry.GetZombieCount(1), Is.Zero);
            Assert.That(zombie == null || zombie.State == ZombieController.ZombieState.Dead, Is.True);
        }

        [UnityTest]
        public IEnumerator ReachingHome_FiresEventWithoutDeath()
        {
            ZombieController zombie = spawner.SpawnNormal(0);
            int reached = 0;
            int died = 0;
            zombie.OnReachedHome += _ => reached++;
            zombie.Health.OnDied += _ => died++;
            zombie.transform.position = new Vector3(-6.6f, zombie.transform.position.y, 0f);
            yield return null;

            Assert.That(reached, Is.EqualTo(1));
            Assert.That(died, Is.Zero);
            Assert.That(zombie.ReachedHome, Is.True);
            Assert.That(zombie.Health.IsDead, Is.False);
        }

        private PlantBase Place(string id, int lane, int column)
        {
            PlantData data = Resources.FindObjectsOfTypeAll<PlantData>().First(item => item.id == id);
            Assert.That(placement.TryPlace(grid.GetCell(lane, column), data, Time.time), Is.True);
            return placement.LastPlacedPlant;
        }
    }
}
