using UnityEngine;

namespace PvZ2.Foundation
{
    [RequireComponent(typeof(PlantBase))]
    public sealed class SunflowerProducer : MonoBehaviour
    {
        [SerializeField] private SunCollectible sunPrefab;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private Transform sunParent;
        [SerializeField, Min(0.1f)] private float initialDelay = 5f;
        [SerializeField, Min(0.1f)] private float productionInterval = 9f;
        [SerializeField, Min(1)] private int sunValue = 50;
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float restLifetime = 9f;

        private PlantBase plant;
        private float timeUntilProduction;

        public int ProductionCount { get; private set; }
        public SunCollectible LastProduced { get; private set; }

        private void Awake()
        {
            plant = GetComponent<PlantBase>();
        }

        private void OnEnable()
        {
            timeUntilProduction = initialDelay;
            if (Application.isPlaying && resourceManager == null)
            {
                resourceManager = FindFirstObjectByType<ResourceManager>();
            }

            if (Application.isPlaying && sunParent == null)
            {
                GameObject container = GameObject.Find("Sun Collectibles");
                sunParent = container != null ? container.transform : null;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            SunCollectible prefab,
            ResourceManager resources,
            Transform parent,
            float delay = 5f,
            float interval = 9f)
        {
            sunPrefab = prefab;
            resourceManager = resources;
            sunParent = parent;
            initialDelay = Mathf.Max(0.1f, delay);
            productionInterval = Mathf.Max(0.1f, interval);
            timeUntilProduction = initialDelay;
            plant ??= GetComponent<PlantBase>();
        }

        public SunCollectible Tick(float deltaTime)
        {
            plant ??= GetComponent<PlantBase>();
            if (!isActiveAndEnabled || plant == null || plant.CurrentCell == null ||
                sunPrefab == null || resourceManager == null)
            {
                return null;
            }

            timeUntilProduction -= Mathf.Max(0f, deltaTime);
            if (timeUntilProduction > 0f)
            {
                return null;
            }

            timeUntilProduction += productionInterval;
            Vector3 start = transform.position + new Vector3(0f, 0.55f, 0f);
            Vector3 target = transform.position + new Vector3(0.5f, 0.08f, 0f);
            SunCollectible instance = Instantiate(sunPrefab, start, Quaternion.identity, sunParent);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            instance.Configure(resourceManager, sunValue, start, target, moveSpeed, restLifetime);
            LastProduced = instance;
            ProductionCount++;
            return instance;
        }
    }
}
