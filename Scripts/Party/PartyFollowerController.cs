using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    // 中文说明：地图上的单个队友跟随者，负责移动插值、左右朝向和基础序列帧。
    public class PartyFollowerController : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private float moveSpeed = 4.9f;
        [SerializeField] private float stopDistance = 0.05f;

        private SpriteRenderer spriteRenderer;
        private PartyMember member;
        private Sprite[] idleFrames = System.Array.Empty<Sprite>();
        private Sprite[] runFrames = System.Array.Empty<Sprite>();
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
            currentFrames = idleFrames.Length > 0 ? idleFrames : runFrames;
            frameIndex = 0;
            frameTimer = 0f;
            if (spriteRenderer != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[0];
            }
        }

        public void MoveToward(Vector2 targetPosition, float deltaTime)
        {
            Vector2 currentPosition = transform.position;
            Vector2 toTarget = targetPosition - currentPosition;
            bool moving = toTarget.magnitude > stopDistance;
            if (moving)
            {
                Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * deltaTime);
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
                if (Mathf.Abs(toTarget.x) > 0.025f && spriteRenderer != null)
                {
                    spriteRenderer.flipX = toTarget.x < 0f;
                }
            }

            UseFrames(moving && runFrames.Length > 0 ? runFrames : idleFrames);
        }

        public void SnapTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            UseFrames(idleFrames);
            UpdateDepthSorting();
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
