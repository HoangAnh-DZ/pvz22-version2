using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class SkySunSpawner : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private SunCollectible sunPrefab;
        [SerializeField] private Transform sunParent;
        [SerializeField, Min(0.1f)] private float spawnInterval = 8f;
        [SerializeField, Min(0.1f)] private float startHeight = 2.6f;
        [SerializeField, Min(1)] private int sunValue = 50;
        [SerializeField, Min(0.01f)] private float moveSpeed = 2.6f;
        [SerializeField, Min(0.1f)] private float restLifetime = 8f;

        private float timeUntilSpawn;

        public int SpawnCount { get; private set; }
        public SunCollectible LastSpawned { get; private set; }

        private void OnEnable()
        {
            timeUntilSpawn = spawnInterval;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            GridManager grid,
            ResourceManager resources,
            SunCollectible prefab,
            Transform parent,
            float interval = 8f,
            float height = 2.6f)
        {
            gridManager = grid;
            resourceManager = resources;
            sunPrefab = prefab;
            sunParent = parent;
            spawnInterval = Mathf.Max(0.1f, interval);
            startHeight = Mathf.Max(0.1f, height);
            timeUntilSpawn = spawnInterval;
        }

        public SunCollectible Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || gridManager == null || resourceManager == null || sunPrefab == null)
            {
                return null;
            }

            timeUntilSpawn -= Mathf.Max(0f, deltaTime);
            if (timeUntilSpawn > 0f)
            {
                return null;
            }

            timeUntilSpawn += spawnInterval;
            int row = Random.Range(0, gridManager.Rows);
            int column = Random.Range(0, gridManager.Columns);
            return SpawnSunAt(row, column, startHeight);
        }

        public Vector3 GetDestination(int row, int column)
        {
            return gridManager != null
                ? gridManager.GetCellCenter(row, column)
                : Vector3.positiveInfinity;
        }

        public SunCollectible SpawnSunAt(int row, int column, float height)
        {
            if (gridManager == null || resourceManager == null || sunPrefab == null)
            {
                return null;
            }

            GridCell cell = gridManager.GetCell(row, column);
            if (cell == null)
            {
                return null;
            }

            Vector3 target = cell.WorldPosition;
            Vector3 start = target + Vector3.up * Mathf.Max(0.1f, height);
            SunCollectible instance = Instantiate(sunPrefab, start, Quaternion.identity, sunParent);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            instance.Configure(resourceManager, sunValue, start, target, moveSpeed, restLifetime);
            LastSpawned = instance;
            SpawnCount++;
            return instance;
        }
    }
}
