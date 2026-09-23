using UnityEngine;

namespace PvZ2.Foundation
{
    [RequireComponent(typeof(CombatHealth))]
    
public sealed class PlantBase : MonoBehaviour
    {
        [SerializeField] private PlantData data;
        [SerializeField] private GridCell currentCell;

        private CombatHealth combatHealth;
        private LaneCombatRegistry registry;
        private bool deathResolved;

        public PlantData Data => data;
        public GridCell CurrentCell => currentCell;
        public int Lane => currentCell != null ? currentCell.Row : -1;
        public CombatHealth Health => combatHealth != null ? combatHealth : combatHealth = GetComponent<CombatHealth>();
        public LaneCombatRegistry Registry => registry;


public bool Initialize(PlantData plantData, GridCell cell, LaneCombatRegistry combatRegistry = null)
        {
            if (plantData == null || cell == null || currentCell != null)
            {
                return false;
            }

            if (!cell.TryOccupy(this))
            {
                return false;
            }

            data = plantData;
            currentCell = cell;
            transform.position = cell.WorldPosition;
            registry = combatRegistry != null ? combatRegistry : LaneCombatRegistry.Active;
            deathResolved = false;
            Health.Configure(data.maxHealth);
            Health.OnDied -= HandleDied;
            Health.OnDied += HandleDied;
            
            GetComponent<LaneDepthVisual>()?.ApplyLane(cell.Row, registry != null ? registry.LaneCount : 5);
registry?.RegisterPlant(this);
            return true;
        }

public void ReleaseFromCell()
        {
            if (currentCell == null)
            {
                return;
            }

            registry?.UnregisterPlant(this);
            GridCell occupiedCell = currentCell;
            currentCell = null;
            occupiedCell.Release(this);
        }

public void Die()
        {
            if (!Health.IsDead)
            {
                Health.TakeDamage(Health.CurrentHealth);
            }
            else
            {
                HandleDied(Health);
            }
        }

private void HandleDied(CombatHealth source)
        {
            if (deathResolved)
            {
                return;
            }

            deathResolved = true;
            ReleaseFromCell();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }


private void OnDestroy()
        {
            if (combatHealth != null)
            {
                combatHealth.OnDied -= HandleDied;
            }

            ReleaseFromCell();
        }
    

private void Awake()
        {
            combatHealth = GetComponent<CombatHealth>();
        }
}
}
