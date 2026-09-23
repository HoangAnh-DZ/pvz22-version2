using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int rows = 5;
        [SerializeField, Min(1)] private int columns = 9;
        [SerializeField] private Vector2 origin = new(-5.75f, -2.1f);
        [SerializeField] private Vector2 cellSize = new(1.25f, 1.05f);

        private GridCell[,] cells;

        public int Rows => rows;
        public int Columns => columns;
        public Vector2 Origin => origin;
        public Vector2 CellSize => cellSize;

        private void Awake()
        {
            RebuildLookup();
        }

        public GridCell GetCell(int row, int column)
        {
            EnsureLookup();
            if (row < 0 || row >= rows || column < 0 || column >= columns)
            {
                return null;
            }

            return cells[row, column];
        }

        public Vector3 GetCellCenter(int row, int column)
        {
            if (row < 0 || row >= rows || column < 0 || column >= columns)
            {
                return Vector3.positiveInfinity;
            }

            return new Vector3(
                origin.x + column * cellSize.x,
                origin.y + row * cellSize.y,
                transform.position.z);
        }

        public bool TryWorldToCell(Vector3 worldPosition, out GridCell cell)
        {
            EnsureLookup();

            float localX = worldPosition.x - origin.x + cellSize.x * 0.5f;
            float localY = worldPosition.y - origin.y + cellSize.y * 0.5f;
            int column = Mathf.FloorToInt(localX / cellSize.x);
            int row = Mathf.FloorToInt(localY / cellSize.y);

            cell = GetCell(row, column);
            return cell != null;
        }

        public void BuildGrid(int newRows, int newColumns, Vector2 newOrigin, Vector2 newCellSize, Sprite cellSprite = null)
        {
            rows = Mathf.Max(1, newRows);
            columns = Mathf.Max(1, newColumns);
            origin = newOrigin;
            cellSize = new Vector2(Mathf.Max(0.01f, newCellSize.x), Mathf.Max(0.01f, newCellSize.y));

            GridCell[] existing = GetComponentsInChildren<GridCell>(true);
            for (int i = existing.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing[i].gameObject);
                }
                else
                {
                    DestroyImmediate(existing[i].gameObject);
                }
            }

            cells = new GridCell[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GameObject cellObject = new($"Cell_{row}_{column}");
                    cellObject.transform.SetParent(transform, true);

                    GridCell cell = cellObject.AddComponent<GridCell>();
                    cell.Configure(row, column, GetCellCenter(row, column));
                    cells[row, column] = cell;

                    if (cellSprite != null)
                    {
                        SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
                        renderer.sprite = cellSprite;
                        cellObject.transform.localScale = new Vector3(cellSize.x * 0.98f, cellSize.y * 0.98f, 1f);
                    }
                }
            }
        }

        public void RebuildLookup()
        {
            cells = new GridCell[rows, columns];
            GridCell[] foundCells = GetComponentsInChildren<GridCell>(true);
            foreach (GridCell cell in foundCells)
            {
                if (cell.Row >= 0 && cell.Row < rows && cell.Column >= 0 && cell.Column < columns)
                {
                    cells[cell.Row, cell.Column] = cell;
                }
            }
        }

        private void EnsureLookup()
        {
            if (cells == null || cells.GetLength(0) != rows || cells.GetLength(1) != columns)
            {
                RebuildLookup();
            }
        }
    }
}
