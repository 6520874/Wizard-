using UnityEngine;

namespace WitcherGame
{
    public class WitcherFlameLine : MonoBehaviour
    {
        private const int SortingBoost = 38;

        private SpriteRenderer glowRenderer;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer coreRenderer;
        private float lifetime = 0.32f;
        private float timer;
        private float glowAlpha;
        private float bodyAlpha;
        private float coreAlpha;

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

            Sprite solidSprite = WitcherSpriteLibrary.GetSolidSprite(Color.white);
            glowRenderer = CreateLayer("Glow", solidSprite, new Color32(255, 68, 12, 82), new Vector3(length, width * 1.35f, 1f), -0.02f);
            bodyRenderer = CreateLayer("Flame Body", solidSprite, new Color32(255, 92, 17, 198), new Vector3(length, width, 1f), 0f);
            coreRenderer = CreateLayer("White Hot Core", solidSprite, new Color32(255, 232, 113, 232), new Vector3(length * 0.86f, width * 0.34f, 1f), 0.02f);
            glowAlpha = glowRenderer.color.a;
            bodyAlpha = bodyRenderer.color.a;
            coreAlpha = coreRenderer.color.a;

            int sortingOrder = Mathf.RoundToInt((5f - origin.y) * 100f) + SortingBoost;
            glowRenderer.sortingOrder = sortingOrder;
            bodyRenderer.sortingOrder = sortingOrder + 1;
            coreRenderer.sortingOrder = sortingOrder + 2;

            ApplyDamage(origin, direction, length, width, damage);
        }

        private SpriteRenderer CreateLayer(string layerName, Sprite sprite, Color32 color, Vector3 scale, float zOffset)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(transform, false);
            layer.transform.localPosition = new Vector3(0f, 0f, zOffset);
            layer.transform.localScale = scale;

            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
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

            Fade(glowRenderer, normalized, glowAlpha);
            Fade(bodyRenderer, normalized, bodyAlpha);
            Fade(coreRenderer, normalized, coreAlpha);

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
