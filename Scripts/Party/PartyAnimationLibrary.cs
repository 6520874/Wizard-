using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：定义队伍成员在地图和战斗里会用到的动画类型。
    public enum PartyAnimationKind
    {
        Idle,
        Run,
        Up,
        DownWalk,
        Attack,
        Cast,
        Special,
        Hurt,
        Down
    }

    // 中文说明：从角色切图目录读取队伍成员动画帧，并提供待机预览图。
    public static class PartyAnimationLibrary
    {
        private const float PartyPixelsPerUnit = 96f;
        private static readonly Dictionary<string, Sprite[]> CachedFrames = new Dictionary<string, Sprite[]>();

        public static Sprite[] GetFrames(PartyMember member, PartyAnimationKind animation)
        {
            if (member == null)
            {
                return System.Array.Empty<Sprite>();
            }

            return GetFrames(GetVisualFolder(member.Name), animation);
        }

        public static Sprite[] GetFrames(string visualFolder, PartyAnimationKind animation)
        {
            if (string.IsNullOrEmpty(visualFolder))
            {
                return System.Array.Empty<Sprite>();
            }

            string relativeFolder = Path.Combine(GetFrameRoot(visualFolder), GetFolderName(visualFolder, animation));
            string folderPath = Path.Combine(Application.dataPath, relativeFolder);
            string key = BuildCacheKey(relativeFolder, folderPath);

            if (CachedFrames.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            CachedFrames[key] = Directory.Exists(folderPath)
                ? LoadSpritesFromFolder(folderPath)
                : System.Array.Empty<Sprite>();
            return CachedFrames[key];
        }

        private static string BuildCacheKey(string relativeFolder, string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return $"{relativeFolder}_missing";
            }

            int pngCount = Directory.GetFiles(folderPath, "*.png").Length;
            long writeTime = Directory.GetLastWriteTimeUtc(folderPath).Ticks;
            return $"{relativeFolder}_{pngCount}_{writeTime}";
        }

        private static Sprite[] LoadSpritesFromFolder(string folderPath)
        {
            List<Sprite> frames = new List<Sprite>();
            foreach (string filePath in Directory.GetFiles(folderPath, "*.png").OrderBy(path => path))
            {
                Sprite sprite = LoadSprite(filePath);
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }

            return frames.ToArray();
        }

        private static Sprite LoadSprite(string filePath)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(filePath)))
            {
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.08f),
                PartyPixelsPerUnit);
            sprite.name = Path.GetFileNameWithoutExtension(filePath);
            return sprite;
        }

        private static string GetFrameRoot(string visualFolder)
        {
            return visualFolder == "Geralt"
                ? Path.Combine("Art", "Geralt", "Frames")
                : Path.Combine("Art", "Party", visualFolder, "Frames");
        }

        private static string GetFolderName(string visualFolder, PartyAnimationKind animation)
        {
            if (visualFolder == "Geralt")
            {
                switch (animation)
                {
                    case PartyAnimationKind.Up:
                        return "RunUp";
                    case PartyAnimationKind.DownWalk:
                        return "RunDown";
                    case PartyAnimationKind.Attack:
                        return "Slash";
                    case PartyAnimationKind.Cast:
                        return "FlameSign";
                    case PartyAnimationKind.Special:
                        return "PurpleSign";
                    case PartyAnimationKind.Down:
                        return "Death";
                }
            }

            switch (animation)
            {
                case PartyAnimationKind.DownWalk:
                    return "DownWalk";
                default:
                    return animation.ToString();
            }
        }

        public static Sprite GetIdlePreview(PartyMember member)
        {
            Sprite[] frames = GetFrames(member, PartyAnimationKind.Idle);
            return frames.Length > 0 ? frames[0] : PartyPortraitLibrary.GetPortrait(member);
        }

        public static string GetVisualFolder(string memberName)
        {
            if (memberName == GameText.HunterName)
            {
                return "Geralt";
            }

            if (memberName == GameText.YenneferName)
            {
                return "Yennefer";
            }

            if (memberName == GameText.TrissName)
            {
                return "Triss";
            }

            return string.Empty;
        }
    }
}
