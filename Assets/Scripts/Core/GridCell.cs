using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class GridCell : MonoBehaviour
    {
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private PlantBase currentPlant;

        public int Row => row;
        public int Column => column;
        public Vector3 WorldPosition => worldPosition;
        public PlantBase CurrentPlant
        {
            get
            {
                if (currentPlant == null)
                {
                    currentPlant = null;
                }

                return currentPlant;
            }
        }

        public bool IsOccupied => CurrentPlant != null;


        public void Configure(int newRow, int newColumn, Vector3 center)
        {
            row = newRow;
            column = newColumn;
            worldPosition = center;
            transform.position = center;
        }

        public bool TryOccupy(PlantBase plant)
        {
            if (plant == null || IsOccupied)
            {
                return false;
            }

            currentPlant = plant;
            return true;
        }

        internal void Release(PlantBase plant)
        {
            if (currentPlant == plant)
            {
                currentPlant = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            worldPosition = transform.position;
        }
#endif
    }
}
