using System.Collections.Generic;
using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class PlantPlacementController : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private PlantSelectionController selectionController;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private Transform plantParent;

        

        public bool PlacementBlocked { get; private set; }
private readonly Dictionary<PlantData, float> nextReadyTimes = new();

        public PlantBase LastPlacedPlant { get; private set; }

        public void Configure(
            GridManager grid,
            PlantSelectionController selection,
            ResourceManager resources,
            Transform parent = null)
        {
            gridManager = grid;
            selectionController = selection;
            resourceManager = resources;
            plantParent = parent;
        }

        public bool TryPlaceSelectedAtWorld(Vector3 worldPosition, float currentTime)
        {
            if (gridManager == null || selectionController == null ||
                !gridManager.TryWorldToCell(worldPosition, out GridCell cell))
            {
                return false;
            }

            return TryPlace(cell, selectionController.SelectedPlant, currentTime);
        }

        public bool CanPlace(GridCell cell, PlantData data, float currentTime)
        {
            return !PlacementBlocked &&
                   cell != null &&
                   data != null &&
                   !cell.IsOccupied &&
                   resourceManager != null &&
                   data.prefab != null &&
                   resourceManager.CanAfford(data.sunCost) &&
                   IsCooldownReady(data, currentTime);
        }

        public bool TryPlace(GridCell cell, PlantData data, float currentTime)
        {
            LastPlacedPlant = null;

            if (!CanPlace(cell, data, currentTime))
            {
                return false;
            }

            PlantBase plant = Instantiate(data.prefab, cell.WorldPosition, Quaternion.identity, plantParent);
            if (plant == null)
            {
                return false;
            }

            if (!plant.gameObject.activeSelf)
            {
                plant.gameObject.SetActive(true);
            }

            if (!plant.Initialize(data, cell))
            {
                DestroyPlant(plant);
                return false;
            }

            if (!resourceManager.SpendSun(data.sunCost))
            {
                RollbackFailedPlacement(plant);
                return false;
            }

            nextReadyTimes[data] = currentTime + Mathf.Max(0f, data.cooldown);
            LastPlacedPlant = plant;
            return true;
        }

        public bool IsCooldownReady(PlantData data, float currentTime)
        {
            return data != null &&
                   (!nextReadyTimes.TryGetValue(data, out float readyTime) || currentTime >= readyTime);
        }

        public float GetCooldownRemaining(PlantData data, float currentTime)
        {
            if (data == null || !nextReadyTimes.TryGetValue(data, out float readyTime))
            {
                return 0f;
            }

            return Mathf.Max(0f, readyTime - currentTime);
        }

        private static void RollbackFailedPlacement(PlantBase plant)
        {
            plant.ReleaseFromCell();
            DestroyPlant(plant);
        }

        private static void DestroyPlant(PlantBase plant)
        {
            if (Application.isPlaying)
            {
                Destroy(plant.gameObject);
            }
            else
            {
                DestroyImmediate(plant.gameObject);
            }
        }
    

public void SetPlacementBlocked(bool blocked)
        {
            PlacementBlocked = blocked;
            if (blocked) selectionController?.ClearSelection();
        }
}
}
