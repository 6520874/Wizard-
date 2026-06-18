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
        BlackNailPuppet,
        CrowboneStitcher
    }

    // 中文说明：按怪物类型加载回合制战斗使用的待机、攻击和受击帧。
    public static class TurnBasedEnemyAnimationLibrary
    {
        private const float MonsterPixelsPerUnit = 96f;
        private const float BossPixelsPerUnit = 256f;
        private static readonly Dictionary<string, Sprite[]> CachedFolders = new Dictionary<string, Sprite[]>();

        public static void FillAnimations(TurnBasedEnemyState enemy, TurnBasedEnemyVisualKind visualKind)
        {
            enemy.VisualKind = visualKind;
            switch (visualKind)
            {
                case TurnBasedEnemyVisualKind.CorruptedWolf:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/CorruptedWolf/Reworked/Frames/Idle", MonsterPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/CorruptedWolf/Reworked/Frames/Attack", MonsterPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/CorruptedWolf/Reworked/Frames/Hurt", MonsterPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/CorruptedWolf/Reworked/Frames/Down", MonsterPixelsPerUnit);
                    break;
                case TurnBasedEnemyVisualKind.BloodWraith:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/BloodWraith/Reworked/Frames/Idle", MonsterPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/BloodWraith/Reworked/Frames/Attack", MonsterPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/BloodWraith/Reworked/Frames/Hurt", MonsterPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/BloodWraith/Reworked/Frames/Down", MonsterPixelsPerUnit);
                    break;
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/BlackMoonKnight/Reworked/Frames/Idle", BossPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/BlackMoonKnight/Reworked/Frames/Attack", BossPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/BlackMoonKnight/Reworked/Frames/Hurt", BossPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/BlackMoonKnight/Reworked/Frames/Down", BossPixelsPerUnit);
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
                case TurnBasedEnemyVisualKind.CrowboneStitcher:
                    enemy.IdleFrames = LoadFolderFrames("Art/Monsters/CrowboneStitcher/Reworked/Frames/Idle", MonsterPixelsPerUnit);
                    enemy.AttackFrames = LoadFolderFrames("Art/Monsters/CrowboneStitcher/Reworked/Frames/Attack", MonsterPixelsPerUnit);
                    enemy.HurtFrames = LoadFolderFrames("Art/Monsters/CrowboneStitcher/Reworked/Frames/Hurt", MonsterPixelsPerUnit);
                    enemy.DefeatFrames = LoadFolderFrames("Art/Monsters/CrowboneStitcher/Reworked/Frames/Down", MonsterPixelsPerUnit);
                    break;
            }

            enemy.Sprite = FirstValid(enemy.IdleFrames, enemy.Sprite);
        }

        private static Sprite FirstValid(Sprite[] frames, Sprite fallback)
        {
            return frames != null && frames.Length > 0 && frames[0] != null ? frames[0] : fallback;
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
