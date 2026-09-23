using System.Collections.Generic;
using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class LaneCombatRegistry : MonoBehaviour
    {
        [SerializeField, Min(1)] private int laneCount = 5;
        private List<PlantBase>[] plantsByLane;
        private List<ZombieController>[] zombiesByLane;

        public static LaneCombatRegistry Active { get; private set; }
        public int LaneCount => laneCount;

        private void Awake()
        {
            EnsureStorage();
            Active = this;
        }

        private void OnEnable()
        {
            EnsureStorage();
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        public void Configure(int lanes)
        {
            laneCount = Mathf.Max(1, lanes);
            plantsByLane = null;
            zombiesByLane = null;
            EnsureStorage();
            Active = this;
        }

        public bool RegisterPlant(PlantBase plant)
        {
            EnsureStorage();
            if (plant == null || plant.CurrentCell == null || !IsValidLane(plant.Lane)) return false;
            List<PlantBase> plants = plantsByLane[plant.Lane];
            if (!plants.Contains(plant)) plants.Add(plant);
            return true;
        }

        public void UnregisterPlant(PlantBase plant)
        {
            if (plant == null || plantsByLane == null) return;
            for (int i = 0; i < plantsByLane.Length; i++) plantsByLane[i].Remove(plant);
        }

        public bool RegisterZombie(ZombieController zombie)
        {
            EnsureStorage();
            if (zombie == null || !IsValidLane(zombie.Lane)) return false;
            List<ZombieController> zombies = zombiesByLane[zombie.Lane];
            if (!zombies.Contains(zombie)) zombies.Add(zombie);
            return true;
        }

        public void UnregisterZombie(ZombieController zombie)
        {
            if (zombie == null || zombiesByLane == null) return;
            for (int i = 0; i < zombiesByLane.Length; i++) zombiesByLane[i].Remove(zombie);
        }

        public PlantBase GetNearestPlantAhead(int lane, float zombieX)
        {
            if (!TryGetPlants(lane, out List<PlantBase> plants)) return null;
            PlantBase nearest = null;
            float nearestX = float.NegativeInfinity;
            for (int i = plants.Count - 1; i >= 0; i--)
            {
                PlantBase plant = plants[i];
                if (!IsValidPlant(plant))
                {
                    plants.RemoveAt(i);
                    continue;
                }

                float x = plant.transform.position.x;
                if (x <= zombieX && x > nearestX)
                {
                    nearest = plant;
                    nearestX = x;
                }
            }

            return nearest;
        }

        public ZombieController GetNearestZombieAhead(int lane, float plantX)
        {
            if (!TryGetZombies(lane, out List<ZombieController> zombies)) return null;
            ZombieController nearest = null;
            float nearestX = float.PositiveInfinity;
            for (int i = zombies.Count - 1; i >= 0; i--)
            {
                ZombieController zombie = zombies[i];
                if (!IsValidZombie(zombie))
                {
                    zombies.RemoveAt(i);
                    continue;
                }

                float x = zombie.transform.position.x;
                if (x >= plantX && x < nearestX)
                {
                    nearest = zombie;
                    nearestX = x;
                }
            }

            return nearest;
        }

        public ZombieController GetFirstZombieInSegment(int lane, float fromX, float toX)
        {
            if (!TryGetZombies(lane, out List<ZombieController> zombies)) return null;
            float min = Mathf.Min(fromX, toX);
            float max = Mathf.Max(fromX, toX);
            ZombieController first = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = zombies.Count - 1; i >= 0; i--)
            {
                ZombieController zombie = zombies[i];
                if (!IsValidZombie(zombie))
                {
                    zombies.RemoveAt(i);
                    continue;
                }

                float x = zombie.transform.position.x;
                if (x < min || x > max) continue;
                float distance = Mathf.Abs(x - fromX);
                if (distance < bestDistance)
                {
                    first = zombie;
                    bestDistance = distance;
                }
            }

            return first;
        }

        public int GetPlantCount(int lane)
        {
            return TryGetPlants(lane, out List<PlantBase> plants) ? plants.Count : 0;
        }

        public int GetZombieCount(int lane)
        {
            return TryGetZombies(lane, out List<ZombieController> zombies) ? zombies.Count : 0;
        }

        private bool TryGetPlants(int lane, out List<PlantBase> plants)
        {
            EnsureStorage();
            plants = IsValidLane(lane) ? plantsByLane[lane] : null;
            return plants != null;
        }

        private bool TryGetZombies(int lane, out List<ZombieController> zombies)
        {
            EnsureStorage();
            zombies = IsValidLane(lane) ? zombiesByLane[lane] : null;
            return zombies != null;
        }

        private bool IsValidLane(int lane)
        {
            return lane >= 0 && lane < laneCount;
        }

        private void EnsureStorage()
        {
            if (plantsByLane != null && plantsByLane.Length == laneCount) return;
            plantsByLane = new List<PlantBase>[laneCount];
            zombiesByLane = new List<ZombieController>[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                plantsByLane[i] = new List<PlantBase>();
                zombiesByLane[i] = new List<ZombieController>();
            }
        }

        private static bool IsValidPlant(PlantBase plant)
        {
            return plant != null && plant.CurrentCell != null && plant.Health != null && !plant.Health.IsDead;
        }

        private static bool IsValidZombie(ZombieController zombie)
        {
            return zombie != null && zombie.Health != null && !zombie.Health.IsDead && !zombie.ReachedHome;
        }
    }
}