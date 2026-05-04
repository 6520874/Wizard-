using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PixelRaid
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelRaidGeraltAnimator : MonoBehaviour
    {
        [SerializeField] private string frameRoot = "Art/Geralt/Frames";
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float slashFramesPerSecond = 14f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float pixelsPerUnit = 96f;

        private readonly Dictionary<PixelRaidGeraltAnimation, Sprite[]> framesByAnimation = new Dictionary<PixelRaidGeraltAnimation, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private PixelRaidGeraltAnimation currentAnimation = PixelRaidGeraltAnimation.Idle;
        private int frameIndex;
        private float frameTimer;

        public bool IsSlashPlaying { get; private set; }
        public bool IsHurtPlaying { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadFrames();
            Play(PixelRaidGeraltAnimation.Idle, true);
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
            if (IsSlashPlaying || IsHurtPlaying)
            {
                return;
            }

            Play(isMoving ? PixelRaidGeraltAnimation.Run : PixelRaidGeraltAnimation.Idle);
        }

        public void PlaySlash()
        {
            if (IsHurtPlaying)
            {
                return;
            }

            IsSlashPlaying = true;
            Play(PixelRaidGeraltAnimation.Slash, true);
        }

        public void PlayHurt()
        {
            IsSlashPlaying = false;
            IsHurtPlaying = true;
            Play(PixelRaidGeraltAnimation.Hurt, true);
        }

        public void ForceIdle()
        {
            IsSlashPlaying = false;
            IsHurtPlaying = false;
            Play(PixelRaidGeraltAnimation.Idle, true);
        }

        private void Play(PixelRaidGeraltAnimation animation, bool restart = false)
        {
            if (!restart && currentAnimation == animation)
            {
                return;
            }

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
            bool isOneShot = currentAnimation == PixelRaidGeraltAnimation.Slash || currentAnimation == PixelRaidGeraltAnimation.Hurt;
            if (isOneShot && frameIndex >= frames.Length)
            {
                IsSlashPlaying = false;
                IsHurtPlaying = false;
                Play(PixelRaidGeraltAnimation.Idle, true);
                return;
            }

            frameIndex %= frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private Sprite[] GetFrames(PixelRaidGeraltAnimation animation)
        {
            return framesByAnimation.TryGetValue(animation, out Sprite[] frames) ? frames : System.Array.Empty<Sprite>();
        }

        private float GetFramesPerSecond(PixelRaidGeraltAnimation animation)
        {
            switch (animation)
            {
                case PixelRaidGeraltAnimation.Run:
                    return runFramesPerSecond;
                case PixelRaidGeraltAnimation.Slash:
                    return slashFramesPerSecond;
                case PixelRaidGeraltAnimation.Hurt:
                    return hurtFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void LoadFrames()
        {
            LoadFrames(PixelRaidGeraltAnimation.Idle);
            LoadFrames(PixelRaidGeraltAnimation.Run);
            LoadFrames(PixelRaidGeraltAnimation.Slash);
            LoadFrames(PixelRaidGeraltAnimation.Hurt);
        }

        private void LoadFrames(PixelRaidGeraltAnimation animation)
        {
            string folderPath = Path.Combine(Application.dataPath, frameRoot, animation.ToString());
            if (!Directory.Exists(folderPath))
            {
                framesByAnimation[animation] = LoadFallbackFrames(animation);
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

            framesByAnimation[animation] = sprites.Count > 0 ? sprites.ToArray() : LoadFallbackFrames(animation);
        }

        private Sprite[] LoadFallbackFrames(PixelRaidGeraltAnimation animation)
        {
            int frameCount = PixelRaidSpriteLibrary.GetGeraltFrameCount(animation);
            Sprite[] sprites = new Sprite[frameCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = PixelRaidSpriteLibrary.GetGeraltFrame(animation, i);
            }

            return sprites;
        }
    }
}
