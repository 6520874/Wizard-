using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：定义回合制战斗里敌人的视觉资源类型。
    public enum TurnBasedEnemyVisualKind
    {
        CorruptedWolf,
        BloodWraith,
        BlackMoonKnight,
        BlackNailThrall,
        BlackWaxGateShade,
        BlackWaxAcolyte,
        BlackNailPuppet
    }

    // 中文说明：按怪物类型加载回合制战斗使用的待机、攻击和受击帧。
    public static class TurnBasedEnemyAnimationLibrary
    {
        private const float MonsterPixelsPerUnit = 96f;
        private const float BossPixelsPerUnit = 256f;
        private static readonly Dictionary<string, Sprite[]> CachedRows = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite[]> CachedFolders = new Dictionary<string, Sprite[]>();

        public static void FillAnimations(TurnBasedEnemyState enemy, TurnBasedEnemyVisualKind visualKind)
        {
            enemy.VisualKind = visualKind;
            switch (visualKind)
            {
                case TurnBasedEnemyVisualKind.BloodWraith:
                    enemy.IdleFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 0, MonsterPixelsPerUnit);
                    enemy.AttackFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 1, MonsterPixelsPerUnit);
                    enemy.HurtFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 1, MonsterPixelsPerUnit);
                    break;
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    enemy.IdleFrames = LoadFolderFrames("Art/WildHuntBoss/Frames/Idle", BossPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/WildHuntBoss/Frames/Attack", BossPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/WildHuntBoss/Frames/Hurt", BossPixelsPerUnit);
                    break;
                case TurnBasedEnemyVisualKind.BlackWaxGateShade:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/BlackWaxGateShade/Reworked/Frames/Idle", BossPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/BlackWaxGateShade/Reworked/Frames/Attack", BossPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/BlackWaxGateShade/Reworked/Frames/Hurt", BossPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/BlackWaxGateShade/Reworked/Frames/Down", BossPixelsPerUnit);
                    break;
                case TurnBasedEnemyVisualKind.BlackNailThrall:
                case TurnBasedEnemyVisualKind.BlackWaxAcolyte:
                case TurnBasedEnemyVisualKind.BlackNailPuppet:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/BlackNailPuppet/Reworked/Frames/Idle", BossPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/BlackNailPuppet/Reworked/Frames/Attack", BossPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/BlackNailPuppet/Reworked/Frames/Hurt", BossPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/BlackNailPuppet/Reworked/Frames/Down", BossPixelsPerUnit);
                    break;
                default:
                    enemy.IdleFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 0, MonsterPixelsPerUnit);
                    enemy.AttackFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 1, MonsterPixelsPerUnit);
                    enemy.HurtFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 1, MonsterPixelsPerUnit);
                    break;
            }

            enemy.Sprite = FirstValid(enemy.IdleFrames, enemy.Sprite);
        }

        private static Sprite FirstValid(Sprite[] frames, Sprite fallback)
        {
            return frames != null && frames.Length > 0 && frames[0] != null ? frames[0] : fallback;
        }

        private static Sprite[] LoadFrameRow(string relativePath, int columns, int rows, int row, float pixelsPerUnit)
        {
            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            string key = File.Exists(absolutePath)
                ? $"{relativePath}_{columns}_{rows}_{row}_{File.GetLastWriteTimeUtc(absolutePath).Ticks}"
                : $"{relativePath}_{columns}_{rows}_{row}_missing";

            if (CachedRows.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            if (!File.Exists(absolutePath))
            {
                CachedRows[key] = System.Array.Empty<Sprite>();
                return CachedRows[key];
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                CachedRows[key] = System.Array.Empty<Sprite>();
                return CachedRows[key];
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            int frameWidth = texture.width / columns;
            int frameHeight = texture.height / rows;
            Sprite[] frames = new Sprite[columns];
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new Rect(column * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                frames[column] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.08f), pixelsPerUnit);
                frames[column].name = $"{Path.GetFileNameWithoutExtension(relativePath)}_turn_{row}_{column}";
            }

            CachedRows[key] = frames;
            return frames;
        }

        private static Sprite[] LoadSingleFrame(string relativePath, float pixelsPerUnit, string spriteName)
        {
            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            string key = File.Exists(absolutePath)
                ? $"{relativePath}_{File.GetLastWriteTimeUtc(absolutePath).Ticks}"
                : $"{relativePath}_missing";

            if (CachedRows.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            if (!File.Exists(absolutePath))
            {
                CachedRows[key] = System.Array.Empty<Sprite>();
                return CachedRows[key];
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                CachedRows[key] = System.Array.Empty<Sprite>();
                return CachedRows[key];
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.08f),
                pixelsPerUnit);
            sprite.name = spriteName;
            CachedRows[key] = new[] { sprite };
            return CachedRows[key];
        }

        private static Sprite[] LoadFolderFrames(string relativeFolder, float pixelsPerUnit)
        {
            string folderPath = Path.Combine(Application.dataPath, relativeFolder);
            string key = Directory.Exists(folderPath)
                ? $"{relativeFolder}_{Directory.GetFiles(folderPath, "*.png").Length}_{Directory.GetLastWriteTimeUtc(folderPath).Ticks}"
                : $"{relativeFolder}_missing";

            if (CachedFolders.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            if (!Directory.Exists(folderPath))
            {
                CachedFolders[key] = System.Array.Empty<Sprite>();
                return CachedFolders[key];
            }

            List<Sprite> frames = new List<Sprite>();
            foreach (string filePath in Directory.GetFiles(folderPath, "*.png").OrderBy(path => path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(filePath)))
                {
                    continue;
                }

                // Folder-loaded frames are scaled in the HUD, so keep them smoothed.
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f),
                    pixelsPerUnit);
                sprite.name = Path.GetFileNameWithoutExtension(filePath);
                frames.Add(sprite);
            }

            CachedFolders[key] = frames.ToArray();
            return CachedFolders[key];
        }
    }
}
