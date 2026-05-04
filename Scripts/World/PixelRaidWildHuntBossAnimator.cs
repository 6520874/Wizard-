using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PixelRaid
{
    public enum PixelRaidWildHuntBossAnimation
    {
        Spawn,
        Idle,
        Run,
        Attack,
        Hurt
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelRaidWildHuntBossAnimator : MonoBehaviour
    {
        [SerializeField] private string frameRoot = "Art/WildHuntBoss/Frames";
        [SerializeField] private float spawnFramesPerSecond = 10f;
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float attackFramesPerSecond = 12f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float pixelsPerUnit = 64f;

        private readonly Dictionary<PixelRaidWildHuntBossAnimation, Sprite[]> framesByAnimation = new Dictionary<PixelRaidWildHuntBossAnimation, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private PixelRaidWildHuntBossAnimation currentAnimation = PixelRaidWildHuntBossAnimation.Idle;
        private PixelRaidWildHuntBossAnimation loopAfterOneShot = PixelRaidWildHuntBossAnimation.Idle;
        private int frameIndex;
        private float frameTimer;
        private bool oneShotPlaying;

        public bool IsOneShotPlaying => oneShotPlaying;
        public PixelRaidWildHuntBossAnimation CurrentAnimation => currentAnimation;

        public float NormalizedFrame
        {
            get
            {
                Sprite[] frames = GetFrames(currentAnimation);
                return frames.Length == 0 ? 0f : (frameIndex + 1f) / frames.Length;
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadFrames();
            PlayLoop(PixelRaidWildHuntBossAnimation.Idle, true);
        }

        private void Update()
        {
            Sprite[] frames = GetFrames(currentAnimation);
            if (frames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / GetFramesPerSecond(currentAnimation);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                AdvanceFrame(frames);
            }
        }

        public void PlayLocomotion(bool isMoving)
        {
            if (oneShotPlaying)
            {
                return;
            }

            PlayLoop(isMoving ? PixelRaidWildHuntBossAnimation.Run : PixelRaidWildHuntBossAnimation.Idle);
        }

        public void PlaySpawn()
        {
            PlayOneShot(PixelRaidWildHuntBossAnimation.Spawn, PixelRaidWildHuntBossAnimation.Idle);
        }

        public void PlayAttack()
        {
            PlayOneShot(PixelRaidWildHuntBossAnimation.Attack, PixelRaidWildHuntBossAnimation.Idle);
        }

        public void PlayHurt()
        {
            PlayOneShot(PixelRaidWildHuntBossAnimation.Hurt, PixelRaidWildHuntBossAnimation.Idle);
        }

        private void PlayLoop(PixelRaidWildHuntBossAnimation animation, bool restart = false)
        {
            if (!restart && currentAnimation == animation)
            {
                return;
            }

            oneShotPlaying = false;
            SetAnimation(animation);
        }

        private void PlayOneShot(PixelRaidWildHuntBossAnimation animation, PixelRaidWildHuntBossAnimation afterAnimation)
        {
            if (GetFrames(animation).Length == 0)
            {
                PlayLoop(afterAnimation, true);
                return;
            }

            oneShotPlaying = true;
            loopAfterOneShot = afterAnimation;
            SetAnimation(animation);
        }

        private void SetAnimation(PixelRaidWildHuntBossAnimation animation)
        {
            currentAnimation = animation;
            frameIndex = 0;
            frameTimer = 0f;
            Sprite[] frames = GetFrames(currentAnimation);
            if (frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void AdvanceFrame(Sprite[] frames)
        {
            frameIndex++;
            if (oneShotPlaying && frameIndex >= frames.Length)
            {
                oneShotPlaying = false;
                PlayLoop(loopAfterOneShot, true);
                return;
            }

            frameIndex %= frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private Sprite[] GetFrames(PixelRaidWildHuntBossAnimation animation)
        {
            return framesByAnimation.TryGetValue(animation, out Sprite[] frames) ? frames : System.Array.Empty<Sprite>();
        }

        private float GetFramesPerSecond(PixelRaidWildHuntBossAnimation animation)
        {
            switch (animation)
            {
                case PixelRaidWildHuntBossAnimation.Spawn:
                    return spawnFramesPerSecond;
                case PixelRaidWildHuntBossAnimation.Run:
                    return runFramesPerSecond;
                case PixelRaidWildHuntBossAnimation.Attack:
                    return attackFramesPerSecond;
                case PixelRaidWildHuntBossAnimation.Hurt:
                    return hurtFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void LoadFrames()
        {
            LoadFrames(PixelRaidWildHuntBossAnimation.Spawn);
            LoadFrames(PixelRaidWildHuntBossAnimation.Idle);
            LoadFrames(PixelRaidWildHuntBossAnimation.Run);
            LoadFrames(PixelRaidWildHuntBossAnimation.Attack);
            LoadFrames(PixelRaidWildHuntBossAnimation.Hurt);
        }

        private void LoadFrames(PixelRaidWildHuntBossAnimation animation)
        {
            string folderPath = Path.Combine(Application.dataPath, frameRoot, animation.ToString());
            if (!Directory.Exists(folderPath))
            {
                framesByAnimation[animation] = System.Array.Empty<Sprite>();
                return;
            }

            List<Sprite> sprites = new List<Sprite>();
            foreach (string filePath in Directory.GetFiles(folderPath, "*.png").OrderBy(path => path))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    continue;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f),
                    pixelsPerUnit);
                sprite.name = Path.GetFileNameWithoutExtension(filePath);
                sprites.Add(sprite);
            }

            framesByAnimation[animation] = sprites.ToArray();
        }
    }
}
