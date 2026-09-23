using UnityEngine;
namespace PvZ2.Foundation
{
    public sealed class BattlefieldProjection : MonoBehaviour
    {
        [SerializeField] Vector2 center = new(-0.75f, 0f);
        [SerializeField] Vector2 baseCellSize = new(1.25f, 1.02f);
        [SerializeField, Range(.75f, 1f)] float upperScale = .9f;
        [SerializeField, Range(1f, 1.25f)] float lowerScale = 1.08f;
        public Vector2 BaseCellSize => baseCellSize;
        public void Configure(Vector2 boardCenter, Vector2 cellSize, float topScale = .9f, float bottomScale = 1.08f)
        { center = boardCenter; baseCellSize = cellSize; upperScale = topScale; lowerScale = bottomScale; }
        public float GetDepthScale(int row, int rows) => rows <= 1 ? 1f : Mathf.Lerp(lowerScale, upperScale, Mathf.Clamp01(row / (rows - 1f)));
        public Vector3 GetCellCenter(int row, int column, int rows, int columns)
        {
            float s = GetDepthScale(row, rows);
            return new Vector3(center.x + (column - (columns - 1) * .5f) * baseCellSize.x * s,
                center.y + (row - (rows - 1) * .5f) * baseCellSize.y, transform.position.z);
        }
        public Vector2 GetCellSize(int row, int rows)
        {
            float t = rows <= 1 ? .5f : Mathf.Clamp01(row / (rows - 1f));
            return new Vector2(baseCellSize.x * GetDepthScale(row, rows), baseCellSize.y * Mathf.Lerp(1.04f, .96f, t));
        }
        public float GetLeftBoundary(int row, int rows, int columns, float margin = 0f) => GetCellCenter(row, 0, rows, columns).x - GetCellSize(row, rows).x * .5f - margin;
        public float GetRightBoundary(int row, int rows, int columns, float margin = 0f) => GetCellCenter(row, columns - 1, rows, columns).x + GetCellSize(row, rows).x * .5f + margin;
    }
}