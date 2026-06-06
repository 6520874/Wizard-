using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
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

            string relativeFolder = Path.Combine("Art/Party", visualFolder, "Frames", GetFolderName(animation));
            string folderPath = Path.Combine(Application.dataPath, relativeFolder);
            string key = Directory.Exists(folderPath)
                ? $"{relativeFolder}_{Directory.GetFiles(folderPath, "*.png").Length}_{Directory.GetLastWriteTimeUtc(folderPath).Ticks}"
                : $"{relativeFolder}_missing";

            if (CachedFrames.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            if (!Directory.Exists(folderPath))
            {
                CachedFrames[key] = System.Array.Empty<Sprite>();
                return CachedFrames[key];
            }

            List<Sprite> frames = new List<Sprite>();
            foreach (string filePath in Directory.GetFiles(folderPath, "*.png").OrderBy(path => path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(filePath)))
                {
                    continue;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f),
                    PartyPixelsPerUnit);
                sprite.name = Path.GetFileNameWithoutExtension(filePath);
                frames.Add(sprite);
            }

            CachedFrames[key] = frames.ToArray();
            return CachedFrames[key];
        }

        private static string GetFolderName(PartyAnimationKind animation)
        {
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
            if (memberName == "叶奈法")
            {
                return "Yennefer";
            }

            if (memberName == "特莉丝")
            {
                return "Triss";
            }

            return string.Empty;
        }
    }
}
