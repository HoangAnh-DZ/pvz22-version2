using UnityEngine;

namespace PvZ2.Foundation
{
    public sealed class PeaProjectile : MonoBehaviour
    {
        [SerializeField] private int lane = -1;
        [SerializeField, Min(0)] private int damage;
        [SerializeField, Min(0.01f)] private float speed = 5f;
        [SerializeField] private float rightBoundaryX = 6.5f;

        private LaneCombatRegistry registry;

        public int Lane => lane;
        public int Damage => damage;
        public float Speed => speed;
        public Vector3 PreviousPosition { get; private set; }
        public bool Resolved { get; private set; }
        public bool Expired { get; private set; }
        public ZombieController HitZombie { get; private set; }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Initialize(int projectileLane, int projectileDamage, float projectileSpeed,
            LaneCombatRegistry combatRegistry, float boundaryX = 6.5f)
        {
            lane = projectileLane;
            damage = Mathf.Max(0, projectileDamage);
            speed = Mathf.Max(0.01f, projectileSpeed);
            registry = combatRegistry;
            rightBoundaryX = boundaryX;
            PreviousPosition = transform.position;
            Resolved = false;
            Expired = false;
            HitZombie = null;
        }

        public ZombieController Tick(float deltaTime)
        {
            if (Resolved || registry == null) return null;

            PreviousPosition = transform.position;
            float nextX = PreviousPosition.x + speed * Mathf.Max(0f, deltaTime);
            ZombieController hit = registry.GetFirstZombieInSegment(lane, PreviousPosition.x, nextX);
            if (hit != null)
            {
                transform.position = new Vector3(hit.transform.position.x, transform.position.y, transform.position.z);
                Resolved = true;
                HitZombie = hit;
                hit.Health.TakeDamage(damage);
                Cleanup();
                return hit;
            }

            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            if (nextX > rightBoundaryX)
            {
                Resolved = true;
                Expired = true;
                Cleanup();
            }

            return null;
        }

        private void Cleanup()
        {
            if (Application.isPlaying) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}