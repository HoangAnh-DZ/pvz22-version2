using UnityEngine;
namespace PvZ2.Foundation
{
    public sealed class BattlefieldProjection : MonoBehaviour
    {
        [SerializeField] Vector2 center = new(-0.55f, 0f);
        [SerializeField] Vector2 baseCellSize = new(0.9f, 1.12f);
        public Vector2 BaseCellSize => baseCellSize;
        public void Configure(Vector2 boardCenter, Vector2 cellSize, float unusedTopScale = 1f, float unusedBottomScale = 1f)
        {
            center = boardCenter;
            baseCellSize = new Vector2(Mathf.Max(.01f, cellSize.x), Mathf.Max(.01f, cellSize.y));
        }
        public float GetDepthScale(int row, int rows) => 1f;
        public Vector3 GetCellCenter(int row, int column, int rows, int columns)
        {
            return new Vector3(center.x + (column - (columns - 1) * .5f) * baseCellSize.x,
                center.y + (row - (rows - 1) * .5f) * baseCellSize.y, transform.position.z);
        }
        public Vector2 GetCellSize(int row, int rows) => baseCellSize;
        public float GetLeftBoundary(int row, int rows, int columns, float margin = 0f) => GetCellCenter(row, 0, rows, columns).x - baseCellSize.x * .5f - margin;
        public float GetRightBoundary(int row, int rows, int columns, float margin = 0f) => GetCellCenter(row, columns - 1, rows, columns).x + baseCellSize.x * .5f + margin;
    }
}