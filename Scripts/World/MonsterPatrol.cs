using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public enum MonsterKind
    {
        Nekker,
        Drowner,
        Wraith
    }

    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class MonsterPatrol : MonoBehaviour
    {
        private static readonly Dictionary<MonsterKind, Sprite> EnemySprites = new Dictionary<MonsterKind, Sprite>();

        [SerializeField] private MonsterKind enemyKind = MonsterKind.Nekker;
        [SerializeField] private Vector2 patrolOffset = new Vector2(1.5f, 0f);
        [SerializeField] private float speed = 2f;
        [SerializeField] private float chaseRange = 2.4f;
        [SerializeField] private float contactRange = 1.08f;
        [SerializeField] private float hitCooldown = 0.75f;
        [SerializeField] private float deathDuration = 0.5f;
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private int contactDamage = 8;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private bool movingToEnd = true;
        private SpriteRenderer spriteRenderer;
        private GeraltController player;
        private int health;
        private float hitCooldownTimer;
        private float hurtFlashTimer;
        private bool dying;
        private float deathTimer;
        private Vector3 deathStartScale;

        public bool CanBeHit => health > 0 && !dying;
        public int ContactDamage => contactDamage;

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
            if (dying)
            {
                UpdateDeathAnimation();
                return;
            }

            if (health <= 0)
            {
                return;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            hitCooldownTimer -= Time.deltaTime;
            hurtFlashTimer -= Time.deltaTime;
            spriteRenderer.color = hurtFlashTimer > 0f ? Color.white : GetKindColor();
            spriteRenderer.sortingOrder = Mathf.RoundToInt((-transform.position.y) * 100f) + 15;

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

        public void Configure(MonsterKind kind, Vector2 patrol)
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
            WitcherCombatText.Spawn("-1", transform.position, new Color32(255, 225, 126, 255));
            hurtFlashTimer = 0.12f;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.22f, 0f, 0f);

            if (health <= 0)
            {
                StartDeathAnimation();
            }
        }

        private void StartDeathAnimation()
        {
            if (dying)
            {
                return;
            }

            dying = true;
            deathTimer = 0f;
            deathStartScale = transform.localScale;
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
            }

            if (player != null)
            {
                player.RestoreMana(enemyKind == MonsterKind.Wraith ? 16 : 10);
                if (enemyKind == MonsterKind.Drowner)
                {
                    player.RestoreHealth(6);
                }
            }

            WitcherCombatText.Spawn("击杀", transform.position + Vector3.up * 0.35f, new Color32(179, 228, 255, 255));
        }

        private void UpdateDeathAnimation()
        {
            deathTimer += Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / deathDuration);
            float sink = Mathf.Lerp(0f, -0.24f, t);
            transform.localScale = new Vector3(deathStartScale.x * Mathf.Lerp(1f, 1.18f, t), deathStartScale.y * Mathf.Lerp(1f, 0.18f, t), deathStartScale.z);
            transform.position += new Vector3(0f, sink * Time.deltaTime, 0f);
            Color color = GetKindColor();
            color.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = color;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private Vector3 GetMovementTarget()
        {
            if (player != null && player.IsAlive)
            {
                Vector2 toPlayer = player.transform.position - transform.position;
                if (Mathf.Abs(toPlayer.x) <= chaseRange && Mathf.Abs(toPlayer.y) <= chaseRange * 0.72f)
                {
                    return new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
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

            Vector2 distance = player.transform.position - transform.position;
            if (Mathf.Abs(distance.x) <= contactRange && Mathf.Abs(distance.y) <= contactRange * 0.62f)
            {
                hitCooldownTimer = hitCooldown;
                player.TakeEnemyHit(contactDamage, transform.position.x);
            }
        }

        private void ApplyKindStats()
        {
            switch (enemyKind)
            {
                case MonsterKind.Wraith:
                    maxHealth = 1;
                    contactDamage = 6;
                    speed = 2.75f;
                    chaseRange = 3.15f;
                    transform.localScale = new Vector3(0.52f, 0.68f, 1f);
                    break;
                case MonsterKind.Drowner:
                    maxHealth = 3;
                    contactDamage = 11;
                    speed = 1.72f;
                    chaseRange = 2.25f;
                    transform.localScale = new Vector3(0.68f, 0.78f, 1f);
                    break;
                default:
                    maxHealth = 2;
                    contactDamage = 8;
                    speed = 2.25f;
                    chaseRange = 2.45f;
                    transform.localScale = new Vector3(0.55f, 0.62f, 1f);
                    break;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GetEnemySprite(enemyKind);
            spriteRenderer.color = GetKindColor();
        }

        private static Sprite GetEnemySprite(MonsterKind kind)
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
            Color32 body = kind == MonsterKind.Drowner ? new Color32(36, 88, 92, 255) : new Color32(47, 61, 42, 255);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            switch (kind)
            {
                case MonsterKind.Wraith:
                    Fill(texture, 8, 8, 16, 17, new Color32(196, 224, 238, 210));
                    Fill(texture, 9, 7, 15, 9, outline);
                    Fill(texture, 10, 10, 11, 11, glow);
                    Fill(texture, 14, 10, 15, 11, glow);
                    Fill(texture, 6, 15, 8, 20, new Color32(196, 224, 238, 145));
                    Fill(texture, 12, 16, 14, 22, new Color32(196, 224, 238, 120));
                    Fill(texture, 17, 14, 19, 20, new Color32(196, 224, 238, 145));
                    break;
                case MonsterKind.Drowner:
                    Fill(texture, 6, 8, 17, 17, outline);
                    Fill(texture, 7, 9, 16, 16, body);
                    Fill(texture, 8, 5, 14, 9, outline);
                    Fill(texture, 9, 6, 13, 9, new Color32(58, 122, 124, 255));
                    Fill(texture, 7, 17, 10, 21, outline);
                    Fill(texture, 14, 17, 17, 21, outline);
                    Fill(texture, 4, 11, 7, 14, outline);
                    Fill(texture, 17, 11, 20, 14, outline);
                    Fill(texture, 10, 8, 11, 9, glow);
                    Fill(texture, 14, 8, 15, 9, glow);
                    break;
                default:
                    Fill(texture, 7, 10, 16, 17, outline);
                    Fill(texture, 8, 11, 15, 16, body);
                    Fill(texture, 6, 7, 11, 11, outline);
                    Fill(texture, 12, 7, 18, 11, outline);
                    Fill(texture, 8, 18, 10, 21, outline);
                    Fill(texture, 14, 18, 16, 21, outline);
                    Fill(texture, 4, 13, 7, 15, outline);
                    Fill(texture, 17, 13, 20, 15, outline);
                    Fill(texture, 10, 9, 11, 10, new Color32(235, 213, 91, 255));
                    Fill(texture, 14, 9, 15, 10, new Color32(235, 213, 91, 255));
                    break;
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.05f), 24f);
            sprite.name = $"WitcherMonster_{kind}";
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
                case MonsterKind.Wraith:
                    return new Color32(178, 218, 232, 220);
                case MonsterKind.Drowner:
                    return new Color32(50, 125, 132, 255);
                default:
                    return new Color32(113, 147, 72, 255);
            }
        }
    }
}
