using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：根据对话说话人名称加载剧情对话框头像，优先使用正式资源，缺失时回退为占位图。
    public static class DialoguePortraitLibrary
    {
        private const string DialogueCharactersPath = "Art/Story/DialogueCharacters.png";
        private const string GeraltPortraitPath = "Art/UI/GeraltPortrait.png";
        private static readonly Dictionary<string, Sprite> CachedPortraits = new Dictionary<string, Sprite>();

        public static Sprite GetPortrait(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
            {
                return null;
            }

            string normalizedName = speakerName.Trim();
            if (CachedPortraits.TryGetValue(normalizedName, out Sprite cached))
            {
                return cached;
            }

            Sprite portrait = LoadPortrait(normalizedName);
            if (portrait != null)
            {
                CachedPortraits[normalizedName] = portrait;
            }

            return portrait;
        }

        private static Sprite LoadPortrait(string speakerName)
        {
            if (speakerName == "猎魔人" || speakerName == "灰鸦猎人")
            {
                return WitcherSpriteLibrary.GetGeraltFrame(GeraltAnimation.Idle, 0)
                    ?? LoadSingleFilePortrait(GeraltPortraitPath, "Dialogue_Geralt_Portrait", 128f);
            }

            CharacterCrop crop = GetCharacterCrop(speakerName);
            if (!crop.IsValid)
            {
                return null;
            }

            string absolutePath = Path.Combine(Application.dataPath, DialogueCharactersPath);
            if (!File.Exists(absolutePath))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(30, 22, 18, 255));
            }

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(30, 22, 18, 255));
            }

            source.filterMode = FilterMode.Bilinear;
            source.wrapMode = TextureWrapMode.Clamp;
            Rect pixelRect = crop.ToUnityRect(source.height);
            Texture2D cropped = CropTextureWithTransparentBlack(source, pixelRect);
            cropped.filterMode = FilterMode.Bilinear;
            cropped.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                cropped,
                new Rect(0f, 0f, cropped.width, cropped.height),
                new Vector2(0.5f, 0.08f),
                crop.PixelsPerUnit);
            sprite.name = $"Dialogue_{speakerName}_Portrait";
            return sprite;
        }

        private static Sprite LoadSingleFilePortrait(string relativePath, string spriteName, float pixelsPerUnit)
        {
            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = spriteName;
            return sprite;
        }

        private static CharacterCrop GetCharacterCrop(string speakerName)
        {
            switch (speakerName)
            {
                case "赤焰术师":
                case "特莉丝":
                    return new CharacterCrop(34, 16, 250, 392, 192f);
                case "夜鸦女术士":
                case "叶奈法":
                    return new CharacterCrop(318, 18, 535, 386, 192f);
                case "老村长":
                case "村长":
                case "委托":
                case "委托结算":
                    return new CharacterCrop(618, 22, 812, 382, 192f);
                case "铁匠":
                case "装备商":
                    return new CharacterCrop(884, 35, 1108, 382, 192f);
                case "神父":
                    return new CharacterCrop(1153, 24, 1370, 386, 192f);
                case "贵族使者":
                case "贵族":
                    return new CharacterCrop(1456, 26, 1652, 386, 192f);
                case "失踪孩子":
                case "孩子":
                    return new CharacterCrop(1700, 102, 1895, 386, 192f);
                case "寡妇":
                    return new CharacterCrop(1944, 18, 2148, 390, 192f);
                case "邪教徒":
                    return new CharacterCrop(38, 405, 262, 712, 192f);
                case "井底哭魂":
                case "月夜骑士":
                    return new CharacterCrop(282, 382, 544, 712, 210f);
                case "腐化狼":
                case "井边腐兽":
                    return new CharacterCrop(716, 476, 960, 690, 190f);
                case "吸血女妖":
                case "磨坊哭影":
                    return new CharacterCrop(1066, 428, 1356, 690, 190f);
                default:
                    return CharacterCrop.Invalid;
            }
        }

        private static Texture2D CropTextureWithTransparentBlack(Texture2D source, Rect rect)
        {
            int xMin = Mathf.Clamp(Mathf.RoundToInt(rect.xMin), 0, source.width - 1);
            int yMin = Mathf.Clamp(Mathf.RoundToInt(rect.yMin), 0, source.height - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 1, source.width - xMin);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 1, source.height - yMin);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 color = source.GetPixel(xMin + x, yMin + y);
                    if (color.a <= 4 || IsNearBlackBackground(color))
                    {
                        color.a = 0;
                    }

                    result.SetPixel(x, y, color);
                }
            }

            result.Apply();
            return result;
        }

        private static bool IsNearBlackBackground(Color32 color)
        {
            return color.r <= 4 && color.g <= 4 && color.b <= 4;
        }

        private readonly struct CharacterCrop
        {
            public static readonly CharacterCrop Invalid = new CharacterCrop(0, 0, 0, 0, 100f);

            private readonly int left;
            private readonly int top;
            private readonly int right;
            private readonly int bottom;

            public CharacterCrop(int left, int top, int right, int bottom, float pixelsPerUnit)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
                PixelsPerUnit = pixelsPerUnit;
            }

            public float PixelsPerUnit { get; }
            public bool IsValid => right > left && bottom > top;

            public Rect ToUnityRect(int textureHeight)
            {
                return new Rect(left, textureHeight - bottom, right - left, bottom - top);
            }
        }
    }
}
