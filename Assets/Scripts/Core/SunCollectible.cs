using UnityEngine;

namespace PvZ2.Foundation
{
    public enum SunCollectibleState
    {
        Moving,
        Resting,
        Collected,
        Expired
    }

    public sealed class SunCollectible : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField, Min(1)] private int sunValue = 50;
        [SerializeField, Min(0.01f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float restLifetime = 8f;
        [SerializeField] private Vector3 destination;
        [SerializeField] private SunCollectibleState state = SunCollectibleState.Moving;

        private float restingElapsed;

        public ResourceManager ResourceManager => resourceManager;
        public int SunValue => sunValue;
        public Vector3 Destination => destination;
        public SunCollectibleState State => state;

        public void Configure(
            ResourceManager resources,
            int value,
            Vector3 start,
            Vector3 target,
            float speed = 3.5f,
            float lifetime = 8f)
        {
            resourceManager = resources;
            sunValue = Mathf.Max(1, value);
            moveSpeed = Mathf.Max(0.01f, speed);
            restLifetime = Mathf.Max(0.1f, lifetime);
            destination = target;
            restingElapsed = 0f;
            transform.position = start;
            state = Vector3.SqrMagnitude(start - target) <= 0.0001f
                ? SunCollectibleState.Resting
                : SunCollectibleState.Moving;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (state == SunCollectibleState.Moving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    moveSpeed * Mathf.Max(0f, deltaTime));

                if (Vector3.SqrMagnitude(transform.position - destination) <= 0.0001f)
                {
                    transform.position = destination;
                    state = SunCollectibleState.Resting;
                    restingElapsed = 0f;
                }

                return;
            }

            if (state != SunCollectibleState.Resting)
            {
                return;
            }

            restingElapsed += Mathf.Max(0f, deltaTime);
            if (restingElapsed >= restLifetime)
            {
                Expire();
            }
        }

        public bool Collect()
        {
            if ((state != SunCollectibleState.Moving && state != SunCollectibleState.Resting) ||
                resourceManager == null)
            {
                return false;
            }

            state = SunCollectibleState.Collected;
            resourceManager.AddSun(sunValue);
            DestroyAtRuntime();
            return true;
        }

        public bool Expire()
        {
            if (state != SunCollectibleState.Moving && state != SunCollectibleState.Resting)
            {
                return false;
            }

            state = SunCollectibleState.Expired;
            DestroyAtRuntime();
            return true;
        }

        private void DestroyAtRuntime()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }
}
