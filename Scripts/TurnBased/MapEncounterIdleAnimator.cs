using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class MapEncounterIdleAnimator : MonoBehaviour
    {
        [SerializeField] private TurnBasedEnemyVisualKind visualKind = TurnBasedEnemyVisualKind.CorruptedWolf;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private float maxStageY = 5f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] idleFrames;
        private int frameIndex;
        private float frameTimer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            UpdateDepthSorting();
            if (idleFrames == null || idleFrames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % idleFrames.Length;
                spriteRenderer.sprite = idleFrames[frameIndex];
            }
        }

        public void Configure(TurnBasedEnemyVisualKind kind)
        {
            visualKind = kind;
            TurnBasedEnemyState preview = new TurnBasedEnemyState();
            TurnBasedEnemyAnimationLibrary.FillAnimations(preview, visualKind);
            idleFrames = preview.IdleFrames;
            frameIndex = 0;
            frameTimer = 0f;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (idleFrames != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private void UpdateDepthSorting()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Mathf.RoundToInt((maxStageY - transform.position.y) * 100f) + 17;
            }
        }
    }
}
