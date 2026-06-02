using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    // 中文说明：根据玩家动作切换主角的待机、移动、攻击、受击和死亡动画。
    public class GeraltAnimator : MonoBehaviour
    {
        [SerializeField] private string frameRoot = "Art/Geralt/Frames";
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float slashFramesPerSecond = 14f;
        [SerializeField] private float hurtFramesPerSecond = 12f;
        [SerializeField] private float deathFramesPerSecond = 9f;
        [SerializeField] private float pixelsPerUnit = 82f;

        private readonly Dictionary<GeraltAnimation, Sprite[]> framesByAnimation = new Dictionary<GeraltAnimation, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private GeraltAnimation currentAnimation = GeraltAnimation.Idle;
        private GeraltAnimation lastLocomotionAnimation = GeraltAnimation.Run;
        private GeraltAnimation directionalIdleAnimation = GeraltAnimation.Idle;
        private int frameIndex;
        private float frameTimer;
        private bool holdDirectionalIdle;

        public bool IsSlashPlaying { get; private set; }
        public bool IsSkillPlaying { get; private set; }
        public bool IsHurtPlaying { get; private set; }
        public bool IsDeathPlaying { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadFrames();
            Play(GeraltAnimation.Idle, true);
        }

        private void Update()
        {
            if (holdDirectionalIdle)
            {
                return;
            }

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
            PlayLocomotion(isMoving ? Vector2.right : Vector2.zero, isGrounded);
        }

        public void PlayLocomotion(Vector2 movement, bool isGrounded = true)
        {
            if (IsSlashPlaying || IsSkillPlaying || IsHurtPlaying || IsDeathPlaying)
            {
                return;
            }

            if (!isGrounded)
            {
                Play(GeraltAnimation.Jump);
                return;
            }

            if (movement.sqrMagnitude <= 0.01f)
            {
                HoldDirectionalIdleFrame();
                return;
            }

            lastLocomotionAnimation = GetRunAnimation(movement);
            directionalIdleAnimation = GetDirectionalIdleAnimation(lastLocomotionAnimation);
            Play(lastLocomotionAnimation);
        }

        public void PlayJump()
        {
            if (IsSlashPlaying || IsSkillPlaying || IsHurtPlaying || IsDeathPlaying)
            {
                return;
            }

            Play(GeraltAnimation.Jump, true);
        }

        public void PlaySlash()
        {
            PlaySkillAnimation(GeraltAnimation.Slash);
        }

        public void PlaySkillAnimation(GeraltAnimation animation)
        {
            if (IsSlashPlaying || IsSkillPlaying || IsHurtPlaying || IsDeathPlaying)
            {
                return;
            }

            IsSlashPlaying = animation == GeraltAnimation.Slash;
            IsSkillPlaying = animation != GeraltAnimation.Slash;
            Play(animation, true);
        }

        public void PlayHurt()
        {
            if (IsDeathPlaying)
            {
                return;
            }

            IsSlashPlaying = false;
            IsSkillPlaying = false;
            IsHurtPlaying = true;
            Play(GeraltAnimation.Hurt, true);
        }

        public void PlayDeath()
        {
            IsSlashPlaying = false;
            IsSkillPlaying = false;
            IsHurtPlaying = false;
            IsDeathPlaying = true;
            Play(GeraltAnimation.Death, true);
        }

        public void ForceIdle()
        {
            if (IsDeathPlaying)
            {
                return;
            }

            IsSlashPlaying = false;
            IsSkillPlaying = false;
            IsHurtPlaying = false;
            HoldDirectionalIdleFrame(true);
        }

        public Sprite[] GetFramesForBattleHud(GeraltAnimation animation)
        {
            Sprite[] frames = GetFrames(animation);
            return frames.Length > 0 ? frames : LoadFallbackFrames(animation);
        }

        private void Play(GeraltAnimation animation, bool restart = false)
        {
            if (!restart && currentAnimation == animation && !holdDirectionalIdle)
            {
                return;
            }

            holdDirectionalIdle = false;
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
            if (currentAnimation == GeraltAnimation.Death && frameIndex >= frames.Length)
            {
                frameIndex = frames.Length - 1;
                spriteRenderer.sprite = frames[frameIndex];
                return;
            }

            bool isOneShot = IsOneShotAnimation(currentAnimation);
            if (isOneShot && frameIndex >= frames.Length)
            {
                IsSlashPlaying = false;
                IsSkillPlaying = false;
                IsHurtPlaying = false;
                HoldDirectionalIdleFrame(true);
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
                case GeraltAnimation.RunDown:
                case GeraltAnimation.RunUp:
                    return runFramesPerSecond;
                case GeraltAnimation.Jump:
                    return jumpFramesPerSecond;
                case GeraltAnimation.Slash:
                case GeraltAnimation.FlameSign:
                case GeraltAnimation.ShieldSign:
                case GeraltAnimation.PurpleSign:
                    return slashFramesPerSecond;
                case GeraltAnimation.Hurt:
                    return hurtFramesPerSecond;
                case GeraltAnimation.Death:
                    return deathFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void LoadFrames()
        {
            LoadFrames(GeraltAnimation.Idle);
            LoadFrames(GeraltAnimation.Run);
            LoadFrames(GeraltAnimation.RunDown);
            LoadFrames(GeraltAnimation.RunUp);
            LoadFrames(GeraltAnimation.FlameSign);
            LoadFrames(GeraltAnimation.ShieldSign);
            LoadFrames(GeraltAnimation.PurpleSign);
            LoadFrames(GeraltAnimation.Jump);
            LoadFrames(GeraltAnimation.Slash);
            LoadFrames(GeraltAnimation.Hurt);
            LoadFrames(GeraltAnimation.Death);
        }

        private GeraltAnimation GetRunAnimation(Vector2 movement)
        {
            float absoluteX = Mathf.Abs(movement.x);
            float absoluteY = Mathf.Abs(movement.y);
            if (absoluteY > 0.01f && absoluteY >= absoluteX * 0.65f)
            {
                return movement.y > 0f ? GeraltAnimation.RunUp : GeraltAnimation.RunDown;
            }

            return GeraltAnimation.Run;
        }

        private void HoldDirectionalIdleFrame(bool restart = false)
        {
            Sprite[] frames = GetFrames(directionalIdleAnimation);
            if (frames.Length == 0)
            {
                Play(GeraltAnimation.Idle, restart);
                return;
            }

            if (!restart && holdDirectionalIdle && currentAnimation == directionalIdleAnimation)
            {
                return;
            }

            holdDirectionalIdle = true;
            currentAnimation = directionalIdleAnimation;
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private static GeraltAnimation GetDirectionalIdleAnimation(GeraltAnimation locomotionAnimation)
        {
            switch (locomotionAnimation)
            {
                case GeraltAnimation.RunUp:
                    return GeraltAnimation.RunUp;
                case GeraltAnimation.RunDown:
                    return GeraltAnimation.RunDown;
                default:
                    return GeraltAnimation.Idle;
            }
        }

        private static bool IsOneShotAnimation(GeraltAnimation animation)
        {
            return animation == GeraltAnimation.Slash
                || animation == GeraltAnimation.FlameSign
                || animation == GeraltAnimation.ShieldSign
                || animation == GeraltAnimation.PurpleSign
                || animation == GeraltAnimation.Hurt;
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

                texture.filterMode = FilterMode.Bilinear;
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
