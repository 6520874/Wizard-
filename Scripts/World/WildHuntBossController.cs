using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(WildHuntBossAnimator))]
    public class WildHuntBossController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.55f;
        [SerializeField] private float stopDistance = 1.35f;
        [SerializeField] private float attackRange = 1.7f;
        [SerializeField] private float laneAttackTolerance = 0.78f;
        [SerializeField] private float minStageY = -2.55f;
        [SerializeField] private float maxStageY = 5f;
        [SerializeField] private float attackCooldown = 1.45f;
        [SerializeField] private float hitStunDuration = 0.32f;
        [SerializeField] private float deathDuration = 1.1f;
        [SerializeField] private int maxHealth = 8;
        [SerializeField] private float visualScale = 0.82f;

        private WildHuntBossAnimator bossAnimator;
        private SpriteRenderer spriteRenderer;
        private GeraltController player;
        private int health;
        private float attackCooldownTimer;
        private float hitStunTimer;
        private bool attackDamageApplied;
        private bool spawned;
        private bool dying;
        private float deathTimer;
        private bool equipmentDropped;

        public bool CanBeHit => spawned && hitStunTimer <= 0f && health > 0 && !dying;
        public bool HasSpawned => spawned;
        public int CurrentHealth => health;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            bossAnimator = GetComponent<WildHuntBossAnimator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = maxHealth;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.offset = new Vector2(0f, 0.9f);
            boxCollider.size = new Vector2(1.7f, 1.75f);

            transform.localScale = Vector3.one * visualScale;
            ClampToStage();
        }

        private void Start()
        {
            player = FindObjectOfType<GeraltController>();
            bossAnimator.PlaySpawn();
        }

        private void Update()
        {
            ClampToStage();

            if (dying)
            {
                UpdateDeathAnimation();
                return;
            }

            if (!spawned)
            {
                spawned = !bossAnimator.IsOneShotPlaying;
                return;
            }

            if (health <= 0)
            {
                return;
            }

            if (hitStunTimer > 0f)
            {
                hitStunTimer -= Time.deltaTime;
                return;
            }

            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
                bossAnimator.PlayLocomotion(false);
                return;
            }

            attackCooldownTimer -= Time.deltaTime;
            float dx = player.transform.position.x - transform.position.x;
            float horizontalDistance = Mathf.Abs(dx);
            float verticalDistance = Mathf.Abs(player.transform.position.y - transform.position.y);
            spriteRenderer.flipX = dx < 0f;
            spriteRenderer.sortingOrder = Mathf.RoundToInt((maxStageY - transform.position.y) * 100f) + 18;

            if (bossAnimator.CurrentAnimation == WildHuntAnimation.Attack && bossAnimator.IsOneShotPlaying)
            {
                TryApplyAttackDamage(horizontalDistance);
                return;
            }

            if (horizontalDistance <= attackRange && verticalDistance <= laneAttackTolerance && attackCooldownTimer <= 0f)
            {
                attackCooldownTimer = attackCooldown;
                attackDamageApplied = false;
                bossAnimator.PlayAttack();
                return;
            }

            if (horizontalDistance > stopDistance || verticalDistance > laneAttackTolerance * 0.75f)
            {
                Vector3 target = new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                ClampToStage();
                bossAnimator.PlayLocomotion(true);
            }
            else
            {
                bossAnimator.PlayLocomotion(false);
            }
        }

        public void TakeHit(float attackerX)
        {
            if (!CanBeHit)
            {
                return;
            }

            health--;
            WitcherCombatText.Spawn("-1", transform.position, new Color32(149, 221, 255, 255));
            if (health <= 0)
            {
                StartDeathAnimation();
                return;
            }

            hitStunTimer = hitStunDuration;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.18f, 0f, 0f);
            spriteRenderer.flipX = attackerX < transform.position.x;
            bossAnimator.PlayHurt();
            ClampToStage();
        }

        private void StartDeathAnimation()
        {
            if (dying)
            {
                return;
            }

            dying = true;
            deathTimer = 0f;
            attackDamageApplied = true;
            hitStunTimer = 0f;
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            bossAnimator.PlayDeath();
            WitcherCombatText.Spawn("狂猎倒下", transform.position + Vector3.up * 1.2f, new Color32(118, 219, 255, 255));
        }

        private void UpdateDeathAnimation()
        {
            deathTimer += Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / deathDuration);
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = color;
            transform.localScale = Vector3.one * visualScale * Mathf.Lerp(1f, 0.9f, t);

            if (!equipmentDropped && t >= 0.62f)
            {
                equipmentDropped = true;
                WitcherEquipmentDrop.SpawnAt(transform.position + new Vector3(0.55f, 0.15f, 0f));
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void TryApplyAttackDamage(float currentDistance)
        {
            if (attackDamageApplied || bossAnimator.NormalizedFrame < 0.55f)
            {
                return;
            }

            attackDamageApplied = true;
            float verticalDistance = player == null ? float.MaxValue : Mathf.Abs(player.transform.position.y - transform.position.y);
            if (player != null && currentDistance <= attackRange + 0.25f && verticalDistance <= laneAttackTolerance)
            {
                player.TakeBossHit(transform.position.x);
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
