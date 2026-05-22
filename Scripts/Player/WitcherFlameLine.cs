using System.IO;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：控制主角释放的直线火焰攻击表现和范围命中。
    public class WitcherFlameLine : MonoBehaviour
    {
        private const int SortingBoost = 38;
        private const string FlameSheetPath = "Art/Effects/HunterFlameBeamSheet.png";
        private const string FlameSpritePath = "Art/Effects/HunterFlameBeam.png";
        private const int FlameSheetColumns = 4;
        private const int FlameSheetRows = 4;
        private const float FlameSpritePixelsPerUnit = 256f;

        private static Sprite[] cachedFlameFrames;
        private static Sprite cachedFlameSprite;

        private SpriteRenderer flameRenderer;
        private Transform flameLayer;
        private float lifetime = 0.32f;
        private float timer;
        private float startAlpha;
        private float targetLength;
        private float targetVisualHeight;
        private float direction;

        public static void Spawn(Vector3 origin, float facingDirection, float length, float width, int damage, float duration)
        {
            GameObject flameObject = new GameObject("Hunter Flame Line");
            WitcherFlameLine flame = flameObject.AddComponent<WitcherFlameLine>();
            flame.Configure(origin, facingDirection, length, width, damage, duration);
        }

        private void Configure(Vector3 origin, float facingDirection, float length, float width, int damage, float duration)
        {
            direction = facingDirection >= 0f ? 1f : -1f;
            lifetime = Mathf.Max(0.08f, duration);
            transform.position = origin + new Vector3(direction * length * 0.5f, 0f, 0f);
            targetLength = length;
            targetVisualHeight = Mathf.Max(0.74f, width * 1.75f);

            flameRenderer = CreateFlameRenderer(length, width, direction);
            startAlpha = flameRenderer.color.a;

            int sortingOrder = Mathf.RoundToInt((5f - origin.y) * 100f) + SortingBoost;
            flameRenderer.sortingOrder = sortingOrder;

            ApplyDamage(origin, direction, length, width, damage);
        }

        private SpriteRenderer CreateFlameRenderer(float length, float hitWidth, float direction)
        {
            GameObject layer = new GameObject("Flame Sprite");
            layer.transform.SetParent(transform, false);
            layer.transform.localPosition = Vector3.zero;
            flameLayer = layer.transform;

            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            Sprite[] frames = LoadFlameFrames();
            renderer.sprite = frames.Length > 0 ? frames[0] : LoadFlameSprite();
            renderer.color = Color.white;
            renderer.flipX = direction < 0f;

            if (renderer.sprite == null)
            {
                renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 92, 17, 220));
                renderer.color = new Color32(255, 92, 17, 220);
                layer.transform.localScale = new Vector3(length, hitWidth, 1f);
                return renderer;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            layer.transform.localScale = new Vector3(
                length / spriteSize.x,
                targetVisualHeight / spriteSize.y,
                1f);
            return renderer;
        }

        private static Sprite[] LoadFlameFrames()
        {
            if (cachedFlameFrames != null)
            {
                return cachedFlameFrames;
            }

            string absolutePath = Path.Combine(Application.dataPath, FlameSheetPath);
            if (!File.Exists(absolutePath))
            {
                cachedFlameFrames = System.Array.Empty<Sprite>();
                return cachedFlameFrames;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Debug.LogWarning($"Could not load flame beam sheet: {absolutePath}");
                cachedFlameFrames = System.Array.Empty<Sprite>();
                return cachedFlameFrames;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            int frameWidth = texture.width / FlameSheetColumns;
            int frameHeight = texture.height / FlameSheetRows;
            cachedFlameFrames = new Sprite[FlameSheetColumns * FlameSheetRows];
            for (int row = 0; row < FlameSheetRows; row++)
            {
                for (int column = 0; column < FlameSheetColumns; column++)
                {
                    int index = row * FlameSheetColumns + column;
                    Rect rect = new Rect(column * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    Sprite frame = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), FlameSpritePixelsPerUnit);
                    frame.name = $"HunterFlameBeam_{index:00}";
                    cachedFlameFrames[index] = frame;
                }
            }

            return cachedFlameFrames;
        }

        private static Sprite LoadFlameSprite()
        {
            if (cachedFlameSprite != null)
            {
                return cachedFlameSprite;
            }

            string absolutePath = Path.Combine(Application.dataPath, FlameSpritePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"Flame beam image not found: {absolutePath}");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Debug.LogWarning($"Could not load flame beam image: {absolutePath}");
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            cachedFlameSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                FlameSpritePixelsPerUnit);
            cachedFlameSprite.name = "HunterFlameBeam";
            return cachedFlameSprite;
        }

        private static void ApplyDamage(Vector3 origin, float direction, float length, float width, int damage)
        {
            WildHuntBossController[] bosses = FindObjectsOfType<WildHuntBossController>();
            for (int i = 0; i < bosses.Length; i++)
            {
                WildHuntBossController boss = bosses[i];
                if (boss != null && IsInsideFlame(origin, direction, length, width, boss.transform.position))
                {
                    boss.TakeMagicHit(damage, origin.x);
                }
            }

            MonsterPatrol[] monsters = FindObjectsOfType<MonsterPatrol>();
            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterPatrol monster = monsters[i];
                if (monster != null && IsInsideFlame(origin, direction, length, width, monster.transform.position))
                {
                    monster.TakeMagicHit(damage, origin.x);
                }
            }

            WitcherHordeMonsterController[] hordeMonsters = FindObjectsOfType<WitcherHordeMonsterController>();
            for (int i = 0; i < hordeMonsters.Length; i++)
            {
                WitcherHordeMonsterController monster = hordeMonsters[i];
                if (monster != null && IsInsideFlame(origin, direction, length, width, monster.transform.position))
                {
                    monster.TakeMagicHit(damage, origin.x);
                }
            }
        }

        private static bool IsInsideFlame(Vector3 origin, float direction, float length, float width, Vector3 target)
        {
            float forwardDistance = (target.x - origin.x) * direction;
            if (forwardDistance < -0.15f || forwardDistance > length)
            {
                return false;
            }

            return Mathf.Abs(target.y - origin.y) <= width * 0.5f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / lifetime);
            Sprite[] frames = LoadFlameFrames();
            if (frames.Length > 0)
            {
                int frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(normalized * frames.Length));
                flameRenderer.sprite = frames[frameIndex];
                ScaleCurrentFrame(normalized);
            }

            float pulse = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.12f;
            transform.localScale = new Vector3(1f, pulse, 1f);

            Fade(flameRenderer, normalized, startAlpha);

            if (normalized >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void ScaleCurrentFrame(float normalized)
        {
            if (flameRenderer == null || flameRenderer.sprite == null || flameLayer == null)
            {
                return;
            }

            Vector2 spriteSize = flameRenderer.sprite.bounds.size;
            float lengthGrowth = Mathf.Clamp01(normalized / 0.34f);
            float visualLength = Mathf.Lerp(targetLength * 0.28f, targetLength, lengthGrowth);
            float fadeTail = normalized > 0.72f ? Mathf.Lerp(1f, 0.82f, (normalized - 0.72f) / 0.28f) : 1f;
            flameLayer.localScale = new Vector3(
                visualLength / spriteSize.x,
                targetVisualHeight / spriteSize.y * fadeTail,
                1f);
            flameLayer.localPosition = new Vector3(direction * (visualLength - targetLength) * 0.5f, 0f, 0f);
        }

        private static void Fade(SpriteRenderer renderer, float normalized, float startAlpha)
        {
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.color;
            color.a = startAlpha * (1f - normalized);
            renderer.color = color;
        }
    }
}
