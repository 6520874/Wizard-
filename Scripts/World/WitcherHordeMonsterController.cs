using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WitcherGame
{
    public enum WitcherHordeMonsterKind
    {
        CorruptedWolf,
        BloodWraith
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class WitcherHordeMonsterController : MonoBehaviour
    {
        private const float PixelsPerUnit = 96f;
        private static readonly Dictionary<string, Sprite[]> CachedFrames = new Dictionary<string, Sprite[]>();

        [SerializeField] private WitcherHordeMonsterKind monsterKind = WitcherHordeMonsterKind.CorruptedWolf;
        [SerializeField] private float moveSpeed = 2.7f;
        [SerializeField] private float stopDistance = 0.82f;
        [SerializeField] private float attackRange = 1.05f;
        [SerializeField] private float laneAttackTolerance = 0.58f;
        [SerializeField] private float attackCooldown = 1.18f;
        [SerializeField] private float hitStunDuration = 0.22f;
        [SerializeField] private float deathDuration = 0.55f;
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private int contactDamage = 4;
        [SerializeField] private float visualScale = 0.72f;
        [SerializeField] private float minStageY = -2.55f;
        [SerializeField] private float maxStageY = 5f;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private GeraltController player;
        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Sprite[] attackFrames;
        private Sprite[] hurtFrames;
        private Sprite[] deathFrames;
        private Sprite[] currentFrames;
        private int frameIndex;
        private float frameTimer;
        private float framesPerSecond = 8f;
        private float attackCooldownTimer;
        private float hitStunTimer;
        private float hurtFlashTimer;
        private float deathTimer;
        private bool dying;
        private bool attacking;
        private bool attackDamageApplied;
        private int health;
        private Vector3 deathStartScale;

        public bool CanBeHit => health > 0 && !dying;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            ApplyKindTuning();
            LoadFrames();
            health = maxHealth;
            transform.localScale = Vector3.one * visualScale;
            Play(idleFrames, 7f, true);
        }

        private void Start()
        {
            player = FindObjectOfType<GeraltController>();
        }

        private void Update()
        {
            ClampToStage();
            UpdateAnimation();

            if (dying)
            {
                UpdateDeath();
                return;
            }

            if (health <= 0)
            {
                return;
            }

            hurtFlashTimer -= Time.deltaTime;
            spriteRenderer.color = hurtFlashTimer > 0f ? new Color32(255, 244, 222, 255) : Color.white;
            spriteRenderer.sortingOrder = Mathf.RoundToInt((maxStageY - transform.position.y) * 100f) + 17;

            if (hitStunTimer > 0f)
            {
                hitStunTimer -= Time.deltaTime;
                return;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null || !player.IsAlive)
            {
                Play(idleFrames, 7f);
                return;
            }

            attackCooldownTimer -= Time.deltaTime;
            float dx = player.transform.position.x - transform.position.x;
            float horizontalDistance = Mathf.Abs(dx);
            float verticalDistance = Mathf.Abs(player.transform.position.y - transform.position.y);
            spriteRenderer.flipX = dx < 0f;

            if (attacking)
            {
                TryApplyAttackDamage(horizontalDistance, verticalDistance);
                return;
            }

            if (horizontalDistance <= attackRange && verticalDistance <= laneAttackTolerance && attackCooldownTimer <= 0f)
            {
                StartAttack();
                return;
            }

            if (horizontalDistance > stopDistance || verticalDistance > laneAttackTolerance * 0.75f)
            {
                Vector3 target = new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                Play(runFrames, monsterKind == WitcherHordeMonsterKind.CorruptedWolf ? 11f : 8.5f);
            }
            else
            {
                Play(idleFrames, 7f);
            }
        }

        public void Configure(WitcherHordeMonsterKind kind, int waveHealthBonus, float speedBonus)
        {
            monsterKind = kind;
            ApplyKindTuning();
            maxHealth += Mathf.Max(0, waveHealthBonus);
            health = maxHealth;
            moveSpeed += Mathf.Max(0f, speedBonus);
            transform.localScale = Vector3.one * visualScale;
            LoadFrames();
            Play(idleFrames, 7f, true);
        }

        public void TakeHit(float attackerX)
        {
            TakeDamage(1, attackerX, false);
        }

        public void TakeMagicHit(int damage, float attackerX)
        {
            TakeDamage(Mathf.Max(1, damage), attackerX, true);
        }

        private void TakeDamage(int damage, float attackerX, bool magicHit)
        {
            if (!CanBeHit)
            {
                return;
            }

            health -= damage;
            WitcherCombatText.Spawn($"-{damage}", transform.position + Vector3.up * 0.45f, magicHit ? new Color32(255, 151, 65, 255) : new Color32(255, 225, 126, 255));
            WitcherCombatFeedback.EnemyHit(transform.position, magicHit ? 0.09f : 0.065f, magicHit ? 0.035f : 0.028f);

            if (health <= 0)
            {
                StartDeath();
                return;
            }

            attacking = false;
            hitStunTimer = hitStunDuration;
            hurtFlashTimer = 0.16f;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * (magicHit ? 0.35f : 0.28f), 0f, 0f);
            Play(hurtFrames, 10f, true);
            ClampToStage();
        }

        private void StartAttack()
        {
            attacking = true;
            attackDamageApplied = false;
            attackCooldownTimer = attackCooldown;
            Play(attackFrames, monsterKind == WitcherHordeMonsterKind.CorruptedWolf ? 12f : 9f, true);
        }

        private void TryApplyAttackDamage(float horizontalDistance, float verticalDistance)
        {
            if (!attackDamageApplied && frameIndex >= Mathf.Max(1, currentFrames.Length / 2))
            {
                attackDamageApplied = true;
                if (player != null && horizontalDistance <= attackRange + 0.2f && verticalDistance <= laneAttackTolerance)
                {
                    player.TakeEnemyHit(contactDamage, transform.position.x);
                }
            }

            if (frameIndex >= currentFrames.Length - 1)
            {
                attacking = false;
            }
        }

        private void StartDeath()
        {
            if (dying)
            {
                return;
            }

            dying = true;
            attacking = false;
            deathTimer = 0f;
            deathStartScale = transform.localScale;
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            Play(deathFrames, 9f, true);
            WitcherCombatText.Spawn(monsterKind == WitcherHordeMonsterKind.BloodWraith ? "怨灵消散" : "腐狼倒下", transform.position + Vector3.up * 0.55f, new Color32(180, 228, 255, 255));
        }

        private void UpdateDeath()
        {
            deathTimer += Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / deathDuration);
            transform.localScale = new Vector3(deathStartScale.x * Mathf.Lerp(1f, 1.1f, t), deathStartScale.y * Mathf.Lerp(1f, 0.28f, t), deathStartScale.z);
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = color;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyKindTuning()
        {
            if (monsterKind == WitcherHordeMonsterKind.BloodWraith)
            {
                moveSpeed = 2.05f;
                stopDistance = 1.15f;
                attackRange = 1.45f;
                laneAttackTolerance = 0.72f;
                attackCooldown = 1.45f;
                maxHealth = 4;
                contactDamage = 5;
                visualScale = 0.92f;
                return;
            }

            moveSpeed = 3.15f;
            stopDistance = 0.72f;
            attackRange = 1.02f;
            laneAttackTolerance = 0.56f;
            attackCooldown = 1.05f;
            maxHealth = 2;
            contactDamage = 4;
            visualScale = 0.78f;
        }

        private void LoadFrames()
        {
            if (monsterKind == WitcherHordeMonsterKind.BloodWraith)
            {
                idleFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 0);
                runFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 1);
                attackFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 1);
                hurtFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 1);
                deathFrames = LoadFrameRow("Art/Monsters/BloodWraithSheet.png", 7, 7, 6);
                return;
            }

            idleFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 0);
            runFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 1);
            attackFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 1);
            hurtFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 1);
            deathFrames = LoadFrameRow("Art/Monsters/CorruptedWolfSheet.png", 6, 7, 5);
        }

        private static Sprite[] LoadFrameRow(string relativePath, int columns, int rows, int row)
        {
            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"Monster sheet missing: {absolutePath}");
                Sprite fallback = WitcherSpriteLibrary.GetSolidSprite(new Color32(34, 38, 42, 255));
                string missingKey = $"{relativePath}_{columns}_{rows}_{row}_missing";
                CachedFrames[missingKey] = new[] { fallback };
                return CachedFrames[missingKey];
            }

            string key = $"{relativePath}_{columns}_{rows}_{row}_{File.GetLastWriteTimeUtc(absolutePath).Ticks}";
            if (CachedFrames.TryGetValue(key, out Sprite[] cached))
            {
                return cached;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Sprite fallback = WitcherSpriteLibrary.GetSolidSprite(new Color32(34, 38, 42, 255));
                CachedFrames[key] = new[] { fallback };
                return CachedFrames[key];
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            int frameWidth = texture.width / columns;
            int frameHeight = texture.height / rows;
            Sprite[] frames = new Sprite[columns];
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new Rect(column * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                Sprite frame = Sprite.Create(texture, rect, new Vector2(0.5f, 0.08f), PixelsPerUnit);
                frame.name = $"{Path.GetFileNameWithoutExtension(relativePath)}_{row}_{column}";
                frames[column] = frame;
            }

            CachedFrames[key] = frames;
            return frames;
        }

        private void Play(Sprite[] frames, float fps, bool restart = false)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (!restart && currentFrames == frames)
            {
                return;
            }

            currentFrames = frames;
            framesPerSecond = fps;
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = currentFrames[0];
        }

        private void UpdateAnimation()
        {
            if (currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                if (dying || attacking || currentFrames == hurtFrames)
                {
                    frameIndex = Mathf.Min(frameIndex + 1, currentFrames.Length - 1);
                }
                else
                {
                    frameIndex = (frameIndex + 1) % currentFrames.Length;
                }

                spriteRenderer.sprite = currentFrames[frameIndex];
            }

            if (!dying && !attacking && currentFrames == hurtFrames && frameIndex >= currentFrames.Length - 1)
            {
                Play(idleFrames, 7f, true);
            }
        }

        private void ClampToStage()
        {
            Vector3 position = transform.position;
            position.y = Mathf.Clamp(position.y, minStageY, maxStageY);
            transform.position = position;
        }
    }
}
