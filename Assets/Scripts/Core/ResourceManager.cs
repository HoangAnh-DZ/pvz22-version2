using System;
using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class ResourceManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingSun = 50;
        [SerializeField] private int currentSun;

        public int StartingSun => startingSun;
        public int CurrentSun => currentSun;
        public event Action<int> OnSunChanged;

        private void Awake()
        {
            ResetSun();
        }

        public void ConfigureStartingSun(int amount, bool resetImmediately = true)
        {
            startingSun = Mathf.Max(0, amount);
            if (resetImmediately)
            {
                ResetSun();
            }
        }

        public void AddSun(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int nextSun = amount > int.MaxValue - currentSun
                ? int.MaxValue
                : currentSun + amount;
            if (nextSun == currentSun)
            {
                return;
            }

            currentSun = nextSun;
            OnSunChanged?.Invoke(currentSun);
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && currentSun >= amount;
        }

        public bool SpendSun(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            currentSun -= amount;
            OnSunChanged?.Invoke(currentSun);
            return true;
        }

        public void ResetSun()
        {
            currentSun = startingSun;
            OnSunChanged?.Invoke(currentSun);
        }
    }
}
