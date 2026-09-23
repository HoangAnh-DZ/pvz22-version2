using System;
using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class PlantSelectionController : MonoBehaviour
    {
        [SerializeField] private PlantData selectedPlant;

        public PlantData SelectedPlant => selectedPlant;
        public event Action<PlantData> OnSelectionChanged;

        public void SelectPlant(PlantData plantData)
        {
            if (selectedPlant == plantData)
            {
                return;
            }

            selectedPlant = plantData;
            OnSelectionChanged?.Invoke(selectedPlant);
        }

        public void ClearSelection()
        {
            SelectPlant(null);
        }
    }
}
