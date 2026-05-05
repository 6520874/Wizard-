using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GeraltAnimator : MonoBehaviour
    {
        [SerializeField] private string frameRoot = "Art/Geralt/Frames";
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float slashFramesPerSecond = 14f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float pixelsPerUnit = 96f;

        private readonly Dictionary<GeraltAnimation, Sprite[]> framesByAnimation = new Dictionary<GeraltAnimation, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private GeraltAnimation currentAnimation = GeraltAnimation.Idle;
        private int frameIndex;
        private float frameTimer;

        public bool IsSlashPlaying { get; private set; }
        public bool IsHurtPlaying { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadFrames();
            Play(GeraltAnimation.Idle, true);
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

        public void PlayLocomotion(bool isMoving, bool isGrounded = true)
        {
            if (IsSlashPlaying || IsHurtPlaying)
            {
                return;
            }

            if (!isGrounded)
            {
                Play(GeraltAnimation.Jump);
                return;
            }

            Play(isMoving ? GeraltAnimation.Run : GeraltAnimation.Idle);
        }

        public void PlayJump()
        {
            if (IsSlashPlaying || IsHurtPlaying)
            {
                return;
            }

            Play(GeraltAnimation.Jump, true);
        }

        public void PlaySlash()
        {
            if (IsHurtPlaying)
            {
                return;
            }

            IsSlashPlaying = true;
            Play(GeraltAnimation.Slash, true);
        }

        public void PlayHurt()
        {
            IsSlashPlaying = false;
            IsHurtPlaying = true;
            Play(GeraltAnimation.Hurt, true);
        }

        public void ForceIdle()
        {
            IsSlashPlaying = false;
            IsHurtPlaying = false;
            Play(GeraltAnimation.Idle, true);
        }

        private void Play(GeraltAnimation animation, bool restart = false)
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
            bool isOneShot = currentAnimation == GeraltAnimation.Slash || currentAnimation == GeraltAnimation.Hurt;
            if (isOneShot && frameIndex >= frames.Length)
            {
                IsSlashPlaying = false;
                IsHurtPlaying = false;
                Play(GeraltAnimation.Idle, true);
                return;
            }

            frameIndex %= frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private Sprite[] GetFrames(GeraltAnimation animation)
        {
            return framesByAnimation.TryGetValue(animation, out Sprite[] frames) ? frames : System.Array.Empty<Sprite>();
        }

        private float GetFramesPerSecond(GeraltAnimation animation)
        {
            switch (animation)
            {
                case GeraltAnimation.Run:
                    return runFramesPerSecond;
                case GeraltAnimation.Jump:
                    return jumpFramesPerSecond;
                case GeraltAnimation.Slash:
                    return slashFramesPerSecond;
                case GeraltAnimation.Hurt:
                    return hurtFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void LoadFrames()
        {
            LoadFrames(GeraltAnimation.Idle);
            LoadFrames(GeraltAnimation.Run);
            LoadFrames(GeraltAnimation.Jump);
            LoadFrames(GeraltAnimation.Slash);
            LoadFrames(GeraltAnimation.Hurt);
        }

        private void LoadFrames(GeraltAnimation animation)
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

        private Sprite[] LoadFallbackFrames(GeraltAnimation animation)
        {
            int frameCount = WitcherSpriteLibrary.GetGeraltFrameCount(animation);
            Sprite[] sprites = new Sprite[frameCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = WitcherSpriteLibrary.GetGeraltFrame(animation, i);
            }

            return sprites;
        }
    }
}
