using UnityEngine;

namespace PvZ2.Foundation
{
    [CreateAssetMenu(fileName = "PlantData", menuName = "PvZ2 Foundation/Plant Data")]
    public sealed class PlantData : ScriptableObject
    {
        public string id;
        public string displayName;
        public PlantBase prefab;
        public Sprite icon;
        [Min(0)] public int sunCost = 50;
        [Min(0f)] public float cooldown = 5f;
        [Min(1)] public int maxHealth = 100;
        [Min(0)] public int attackDamage;
        [Min(0f)] public float attackInterval = 1f;

        private void OnValidate()
        {
            sunCost = Mathf.Max(0, sunCost);
            cooldown = Mathf.Max(0f, cooldown);
            maxHealth = Mathf.Max(1, maxHealth);
            attackDamage = Mathf.Max(0, attackDamage);
            attackInterval = Mathf.Max(0f, attackInterval);
        }
    }
}
