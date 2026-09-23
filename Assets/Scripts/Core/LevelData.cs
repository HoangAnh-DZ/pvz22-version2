using System;
using System.Collections.Generic;
using UnityEngine;
namespace PvZ2.Foundation
{
    public enum LaneMode { RandomValidLane, FixedLane }
    public enum GameState { Preparing, Playing, WaveTransition, Victory, Defeat }

    [Serializable]
    public sealed class ZombieSpawnEntry
    {
        public ZombieData zombie;
        [Min(1)] public int amount = 1;
        [Min(0f)] public float startDelay;
        [Min(.01f)] public float spawnInterval = 1f;
        public LaneMode laneMode = LaneMode.RandomValidLane;
        [Min(0)] public int fixedLane;
    }

    [Serializable]
    public sealed class WaveData
    {
        public string displayName = "Wave";
        public List<ZombieSpawnEntry> spawns = new();
    }

    [CreateAssetMenu(menuName = "PvZ2/Level Data", fileName = "Level")]
    public sealed class LevelData : ScriptableObject
    {
        public string id = "level-01";
        public string displayName = "Front Yard";
        [Min(0f)] public float preparationDelay = 2f;
        [Min(0f)] public float waveTransitionDelay = 2f;
        public List<WaveData> waves = new();
    }
}