using System.Collections.Generic;
using UnityEngine;

namespace PixelRaid
{
    public enum PixelRaidEnemyKind
    {
        Wisp,
        Ghoul,
        FrostKnight
    }

    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelRaidEnemyPatrol : MonoBehaviour
    {
        private static readonly Dictionary<PixelRaidEnemyKind, Sprite> EnemySprites = new Dictionary<PixelRaidEnemyKind, Sprite>();

        [SerializeField] private PixelRaidEnemyKind enemyKind = PixelRaidEnemyKind.Ghoul;
        [SerializeField] private Vector2 patrolOffset = new Vector2(1.5f, 0f);
        [SerializeField] private float speed = 2f;
        [SerializeField] private float chaseRange = 2.4f;
        [SerializeField] private float contactRange = 0.78f;
        [SerializeField] private float hitCooldown = 0.75f;
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private int contactDamage = 8;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private bool movingToEnd = true;
        private SpriteRenderer spriteRenderer;
        private PixelRaidPlayerController player;
        private int health;
        private float hitCooldownTimer;
        private float hurtFlashTimer;

        public bool CanBeHit => health > 0;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyKindStats();
            startPosition = transform.position;
            endPosition = startPosition + (Vector3)patrolOffset;
            health = maxHealth;

            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.offset = new Vector2(0f, 0.35f);
            boxCollider.size = new Vector2(0.8f, 0.8f);
        }

        private void Update()
        {
            if (health <= 0)
            {
                return;
            }

            player = player == null ? FindObjectOfType<PixelRaidPlayerController>() : player;
            hitCooldownTimer -= Time.deltaTime;
            hurtFlashTimer -= Time.deltaTime;
            spriteRenderer.color = hurtFlashTimer > 0f ? Color.white : GetKindColor();

            Vector3 target = GetMovementTarget();
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.02f)
            {
                movingToEnd = !movingToEnd;
            }

            float facingDelta = target.x - transform.position.x;
            if (Mathf.Abs(facingDelta) > 0.01f)
            {
                spriteRenderer.flipX = facingDelta < 0f;
            }

            TryContactDamage();
        }

        public void Configure(PixelRaidEnemyKind kind, Vector2 patrol)
        {
            enemyKind = kind;
            patrolOffset = patrol;
            ApplyKindStats();
            health = maxHealth;
            startPosition = transform.position;
            endPosition = startPosition + (Vector3)patrolOffset;
        }

        public void TakeHit(float attackerX)
        {
            if (!CanBeHit)
            {
                return;
            }

            health--;
            hurtFlashTimer = 0.12f;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.22f, 0f, 0f);

            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }

        private Vector3 GetMovementTarget()
        {
            if (player != null && player.IsAlive)
            {
                float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
                if (distance <= chaseRange)
                {
                    return new Vector3(player.transform.position.x, transform.position.y, transform.position.z);
                }
            }

            return movingToEnd ? endPosition : startPosition;
        }

        private void TryContactDamage()
        {
            if (player == null || hitCooldownTimer > 0f || !player.IsAlive)
            {
                return;
            }

            float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
            if (distance <= contactRange)
            {
                hitCooldownTimer = hitCooldown;
                player.TakeEnemyHit(contactDamage, transform.position.x);
            }
        }

        private void ApplyKindStats()
        {
            switch (enemyKind)
            {
                case PixelRaidEnemyKind.Wisp:
                    maxHealth = 1;
                    contactDamage = 6;
                    speed = 2.6f;
                    chaseRange = 2.9f;
                    transform.localScale = new Vector3(0.38f, 0.38f, 1f);
                    break;
                case PixelRaidEnemyKind.FrostKnight:
                    maxHealth = 4;
                    contactDamage = 12;
                    speed = 1.55f;
                    chaseRange = 2.15f;
                    transform.localScale = new Vector3(0.78f, 0.9f, 1f);
                    break;
                default:
                    maxHealth = 2;
                    contactDamage = 8;
                    speed = 2f;
                    chaseRange = 2.35f;
                    transform.localScale = new Vector3(0.58f, 0.72f, 1f);
                    break;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GetEnemySprite(enemyKind);
            spriteRenderer.color = GetKindColor();
        }

        private static Sprite GetEnemySprite(PixelRaidEnemyKind kind)
        {
            if (EnemySprites.TryGetValue(kind, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 outline = new Color32(7, 9, 13, 255);
            Color32 glow = new Color32(91, 198, 255, 255);
            Color32 body = kind == PixelRaidEnemyKind.FrostKnight ? new Color32(67, 77, 92, 255) : new Color32(41, 49, 44, 255);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            switch (kind)
            {
                case PixelRaidEnemyKind.Wisp:
                    Fill(texture, 9, 7, 15, 15, glow);
                    Fill(texture, 11, 10, 13, 12, outline);
                    Fill(texture, 5, 5, 7, 8, glow);
                    Fill(texture, 17, 6, 19, 9, glow);
                    Fill(texture, 8, 4, 15, 5, new Color32(172, 224, 255, 210));
                    break;
                case PixelRaidEnemyKind.FrostKnight:
                    Fill(texture, 7, 4, 16, 17, outline);
                    Fill(texture, 8, 5, 15, 16, body);
                    Fill(texture, 9, 13, 13, 20, outline);
                    Fill(texture, 12, 13, 17, 20, outline);
                    Fill(texture, 10, 15, 12, 20, body);
                    Fill(texture, 13, 15, 15, 20, body);
                    Fill(texture, 9, 14, 18, 15, glow);
                    Fill(texture, 5, 17, 18, 18, outline);
                    break;
                default:
                    Fill(texture, 6, 8, 17, 16, outline);
                    Fill(texture, 7, 9, 16, 15, body);
                    Fill(texture, 5, 6, 8, 10, outline);
                    Fill(texture, 15, 6, 18, 10, outline);
                    Fill(texture, 8, 14, 10, 20, outline);
                    Fill(texture, 14, 14, 16, 20, outline);
                    Fill(texture, 10, 12, 13, 13, glow);
                    break;
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.05f), 24f);
            sprite.name = $"PixelRaidEnemy_{kind}";
            EnemySprites[kind] = sprite;
            return sprite;
        }

        private static void Fill(Texture2D texture, int minX, int minY, int maxX, int maxY, Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private Color32 GetKindColor()
        {
            switch (enemyKind)
            {
                case PixelRaidEnemyKind.Wisp:
                    return new Color32(91, 198, 255, 230);
                case PixelRaidEnemyKind.FrostKnight:
                    return new Color32(66, 93, 126, 255);
                default:
                    return new Color32(119, 174, 91, 255);
            }
        }
    }
}
