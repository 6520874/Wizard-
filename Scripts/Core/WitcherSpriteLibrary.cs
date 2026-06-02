using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public enum GeraltAnimation
    {
        Idle,
        Run,
        RunDown,
        RunUp,
        Jump,
        Slash,
        Hurt,
        Death,
        FlameSign,
        ShieldSign,
        PurpleSign
    }

    // 中文说明：集中提供主角和 UI 使用的运行时精灵图读取与占位资源。
    public static class WitcherSpriteLibrary
    {
        private static readonly Dictionary<Color32, Sprite> CachedSprites = new Dictionary<Color32, Sprite>();
        private static readonly Dictionary<string, Sprite> CachedGeraltSprites = new Dictionary<string, Sprite>();

        public static Sprite GetSolidSprite(Color32 color)
        {
            if (CachedSprites.TryGetValue(color, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, color);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = $"WitcherSprite_{color.r}_{color.g}_{color.b}_{color.a}";
            CachedSprites[color] = sprite;
            return sprite;
        }

        public static int GetGeraltFrameCount(GeraltAnimation animation)
        {
            switch (animation)
            {
                case GeraltAnimation.Run:
                case GeraltAnimation.RunDown:
                case GeraltAnimation.RunUp:
                case GeraltAnimation.FlameSign:
                case GeraltAnimation.ShieldSign:
                case GeraltAnimation.PurpleSign:
                    return 9;
                case GeraltAnimation.Jump:
                    return 5;
                case GeraltAnimation.Slash:
                    return 7;
                case GeraltAnimation.Hurt:
                    return 7;
                case GeraltAnimation.Death:
                    return 6;
                default:
                    return 9;
            }
        }

        public static Sprite GetGeraltFrame(GeraltAnimation animation, int frame)
        {
            int frameCount = GetGeraltFrameCount(animation);
            int normalizedFrame = Mathf.Abs(frame) % frameCount;
            string key = $"{animation}_{normalizedFrame}";

            if (CachedGeraltSprites.TryGetValue(key, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Clear(texture);
            DrawGeralt(texture, animation, normalizedFrame);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 24f);
            sprite.name = $"Geralt_{animation}_{normalizedFrame}";
            CachedGeraltSprites[key] = sprite;
            return sprite;
        }

        private static void DrawGeralt(Texture2D texture, GeraltAnimation animation, int frame)
        {
            int step = frame % 2 == 0 ? -1 : 1;
            bool isRunAnimation = animation == GeraltAnimation.Run || animation == GeraltAnimation.RunDown || animation == GeraltAnimation.RunUp;
            int bob = isRunAnimation && (frame == 1 || frame == 4) ? 1 : 0;
            if (animation == GeraltAnimation.Jump)
            {
                bob = 2;
            }

            int armSwing = isRunAnimation ? step : 0;

            Color32 outline = new Color32(24, 22, 20, 255);
            Color32 skin = new Color32(231, 190, 151, 255);
            Color32 hair = new Color32(214, 211, 197, 255);
            Color32 armor = new Color32(111, 121, 124, 255);
            Color32 leather = new Color32(54, 45, 39, 255);
            Color32 red = new Color32(121, 38, 43, 255);
            Color32 boot = new Color32(24, 23, 22, 255);
            Color32 sword = new Color32(202, 212, 218, 255);
            Color32 swordShade = new Color32(125, 139, 146, 255);
            Color32 slash = new Color32(235, 242, 245, 160);

            if (animation == GeraltAnimation.Death)
            {
                DrawDeadGeralt(texture, frame, outline, skin, hair, armor, leather, red, boot, sword, swordShade);
                return;
            }

            DrawLine(texture, 11, 21 + bob, 5, 30 + bob, swordShade);
            DrawLine(texture, 12, 21 + bob, 6, 30 + bob, sword);
            Fill(texture, 9, 17 + bob, 22, 20 + bob, outline);
            Fill(texture, 10, 18 + bob, 21, 20 + bob, leather);
            Fill(texture, 11, 11 + bob, 20, 18 + bob, outline);
            Fill(texture, 12, 12 + bob, 19, 18 + bob, armor);
            Fill(texture, 13, 13 + bob, 18, 16 + bob, leather);
            Fill(texture, 12, 10 + bob, 19, 12 + bob, red);

            DrawLegs(texture, animation, frame, bob, outline, leather, boot);
            DrawArms(texture, animation, frame, bob, armSwing, outline, leather, skin, sword, slash);

            Fill(texture, 11, 22 + bob, 21, 27 + bob, outline);
            Fill(texture, 12, 22 + bob, 20, 27 + bob, hair);
            Fill(texture, 13, 21 + bob, 20, 25 + bob, skin);
            Fill(texture, 12, 25 + bob, 21, 29 + bob, hair);
            Fill(texture, 17, 23 + bob, 18, 24 + bob, outline);
            Fill(texture, 20, 22 + bob, 21, 24 + bob, hair);
            Fill(texture, 14, 20 + bob, 19, 21 + bob, outline);
            Fill(texture, 15, 20 + bob, 18, 21 + bob, hair);
        }

        private static void DrawLegs(Texture2D texture, GeraltAnimation animation, int frame, int bob, Color32 outline, Color32 leather, Color32 boot)
        {
            bool isRunAnimation = animation == GeraltAnimation.Run || animation == GeraltAnimation.RunDown || animation == GeraltAnimation.RunUp;
            int forward = isRunAnimation && frame % 2 == 0 ? 2 : 0;
            int back = isRunAnimation && frame % 2 != 0 ? 2 : 0;
            if (animation == GeraltAnimation.Jump)
            {
                forward = frame < 2 ? 1 : 3;
                back = frame < 2 ? 2 : 1;
            }

            Fill(texture, 11 - back, 5 + bob, 14 - back, 11 + bob, outline);
            Fill(texture, 12 - back, 6 + bob, 14 - back, 11 + bob, leather);
            Fill(texture, 9 - back, 3 + bob, 15 - back, 5 + bob, boot);

            Fill(texture, 17 + forward, 5 + bob, 20 + forward, 11 + bob, outline);
            Fill(texture, 17 + forward, 6 + bob, 19 + forward, 11 + bob, leather);
            Fill(texture, 16 + forward, 3 + bob, 22 + forward, 5 + bob, boot);
        }

        private static void DrawArms(Texture2D texture, GeraltAnimation animation, int frame, int bob, int armSwing, Color32 outline, Color32 leather, Color32 skin, Color32 sword, Color32 slash)
        {
            if (animation == GeraltAnimation.Slash)
            {
                int reach = frame < 2 ? frame * 2 : 4;
                Fill(texture, 20, 15 + bob, 25 + reach, 18 + bob, outline);
                Fill(texture, 20, 16 + bob, 24 + reach, 17 + bob, leather);
                Fill(texture, 24 + reach, 16 + bob, 26 + reach, 17 + bob, skin);

                if (frame <= 1)
                {
                    DrawLine(texture, 24, 18 + bob, 31, 25 + bob, sword);
                }
                else if (frame == 2)
                {
                    DrawLine(texture, 24, 18 + bob, 31, 18 + bob, sword);
                    DrawLine(texture, 26, 22 + bob, 31, 17 + bob, slash);
                }
                else
                {
                    DrawLine(texture, 23, 17 + bob, 30, 10 + bob, sword);
                    DrawLine(texture, 25, 20 + bob, 31, 12 + bob, slash);
                }

                return;
            }

            Fill(texture, 8 - armSwing, 13 + bob, 12 - armSwing, 17 + bob, outline);
            Fill(texture, 9 - armSwing, 14 + bob, 12 - armSwing, 16 + bob, leather);
            Fill(texture, 7 - armSwing, 12 + bob, 9 - armSwing, 14 + bob, skin);

            Fill(texture, 20 + armSwing, 13 + bob, 24 + armSwing, 17 + bob, outline);
            Fill(texture, 20 + armSwing, 14 + bob, 23 + armSwing, 16 + bob, leather);
            Fill(texture, 23 + armSwing, 12 + bob, 25 + armSwing, 14 + bob, skin);
        }

        private static void DrawDeadGeralt(Texture2D texture, int frame, Color32 outline, Color32 skin, Color32 hair, Color32 armor, Color32 leather, Color32 red, Color32 boot, Color32 sword, Color32 swordShade)
        {
            int drop = Mathf.Min(frame, 4);
            int y = 6 - drop / 2;
            if (frame < 3)
            {
                Fill(texture, 10, 12 - drop, 21, 18 - drop, outline);
                Fill(texture, 11, 13 - drop, 20, 17 - drop, armor);
                Fill(texture, 8, 8, 13, 12, outline);
                Fill(texture, 18, 8, 23, 12, outline);
                Fill(texture, 11, 20 - drop, 20, 26 - drop, outline);
                Fill(texture, 12, 20 - drop, 19, 25 - drop, hair);
                Fill(texture, 13, 19 - drop, 19, 23 - drop, skin);
                return;
            }

            DrawLine(texture, 7, y + 10, 25, y + 15, swordShade);
            DrawLine(texture, 8, y + 11, 26, y + 16, sword);
            Fill(texture, 7, y + 4, 25, y + 10, outline);
            Fill(texture, 8, y + 5, 24, y + 9, armor);
            Fill(texture, 12, y + 5, 18, y + 7, leather);
            Fill(texture, 18, y + 4, 25, y + 6, boot);
            Fill(texture, 6, y + 8, 13, y + 14, outline);
            Fill(texture, 7, y + 9, 12, y + 13, hair);
            Fill(texture, 8, y + 8, 13, y + 11, skin);
            Fill(texture, 10, y + 10, 11, y + 11, red);
        }

        private static void Clear(Texture2D texture)
        {
            Color32 transparent = new Color32(0, 0, 0, 0);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        private static void Fill(Texture2D texture, int minX, int minY, int maxX, int maxY, Color32 color)
        {
            int clampedMinX = Mathf.Clamp(minX, 0, texture.width - 1);
            int clampedMaxX = Mathf.Clamp(maxX, 0, texture.width - 1);
            int clampedMinY = Mathf.Clamp(minY, 0, texture.height - 1);
            int clampedMaxY = Mathf.Clamp(maxY, 0, texture.height - 1);

            for (int y = clampedMinY; y <= clampedMaxY; y++)
            {
                for (int x = clampedMinX; x <= clampedMaxX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                Fill(texture, x0, y0, x0 + 1, y0 + 1, color);

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int doubledError = 2 * error;
                if (doubledError >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (doubledError <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }
    }
}
