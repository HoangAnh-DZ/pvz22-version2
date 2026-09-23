using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int rows = 5;
        [SerializeField, Min(1)] private int columns = 9;
        [SerializeField] private Vector2 origin = new(-5.75f, -2.1f);
        [SerializeField] private Vector2 cellSize = new(1.25f, 1.05f);
        [SerializeField] private BattlefieldProjection projection;
        private GridCell[,] cells;
        public int Rows => rows;
        public int Columns => columns;
        public Vector2 Origin => origin;
        public Vector2 CellSize => cellSize;
        public BattlefieldProjection Projection => projection;
        void Awake() => RebuildLookup();
        public GridCell GetCell(int row, int column)
        {
            EnsureLookup();
            return row < 0 || row >= rows || column < 0 || column >= columns ? null : cells[row, column];
        }
        public Vector3 GetCellCenter(int row, int column)
        {
            if (row < 0 || row >= rows || column < 0 || column >= columns) return Vector3.positiveInfinity;
            if (projection != null) return projection.GetCellCenter(row, column, rows, columns);
            return new Vector3(origin.x + column * cellSize.x, origin.y + row * cellSize.y, transform.position.z);
        }
        public bool TryWorldToCell(Vector3 worldPosition, out GridCell cell)
        {
            EnsureLookup();
            if (projection != null)
            {
                for (int r = 0; r < rows; r++)
                {
                    Vector2 size = projection.GetCellSize(r, rows);
                    for (int c = 0; c < columns; c++)
                    {
                        GridCell candidate = GetCell(r, c);
                        if (candidate == null) continue;
                        Vector3 center = candidate.WorldPosition;
                        if (Mathf.Abs(worldPosition.x - center.x) <= size.x * .5f && Mathf.Abs(worldPosition.y - center.y) <= size.y * .5f)
                        { cell = candidate; return true; }
                    }
                }
                cell = null; return false;
            }
            int column = Mathf.FloorToInt((worldPosition.x - origin.x + cellSize.x * .5f) / cellSize.x);
            int row = Mathf.FloorToInt((worldPosition.y - origin.y + cellSize.y * .5f) / cellSize.y);
            cell = GetCell(row, column);
            return cell != null;
        }
        public void BuildGrid(int newRows, int newColumns, Vector2 newOrigin, Vector2 newCellSize, Sprite cellSprite = null)
        {
            rows = Mathf.Max(1, newRows); columns = Mathf.Max(1, newColumns); origin = newOrigin;
            cellSize = new Vector2(Mathf.Max(.01f, newCellSize.x), Mathf.Max(.01f, newCellSize.y)); projection = null;
            ClearCells(); cells = new GridCell[rows, columns];
            for (int r = 0; r < rows; r++) for (int c = 0; c < columns; c++) CreateCell(r, c, GetCellCenter(r, c), cellSize, cellSprite);
        }
        public void BuildProjectedGrid(int newRows, int newColumns, BattlefieldProjection battlefieldProjection, Sprite cellSprite = null)
        {
            rows = Mathf.Max(1, newRows); columns = Mathf.Max(1, newColumns); projection = battlefieldProjection;
            cellSize = projection.BaseCellSize; origin = projection.GetCellCenter(0, 0, rows, columns);
            ClearCells(); cells = new GridCell[rows, columns];
            for (int r = 0; r < rows; r++) for (int c = 0; c < columns; c++)
                CreateCell(r, c, projection.GetCellCenter(r, c, rows, columns), projection.GetCellSize(r, rows), cellSprite);
        }
        void CreateCell(int row, int column, Vector3 center, Vector2 size, Sprite sprite)
        {
            GameObject go = new($"Cell_{row}_{column}"); go.transform.SetParent(transform, true); go.transform.position = center;
            GridCell cell = go.AddComponent<GridCell>(); cell.Configure(row, column, center); cells[row, column] = cell;
            if (sprite == null) return;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            go.transform.localScale = new Vector3(size.x * .992f, size.y * .985f, 1f);
        }
        void ClearCells()
        {
            GridCell[] existing = GetComponentsInChildren<GridCell>(true);
            for (int i = existing.Length - 1; i >= 0; i--)
                if (Application.isPlaying) Destroy(existing[i].gameObject); else DestroyImmediate(existing[i].gameObject);
        }
        public float GetLaneLeftBoundary(int lane, float margin = 0f) => projection != null ? projection.GetLeftBoundary(lane, rows, columns, margin) : GetCellCenter(lane, 0).x - cellSize.x * .5f - margin;
        public float GetLaneRightBoundary(int lane, float margin = 0f) => projection != null ? projection.GetRightBoundary(lane, rows, columns, margin) : GetCellCenter(lane, columns - 1).x + cellSize.x * .5f + margin;
        public void RebuildLookup()
        {
            cells = new GridCell[rows, columns];
            foreach (GridCell cell in GetComponentsInChildren<GridCell>(true))
                if (cell.Row >= 0 && cell.Row < rows && cell.Column >= 0 && cell.Column < columns) cells[cell.Row, cell.Column] = cell;
        }
        void EnsureLookup() { if (cells == null || cells.GetLength(0) != rows || cells.GetLength(1) != columns) RebuildLookup(); }
    }
}