using UnityEngine;

namespace PvZ2.Foundation
{
    public enum PlantPresentationKind
    {
        Peashooter,
        Sunflower,
        WallNut
    }

    public sealed class PlantPresentationAnimator : MonoBehaviour
    {
        [SerializeField] private PlantPresentationKind kind;

        private PeashooterController shooter;
        private SunflowerProducer producer;
        private CombatHealth health;
        private SpriteRenderer[] renderers;
        private Vector3 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private int fireCount;
        private int productionCount;
        private float actionTime;
        private float damageTime;
        private float deathTime;
        private bool dying;

        public void Configure(PlantPresentationKind value)
        {
            kind = value;
        }

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            shooter = GetComponentInParent<PeashooterController>();
            producer = GetComponentInParent<SunflowerProducer>();
            health = GetComponentInParent<CombatHealth>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            fireCount = shooter != null ? shooter.FireCount : 0;
            productionCount = producer != null ? producer.ProductionCount : 0;
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDied += HandleDied;
            }
        }

        private void Update()
        {
            if (dying)
            {
                UpdateDeath();
                return;
            }

            if (shooter != null && shooter.FireCount != fireCount)
            {
                fireCount = shooter.FireCount;
                actionTime = .20f;
            }

            if (producer != null && producer.ProductionCount != productionCount)
            {
                productionCount = producer.ProductionCount;
                actionTime = .42f;
            }

            actionTime = Mathf.Max(0f, actionTime - Time.deltaTime);
            damageTime = Mathf.Max(0f, damageTime - Time.deltaTime);

            float phase = Time.time * (kind == PlantPresentationKind.WallNut ? 1.7f : 2.5f);
            float bob = Mathf.Sin(phase) * (kind == PlantPresentationKind.WallNut ? .012f : .025f);
            float sway = Mathf.Sin(phase * .73f) * (kind == PlantPresentationKind.WallNut ? .7f : 1.8f);
            float action01 = actionTime <= 0f ? 0f : Mathf.Sin(actionTime * Mathf.PI / .20f);
            if (kind == PlantPresentationKind.Sunflower && actionTime > 0f)
                action01 = Mathf.Sin(actionTime * Mathf.PI / .42f);
            float hurt01 = damageTime <= 0f ? 0f : Mathf.Sin(damageTime * Mathf.PI / .16f);

            transform.localPosition = basePosition + new Vector3(
                kind == PlantPresentationKind.Peashooter ? -action01 * .065f : 0f,
                bob + (kind == PlantPresentationKind.Sunflower ? action01 * .045f : 0f), 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, sway);
            float pulse = kind == PlantPresentationKind.Sunflower ? action01 * .10f : 0f;
            transform.localScale = Vector3.Scale(baseScale,
                new Vector3(1f + pulse + hurt01 * .08f, 1f + pulse - hurt01 * .10f, 1f));
        }

        private void HandleDamaged(CombatHealth source, int amount)
        {
            damageTime = .16f;
        }

        private void HandleDied(CombatHealth source)
        {
            if (dying) return;
            dying = true;
            transform.SetParent(null, true);
            basePosition = transform.position;
            baseScale = transform.localScale;
            baseRotation = transform.rotation;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void UpdateDeath()
        {
            deathTime += Time.deltaTime;
            float t = Mathf.Clamp01(deathTime / .48f);
            transform.position = basePosition + new Vector3(.12f * t, -.22f * t, 0f);
            transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, -72f * t);
            transform.localScale = baseScale * (1f - .12f * t);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
            }

            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (health == null) return;
            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDied;
        }
    }
}
