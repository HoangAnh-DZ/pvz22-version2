using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class BasicZombieSpawner : MonoBehaviour
    {
        [SerializeField] private LaneCombatRegistry registry;
        [SerializeField] private ZombieData normalZombie;
        [SerializeField] private ZombieData coneheadZombie;
        [SerializeField] private GridManager grid;
        [SerializeField] private Transform zombieParent;
        [SerializeField] private float spawnX = 6.15f;
        [SerializeField] private float homeBoundaryX = -6.5f;

        
        public ZombieData NormalZombie => normalZombie;
        public ZombieData ConeheadZombie => coneheadZombie;
public ZombieController LastSpawned { get; private set; }
        public int SpawnCount { get; private set; }

public void Configure(LaneCombatRegistry combatRegistry, GridManager gridManager,
            Transform parent = null, float rightX = 6.15f, float leftBoundaryX = -6.5f,
            ZombieData normal = null, ZombieData conehead = null)
        {
            registry = combatRegistry;
            grid = gridManager;
            zombieParent = parent;
            spawnX = rightX;
            homeBoundaryX = leftBoundaryX;
            normalZombie = normal;
            coneheadZombie = conehead;
        }

        public ZombieController SpawnZombie(ZombieData data, int lane)
        {
            if (data == null || data.prefab == null || registry == null || grid == null ||
                lane < 0 || lane >= grid.Rows)
            {
                return null;
            }

            float laneSpawnX = grid.Projection != null ? grid.GetLaneRightBoundary(lane, 0.65f) : spawnX;
            float laneHomeBoundary = homeBoundaryX;
            Vector3 position = new(laneSpawnX, grid.GetCellCenter(lane, 0).y, 0f);
            ZombieController zombie = Instantiate(data.prefab, position, Quaternion.identity, zombieParent);
            if (!zombie.gameObject.activeSelf) zombie.gameObject.SetActive(true);
            if (!zombie.Initialize(data, lane, registry, laneHomeBoundary))
            {
                if (Application.isPlaying) Destroy(zombie.gameObject);
                else DestroyImmediate(zombie.gameObject);
                return null;
            }

            LastSpawned = zombie;
            SpawnCount++;
            return zombie;
        }
    

public ZombieController SpawnConehead(int lane)
        {
            return SpawnZombie(coneheadZombie, lane);
        }


public ZombieController SpawnNormal(int lane)
        {
            return SpawnZombie(normalZombie, lane);
        }
}
}