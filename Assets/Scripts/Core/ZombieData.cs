using UnityEngine;

namespace PvZ2.Foundation
{
    [CreateAssetMenu(fileName = "ZombieData", menuName = "PvZ2 Foundation/Zombie Data")]
    public sealed class ZombieData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ZombieController prefab;
        public Sprite icon;
        [Min(1)] public int maxHealth = 100;
        [Min(0f)] public float movementSpeed = 0.45f;
        [Min(0f)] public float attackRange = 0.72f;
        [Min(0)] public int attackDamage = 20;
        [Min(0.01f)] public float attackInterval = 1f;

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            movementSpeed = Mathf.Max(0f, movementSpeed);
            attackRange = Mathf.Max(0f, attackRange);
            attackDamage = Mathf.Max(0, attackDamage);
            attackInterval = Mathf.Max(0.01f, attackInterval);
        }
    }
}