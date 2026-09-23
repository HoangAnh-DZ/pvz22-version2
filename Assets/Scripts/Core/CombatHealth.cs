using System;
using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class CombatHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 1;
        [SerializeField] private int currentHealth = 1;
        [SerializeField] private bool isDead;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<CombatHealth, int> OnDamaged;
        public event Action<CombatHealth> OnDied;

        public void Configure(int value)
        {
            maxHealth = Mathf.Max(1, value);
            currentHealth = maxHealth;
            isDead = false;
        }

        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || isDead)
            {
                return false;
            }

            int applied = Mathf.Min(amount, currentHealth);
            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnDamaged?.Invoke(this, applied);
            if (currentHealth == 0)
            {
                isDead = true;
                OnDied?.Invoke(this);
            }

            return true;
        }
    }
}