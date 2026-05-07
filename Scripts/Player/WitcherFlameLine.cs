using System.IO;
using UnityEngine;

namespace WitcherGame
{
    public class WitcherFlameLine : MonoBehaviour
    {
        private const int SortingBoost = 38;
        private const string FlameSpritePath = "Art/Effects/HunterFlameBeam.png";
        private const float FlameSpritePixelsPerUnit = 256f;

        private static Sprite cachedFlameSprite;

        private SpriteRenderer flameRenderer;
        private float lifetime = 0.32f;
        private float timer;
        private float startAlpha;

        public static void Spawn(Vector3 origin, float facingDirection, float length, float width, int damage, float duration)
        {
            GameObject flameObject = new GameObject("Hunter Flame Line");
            WitcherFlameLine flame = flameObject.AddComponent<WitcherFlameLine>();
            flame.Configure(origin, facingDirection, length, width, damage, duration);
        }

        private void Configure(Vector3 origin, float facingDirection, float length, float width, int damage, float duration)
        {
            float direction = facingDirection >= 0f ? 1f : -1f;
            lifetime = Mathf.Max(0.08f, duration);
            transform.position = origin + new Vector3(direction * length * 0.5f, 0f, 0f);

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

            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadFlameSprite();
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
            float visualHeight = Mathf.Max(0.74f, hitWidth * 1.75f);
            layer.transform.localScale = new Vector3(
                length / spriteSize.x,
                visualHeight / spriteSize.y,
                1f);
            return renderer;
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
            float pulse = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.22f;
            transform.localScale = new Vector3(1f, pulse, 1f);

            Fade(flameRenderer, normalized, startAlpha);

            if (normalized >= 1f)
            {
                Destroy(gameObject);
            }
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
