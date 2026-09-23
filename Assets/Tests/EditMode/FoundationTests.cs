using NUnit.Framework;
using UnityEngine;

namespace PvZ2.Foundation.Tests
{
    public sealed class FoundationTests
    {
        private GameObject gridObject;
        private GameObject systemsObject;
        private GameObject prefabObject;
        private GridManager grid;
        private ResourceManager resources;
        private PlantSelectionController selection;
        private PlantPlacementController placement;
        private PlantData data;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("Grid");
            grid = gridObject.AddComponent<GridManager>();
            grid.BuildGrid(5, 9, new Vector2(-4f, -2f), Vector2.one);

            systemsObject = new GameObject("Systems");
            resources = systemsObject.AddComponent<ResourceManager>();
            resources.ConfigureStartingSun(250);
            selection = systemsObject.AddComponent<PlantSelectionController>();
            placement = systemsObject.AddComponent<PlantPlacementController>();
            placement.Configure(grid, selection, resources);

            prefabObject = new GameObject("PlantPrefab");
            prefabObject.SetActive(false);
            PlantBase prefab = prefabObject.AddComponent<PlantBase>();

            data = ScriptableObject.CreateInstance<PlantData>();
            data.id = "test-plant";
            data.displayName = "Test Plant";
            data.prefab = prefab;
            data.sunCost = 50;
            data.cooldown = 2f;
            data.maxHealth = 100;
        }

        [TearDown]
        public void TearDown()
        {
            if (gridObject != null)
            {
                Object.DestroyImmediate(gridObject);
            }

            if (systemsObject != null)
            {
                Object.DestroyImmediate(systemsObject);
            }

            if (prefabObject != null)
            {
                Object.DestroyImmediate(prefabObject);
            }

            if (data != null)
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Grid_HasFiveByNineCells()
        {
            Assert.That(grid.Rows, Is.EqualTo(5));
            Assert.That(grid.Columns, Is.EqualTo(9));

            int count = 0;
            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    Assert.That(grid.GetCell(row, column), Is.Not.Null);
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(45));
        }

        [Test]
        public void WorldPosition_MapsToCorrectCell()
        {
            Vector3 center = grid.GetCellCenter(3, 7);

            bool mapped = grid.TryWorldToCell(center, out GridCell cell);

            Assert.That(mapped, Is.True);
            Assert.That(cell.Row, Is.EqualTo(3));
            Assert.That(cell.Column, Is.EqualTo(7));
        }

        [Test]
        public void OccupiedCell_RejectsPlacement()
        {
            GridCell cell = grid.GetCell(0, 0);
            Assert.That(placement.TryPlace(cell, data, 0f), Is.True);
            int sunAfterFirst = resources.CurrentSun;

            PlantData secondData = ScriptableObject.CreateInstance<PlantData>();
            secondData.prefab = data.prefab;
            secondData.sunCost = 25;
            secondData.cooldown = 0f;
            bool result = placement.TryPlace(cell, secondData, 10f);

            Assert.That(result, Is.False);
            Assert.That(resources.CurrentSun, Is.EqualTo(sunAfterFirst));
            Object.DestroyImmediate(secondData);
        }

        [Test]
        public void ValidPlacement_OccupiesCell()
        {
            GridCell cell = grid.GetCell(2, 4);

            bool result = placement.TryPlace(cell, data, 0f);

            Assert.That(result, Is.True);
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(cell.CurrentPlant, Is.EqualTo(placement.LastPlacedPlant));
            Assert.That(cell.CurrentPlant.CurrentCell, Is.EqualTo(cell));
            Assert.That(cell.CurrentPlant.Lane, Is.EqualTo(2));
        }

        [Test]
        public void ValidPlacement_SpendsCorrectSun()
        {
            int before = resources.CurrentSun;

            bool result = placement.TryPlace(grid.GetCell(1, 1), data, 0f);

            Assert.That(result, Is.True);
            Assert.That(resources.CurrentSun, Is.EqualTo(before - data.sunCost));
        }

        [Test]
        public void InsufficientSun_ChangesNoState()
        {
            resources.SpendSun(resources.CurrentSun);
            GridCell cell = grid.GetCell(1, 2);

            bool result = placement.TryPlace(cell, data, 0f);

            Assert.That(result, Is.False);
            Assert.That(resources.CurrentSun, Is.Zero);
            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(placement.IsCooldownReady(data, 0f), Is.True);
        }

        [Test]
        public void Cooldown_BlocksRepeatedPlacement()
        {
            Assert.That(placement.TryPlace(grid.GetCell(0, 0), data, 10f), Is.True);
            int sunAfterFirst = resources.CurrentSun;
            GridCell secondCell = grid.GetCell(0, 1);

            bool result = placement.TryPlace(secondCell, data, 11f);

            Assert.That(result, Is.False);
            Assert.That(secondCell.IsOccupied, Is.False);
            Assert.That(resources.CurrentSun, Is.EqualTo(sunAfterFirst));
            Assert.That(placement.GetCooldownRemaining(data, 11f), Is.EqualTo(1f));
        }

        [Test]
        public void DestroyingPlant_ReleasesCell()
        {
            GridCell cell = grid.GetCell(4, 8);
            Assert.That(placement.TryPlace(cell, data, 0f), Is.True);
            PlantBase plant = cell.CurrentPlant;

            Object.DestroyImmediate(plant.gameObject);

            Assert.That(cell.CurrentPlant, Is.Null);
            Assert.That(cell.IsOccupied, Is.False);
        }

        [Test]
        public void InvalidPrefab_DoesNotSpendSun()
        {
            data.prefab = null;
            int before = resources.CurrentSun;
            GridCell cell = grid.GetCell(3, 3);

            bool result = placement.TryPlace(cell, data, 0f);

            Assert.That(result, Is.False);
            Assert.That(resources.CurrentSun, Is.EqualTo(before));
            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(placement.IsCooldownReady(data, 0f), Is.True);
        }
    }
}
