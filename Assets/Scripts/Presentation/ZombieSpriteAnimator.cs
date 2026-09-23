using UnityEngine;

namespace PvZ2.Foundation
{
    [DisallowMultipleComponent]
    public sealed class ZombieSpriteAnimator : MonoBehaviour
    {
        [SerializeField] ZombieController owner;
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite[] idleFrames;
        [SerializeField] Sprite[] walkFrames;
        [SerializeField] Sprite[] attackFrames;
        [SerializeField] Sprite[] deathFrames;
        [SerializeField, Min(1f)] float framesPerSecond = 10f;

        ZombieController.ZombieState lastState;
        float stateTime;

        public void Configure(
            ZombieController controller,
            SpriteRenderer spriteRenderer,
            Sprite[] idle,
            Sprite[] walk,
            Sprite[] attack,
            Sprite[] death,
            float fps)
        {
            owner = controller;
            target = spriteRenderer;
            idleFrames = idle;
            walkFrames = walk;
            attackFrames = attack;
            deathFrames = death;
            framesPerSecond = Mathf.Max(1f, fps);
            lastState = owner != null ? owner.State : ZombieController.ZombieState.Moving;
            stateTime = 0f;
            ApplyFrame();
        }

        void Awake()
        {
            if (owner == null) owner = GetComponentInParent<ZombieController>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (target == null) return;
            ZombieController.ZombieState state = owner != null
                ? owner.State
                : ZombieController.ZombieState.Moving;
            if (state != lastState)
            {
                lastState = state;
                stateTime = 0f;
            }
            else
            {
                stateTime += Time.deltaTime;
            }
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (target == null) return;
            Sprite[] frames = FramesFor(lastState);
            if (frames == null || frames.Length == 0) return;
            int frame = Mathf.FloorToInt(stateTime * framesPerSecond) % frames.Length;
            target.sprite = frames[frame];
        }

        Sprite[] FramesFor(ZombieController.ZombieState state)
        {
            if (state == ZombieController.ZombieState.Attacking)
                return attackFrames != null && attackFrames.Length > 0 ? attackFrames : walkFrames;
            if (state == ZombieController.ZombieState.Dead)
                return deathFrames != null && deathFrames.Length > 0 ? deathFrames : idleFrames;
            return walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;
        }
    }
}