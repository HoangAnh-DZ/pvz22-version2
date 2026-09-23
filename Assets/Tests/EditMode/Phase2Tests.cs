using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PvZ2.Foundation.Tests
{
    public sealed class Phase2Tests
    {
        private GameObject gridObject;
        private GameObject systemsObject;
        private GameObject plantPrefabObject;
        private GameObject sunPrefabObject;
        private GameObject plantParentObject;
        private GameObject sunParentObject;
        private GridManager grid;
        private ResourceManager resources;
        private PlantSelectionController selection;
        private PlantPlacementController placement;
        private PlantData plantData;
        private SunCollectible sunPrefab;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("Phase2 Grid");
            grid = gridObject.AddComponent<GridManager>();
            grid.BuildGrid(5, 9, new Vector2(-4f, -2f), Vector2.one);

            systemsObject = new GameObject("Phase2 Systems");
            resources = systemsObject.AddComponent<ResourceManager>();
            resources.ConfigureStartingSun(100);
            selection = systemsObject.AddComponent<PlantSelectionController>();
            placement = systemsObject.AddComponent<PlantPlacementController>();

            plantParentObject = new GameObject("Phase2 Plants");
            sunParentObject = new GameObject("Phase2 Suns");
            placement.Configure(grid, selection, resources, plantParentObject.transform);

            plantPrefabObject = new GameObject("Phase2 Plant Prefab");
            plantPrefabObject.SetActive(false);
            PlantBase plantPrefab = plantPrefabObject.AddComponent<PlantBase>();

            sunPrefabObject = new GameObject("Phase2 Sun Prefab");
            sunPrefabObject.SetActive(false);
            sunPrefab = sunPrefabObject.AddComponent<SunCollectible>();
            sunPrefabObject.AddComponent<CircleCollider2D>().isTrigger = true;

            plantData = ScriptableObject.CreateInstance<PlantData>();
            plantData.id = "phase2-test-plant";
            plantData.displayName = "Phase2 Plant";
            plantData.prefab = plantPrefab;
            plantData.sunCost = 50;
            plantData.cooldown = 2f;
            plantData.maxHealth = 100;
            selection.SelectPlant(plantData);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyImmediate(gridObject);
            DestroyImmediate(systemsObject);
            DestroyImmediate(plantParentObject);
            DestroyImmediate(sunParentObject);
            DestroyImmediate(plantPrefabObject);
            DestroyImmediate(sunPrefabObject);
            if (plantData != null)
            {
                Object.DestroyImmediate(plantData);
            }
        }

        [Test]
        public void Resource_AddPositive_ChangesValueAndRaisesEvent()
        {
            int eventCount = 0;
            int reportedValue = -1;
            resources.OnSunChanged += value =>
            {
                eventCount++;
                reportedValue = value;
            };

            resources.AddSun(50);

            Assert.That(resources.CurrentSun, Is.EqualTo(150));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(reportedValue, Is.EqualTo(150));
        }

        [Test]
        public void Resource_AddAtLimit_NeverBecomesNegative()
        {
            resources.ConfigureStartingSun(int.MaxValue - 25);
            int eventCount = 0;
            resources.OnSunChanged += _ => eventCount++;

            resources.AddSun(50);
            resources.AddSun(50);

            Assert.That(resources.CurrentSun, Is.EqualTo(int.MaxValue));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void Resource_AddNonPositive_ChangesNothingAndRaisesNoEvent()
        {
            int eventCount = 0;
            resources.OnSunChanged += _ => eventCount++;

            resources.AddSun(0);
            resources.AddSun(-25);

            Assert.That(resources.CurrentSun, Is.EqualTo(100));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void Resource_SpendSuccess_ChangesValueAndRaisesEvent()
        {
            int eventCount = 0;
            int reportedValue = -1;
            resources.OnSunChanged += value =>
            {
                eventCount++;
                reportedValue = value;
            };

            bool result = resources.SpendSun(40);

            Assert.That(result, Is.True);
            Assert.That(resources.CurrentSun, Is.EqualTo(60));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(reportedValue, Is.EqualTo(60));
        }

        [Test]
        public void Resource_FailedSpend_ChangesNothingAndRaisesNoEvent()
        {
            int eventCount = 0;
            resources.OnSunChanged += _ => eventCount++;

            bool result = resources.SpendSun(101);

            Assert.That(result, Is.False);
            Assert.That(resources.CurrentSun, Is.EqualTo(100));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void Collectible_SpawnDoesNotRewardSun()
        {
            resources.ConfigureStartingSun(0);
            SunCollectible sun = CreateSun(Vector3.up, Vector3.zero);

            Assert.That(resources.CurrentSun, Is.Zero);
            Assert.That(sun.State, Is.EqualTo(SunCollectibleState.Moving));
        }

        [Test]
        public void Collectible_CollectRewardsExactlyOnce()
        {
            resources.ConfigureStartingSun(0);
            SunCollectible sun = CreateSun(Vector3.zero, Vector3.zero);

            bool first = sun.Collect();
            bool second = sun.Collect();

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(resources.CurrentSun, Is.EqualTo(50));
            Assert.That(sun.State, Is.EqualTo(SunCollectibleState.Collected));
        }

        [Test]
        public void Collectible_MovesDeterministicallyThenRests()
        {
            SunCollectible sun = CreateSun(new Vector3(0f, 1f, 0f), Vector3.zero, 2f, 8f);

            sun.Tick(0.25f);
            Assert.That(sun.transform.position.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sun.State, Is.EqualTo(SunCollectibleState.Moving));

            sun.Tick(0.25f);
            Assert.That(sun.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(sun.State, Is.EqualTo(SunCollectibleState.Resting));
        }

        [Test]
        public void Collectible_ExpiresWithoutReward()
        {
            resources.ConfigureStartingSun(0);
            SunCollectible sun = CreateSun(Vector3.zero, Vector3.zero, 2f, 0.5f);

            sun.Tick(0.5f);

            Assert.That(sun.State, Is.EqualTo(SunCollectibleState.Expired));
            Assert.That(resources.CurrentSun, Is.Zero);
            Assert.That(sun.Collect(), Is.False);
        }

        [Test]
        public void SkySun_DestinationsStayInsideGrid()
        {
            SkySunSpawner spawner = systemsObject.AddComponent<SkySunSpawner>();
            spawner.Configure(grid, resources, sunPrefab, sunParentObject.transform, 1f, 2f);

            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    Vector3 destination = spawner.GetDestination(row, column);
                    Assert.That(grid.TryWorldToCell(destination, out GridCell mapped), Is.True);
                    Assert.That(mapped.Row, Is.EqualTo(row));
                    Assert.That(mapped.Column, Is.EqualTo(column));
                }
            }
        }

        [Test]
        public void SkySun_RespectsIntervalAndDoesNotRewardOnSpawn()
        {
            resources.ConfigureStartingSun(0);
            SkySunSpawner spawner = systemsObject.AddComponent<SkySunSpawner>();
            spawner.Configure(grid, resources, sunPrefab, sunParentObject.transform, 1f, 2f);

            Assert.That(spawner.Tick(0.9f), Is.Null);
            SunCollectible spawned = spawner.Tick(0.11f);

            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawner.SpawnCount, Is.EqualTo(1));
            Assert.That(resources.CurrentSun, Is.Zero);
            Assert.That(grid.TryWorldToCell(spawned.Destination, out _), Is.True);
        }

        [Test]
        public void Sunflower_ProducesOnlyAfterConfiguredDelay()
        {
            resources.ConfigureStartingSun(0);
            SunflowerProducer producer = CreatePlacedSunflower(0.5f, 1f);

            Assert.That(producer.Tick(0.4f), Is.Null);
            SunCollectible produced = producer.Tick(0.11f);

            Assert.That(produced, Is.Not.Null);
            Assert.That(producer.ProductionCount, Is.EqualTo(1));
            Assert.That(resources.CurrentSun, Is.Zero);
        }

        [Test]
        public void Sunflower_ProducedSunRewardsOnlyWhenCollected()
        {
            resources.ConfigureStartingSun(0);
            SunflowerProducer producer = CreatePlacedSunflower(0.1f, 1f);

            SunCollectible produced = producer.Tick(0.11f);
            Assert.That(resources.CurrentSun, Is.Zero);

            produced.Collect();

            Assert.That(resources.CurrentSun, Is.EqualTo(50));
        }

        [Test]
        public void Sunflower_StopsWhenPlantIsInactiveOrUnplaced()
        {
            GameObject plantObject = new GameObject("Inactive Sunflower");
            plantObject.transform.SetParent(plantParentObject.transform);
            plantObject.AddComponent<PlantBase>();
            SunflowerProducer producer = plantObject.AddComponent<SunflowerProducer>();
            producer.Configure(sunPrefab, resources, sunParentObject.transform, 0.1f, 1f);

            Assert.That(producer.Tick(1f), Is.Null);

            plantObject.SetActive(false);
            Assert.That(producer.Tick(1f), Is.Null);
            Assert.That(producer.ProductionCount, Is.Zero);
        }

        [Test]
        public void InputPriority_SunClickIsConsumedBeforeBoardPlacement()
        {
            resources.ConfigureStartingSun(100);
            GridCell cell = grid.GetCell(2, 3);
            SunCollectible sun = CreateSun(cell.WorldPosition, cell.WorldPosition);
            sun.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();

            BoardInteractionRouter router = systemsObject.AddComponent<BoardInteractionRouter>();
            router.Configure(placement);
            BoardInteractionResult result = router.HandleWorldClick(cell.WorldPosition, 0f);

            Assert.That(result, Is.EqualTo(BoardInteractionResult.SunCollected));
            Assert.That(resources.CurrentSun, Is.EqualTo(150));
            Assert.That(cell.IsOccupied, Is.False);
        }

        [Test]
        public void FailedSpendRollback_ReleasesCellImmediately()
        {
            GridCell cell = grid.GetCell(1, 1);
            GameObject plantObject = new GameObject("Unpaid Plant");
            plantObject.transform.SetParent(plantParentObject.transform);
            PlantBase plant = plantObject.AddComponent<PlantBase>();
            Assert.That(plant.Initialize(plantData, cell), Is.True);

            MethodInfo rollback = typeof(PlantPlacementController).GetMethod(
                "RollbackFailedPlacement",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(rollback, Is.Not.Null);
            rollback.Invoke(null, new object[] { plant });

            Assert.That(cell.CurrentPlant, Is.Null);
            Assert.That(cell.IsOccupied, Is.False);
        }

        private SunCollectible CreateSun(
            Vector3 start,
            Vector3 destination,
            float speed = 2f,
            float lifetime = 8f)
        {
            GameObject sunObject = new GameObject("Test Sun");
            sunObject.transform.SetParent(sunParentObject.transform);
            SunCollectible sun = sunObject.AddComponent<SunCollectible>();
            sun.Configure(resources, 50, start, destination, speed, lifetime);
            return sun;
        }

        private SunflowerProducer CreatePlacedSunflower(float delay, float interval)
        {
            GameObject plantObject = new GameObject("Placed Sunflower");
            plantObject.transform.SetParent(plantParentObject.transform);
            PlantBase plant = plantObject.AddComponent<PlantBase>();
            SunflowerProducer producer = plantObject.AddComponent<SunflowerProducer>();
            Assert.That(plant.Initialize(plantData, grid.GetCell(0, 0)), Is.True);
            producer.Configure(sunPrefab, resources, sunParentObject.transform, delay, interval);
            return producer;
        }

        private static void DestroyImmediate(GameObject target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
