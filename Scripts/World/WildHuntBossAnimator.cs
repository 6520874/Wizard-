using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
    public enum WildHuntAnimation
    {
        Spawn,
        Idle,
        Run,
        Attack,
        Hurt,
        Death
    }

    [RequireComponent(typeof(SpriteRenderer))]
    // 中文说明：播放月夜骑士首领的待机、奔跑、攻击、受击和死亡动画。
    public class WildHuntBossAnimator : MonoBehaviour
    {
        [SerializeField] private string frameRoot = "Art/WildHuntBoss/Frames";
        [SerializeField] private float spawnFramesPerSecond = 10f;
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float attackFramesPerSecond = 12f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float deathFramesPerSecond = 9f;
        [SerializeField] private float pixelsPerUnit = 64f;

        private readonly Dictionary<WildHuntAnimation, Sprite[]> framesByAnimation = new Dictionary<WildHuntAnimation, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private WildHuntAnimation currentAnimation = WildHuntAnimation.Idle;
        private WildHuntAnimation loopAfterOneShot = WildHuntAnimation.Idle;
        private int frameIndex;
        private float frameTimer;
        private bool oneShotPlaying;
        private bool deathPlaying;

        public bool IsOneShotPlaying => oneShotPlaying;
        public bool IsDeathPlaying => deathPlaying;
        public WildHuntAnimation CurrentAnimation => currentAnimation;

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
            PlayLoop(WildHuntAnimation.Idle, true);
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
            if (oneShotPlaying || deathPlaying)
            {
                return;
            }

            PlayLoop(isMoving ? WildHuntAnimation.Run : WildHuntAnimation.Idle);
        }

        public void PlaySpawn()
        {
            PlayOneShot(WildHuntAnimation.Spawn, WildHuntAnimation.Idle);
        }

        public void PlayAttack()
        {
            if (deathPlaying)
            {
                return;
            }

            PlayOneShot(WildHuntAnimation.Attack, WildHuntAnimation.Idle);
        }

        public void PlayHurt()
        {
            if (deathPlaying)
            {
                return;
            }

            PlayOneShot(WildHuntAnimation.Hurt, WildHuntAnimation.Idle);
        }

        public void PlayDeath()
        {
            deathPlaying = true;
            oneShotPlaying = false;
            SetAnimation(WildHuntAnimation.Death);
        }

        private void PlayLoop(WildHuntAnimation animation, bool restart = false)
        {
            if (!restart && currentAnimation == animation)
            {
                return;
            }

            oneShotPlaying = false;
            SetAnimation(animation);
        }

        private void PlayOneShot(WildHuntAnimation animation, WildHuntAnimation afterAnimation)
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

        private void SetAnimation(WildHuntAnimation animation)
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
            if (deathPlaying && frameIndex >= frames.Length)
            {
                frameIndex = frames.Length - 1;
                spriteRenderer.sprite = frames[frameIndex];
                return;
            }

            if (oneShotPlaying && frameIndex >= frames.Length)
            {
                oneShotPlaying = false;
                PlayLoop(loopAfterOneShot, true);
                return;
            }

            frameIndex %= frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private Sprite[] GetFrames(WildHuntAnimation animation)
        {
            return framesByAnimation.TryGetValue(animation, out Sprite[] frames) ? frames : System.Array.Empty<Sprite>();
        }

        private float GetFramesPerSecond(WildHuntAnimation animation)
        {
            switch (animation)
            {
                case WildHuntAnimation.Spawn:
                    return spawnFramesPerSecond;
                case WildHuntAnimation.Run:
                    return runFramesPerSecond;
                case WildHuntAnimation.Attack:
                    return attackFramesPerSecond;
                case WildHuntAnimation.Hurt:
                    return hurtFramesPerSecond;
                case WildHuntAnimation.Death:
                    return deathFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void LoadFrames()
        {
            LoadFrames(WildHuntAnimation.Spawn);
            LoadFrames(WildHuntAnimation.Idle);
            LoadFrames(WildHuntAnimation.Run);
            LoadFrames(WildHuntAnimation.Attack);
            LoadFrames(WildHuntAnimation.Hurt);
            LoadFrames(WildHuntAnimation.Death);
            if (framesByAnimation[WildHuntAnimation.Death].Length == 0)
            {
                framesByAnimation[WildHuntAnimation.Death] = framesByAnimation[WildHuntAnimation.Hurt];
            }
        }

        private void LoadFrames(WildHuntAnimation animation)
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
