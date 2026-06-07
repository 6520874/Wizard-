using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    // 中文说明：地图上的单个队友跟随者，负责移动插值、方向朝向和基础序列帧。
    public class PartyFollowerController : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private float moveSpeed = 4.9f;
        [SerializeField] private float stopDistance = 0.05f;

        private SpriteRenderer spriteRenderer;
        private PartyMember member;
        private Sprite[] idleFrames = System.Array.Empty<Sprite>();
        private Sprite[] runFrames = System.Array.Empty<Sprite>();
        private Sprite[] upFrames = System.Array.Empty<Sprite>();
        private Sprite[] downFrames = System.Array.Empty<Sprite>();
        private Sprite[] currentFrames = System.Array.Empty<Sprite>();
        private int frameIndex;
        private float frameTimer;
        private float maxStageY = 5f;

        public PartyMember Member => member;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
        }

        private void Update()
        {
            AdvanceAnimation();
            UpdateDepthSorting();
        }

        public void Configure(PartyMember partyMember, float visualScale, float stageMaxY)
        {
            member = partyMember;
            maxStageY = stageMaxY;
            transform.localScale = Vector3.one * Mathf.Clamp(visualScale, 0.18f, 1.2f);
            idleFrames = PartyAnimationLibrary.GetFrames(member, PartyAnimationKind.Idle);
            runFrames = PartyAnimationLibrary.GetFrames(member, PartyAnimationKind.Run);
            upFrames = PartyAnimationLibrary.GetFrames(member, PartyAnimationKind.Up);
            downFrames = PartyAnimationLibrary.GetFrames(member, PartyAnimationKind.DownWalk);
            frameIndex = 0;
            frameTimer = 0f;
            ResetToIdlePose();
        }

        public void MoveToward(Vector2 targetPosition, float deltaTime)
        {
            Vector2 currentPosition = transform.position;
            Vector2 toTarget = targetPosition - currentPosition;
            bool moving = toTarget.magnitude > stopDistance;
            if (moving)
            {
                Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * deltaTime);
                Vector2 actualMovement = nextPosition - currentPosition;
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
                Vector2 animationDirection = actualMovement.sqrMagnitude > 0.000001f ? actualMovement : toTarget;
                PartyAnimationKind moveAnimation = GetMovementAnimation(animationDirection);
                if (moveAnimation == PartyAnimationKind.Run && Mathf.Abs(animationDirection.x) > 0.001f && spriteRenderer != null)
                {
                    spriteRenderer.flipX = animationDirection.x < 0f;
                }
                else if (moveAnimation != PartyAnimationKind.Run && spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }

                UseFrames(GetMovementFrames(moveAnimation));
            }
            else
            {
                ResetToIdlePose();
            }
        }

        public void SnapTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            ResetToIdlePose();
            UpdateDepthSorting();
        }

        private PartyAnimationKind GetMovementAnimation(Vector2 movement)
        {
            if (Mathf.Abs(movement.y) > Mathf.Abs(movement.x) * 1.15f)
            {
                return movement.y > 0f ? PartyAnimationKind.Up : PartyAnimationKind.DownWalk;
            }

            return PartyAnimationKind.Run;
        }

        private Sprite[] GetMovementFrames(PartyAnimationKind animation)
        {
            switch (animation)
            {
                case PartyAnimationKind.Idle:
                    return idleFrames;
                case PartyAnimationKind.Up:
                    return upFrames.Length > 0 ? upFrames : GetBestRunFallback();
                case PartyAnimationKind.DownWalk:
                    return downFrames.Length > 0 ? downFrames : GetBestRunFallback();
                default:
                    return GetBestRunFallback();
            }
        }

        private void ResetToIdlePose()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }

            if (idleFrames.Length > 0)
            {
                currentFrames = System.Array.Empty<Sprite>();
                frameIndex = 0;
                frameTimer = 0f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = idleFrames[0];
                }
                return;
            }

            Sprite[] fallbackFrames = GetBestRunFallback();
            currentFrames = System.Array.Empty<Sprite>();
            frameIndex = 0;
            frameTimer = 0f;
            if (spriteRenderer != null && fallbackFrames.Length > 0)
            {
                spriteRenderer.sprite = fallbackFrames[0];
            }
        }

        private Sprite[] GetBestRunFallback()
        {
            if (runFrames.Length > 0)
            {
                return runFrames;
            }

            return idleFrames;
        }

        private void UseFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0 || currentFrames == frames)
            {
                return;
            }

            currentFrames = frames;
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = currentFrames[0];
        }

        private void AdvanceAnimation()
        {
            if (currentFrames == null || currentFrames.Length <= 1 || spriteRenderer == null)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % currentFrames.Length;
                spriteRenderer.sprite = currentFrames[frameIndex];
            }
        }

        private void UpdateDepthSorting()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Mathf.RoundToInt((maxStageY - transform.position.y) * 100f) + 16;
            }
        }
    }
}
