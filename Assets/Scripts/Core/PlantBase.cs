using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class PlantBase : MonoBehaviour
    {
        [SerializeField] private PlantData data;
        [SerializeField] private GridCell currentCell;

        public PlantData Data => data;
        public GridCell CurrentCell => currentCell;
        public int Lane => currentCell != null ? currentCell.Row : -1;

        public bool Initialize(PlantData plantData, GridCell cell)
        {
            if (plantData == null || cell == null || currentCell != null)
            {
                return false;
            }

            if (!cell.TryOccupy(this))
            {
                return false;
            }

            data = plantData;
            currentCell = cell;
            transform.position = cell.WorldPosition;
            return true;
        }

        public void Die()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (currentCell != null)
            {
                currentCell.Release(this);
                currentCell = null;
            }
        }
    }
}
