using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(WildHuntBossAnimator))]
    public class WildHuntBossController : MonoBehaviour
    {
        [SerializeField] private float roadY = -1.88f;
        [SerializeField] private float moveSpeed = 1.55f;
        [SerializeField] private float stopDistance = 1.35f;
        [SerializeField] private float attackRange = 1.7f;
        [SerializeField] private float attackCooldown = 1.45f;
        [SerializeField] private float hitStunDuration = 0.32f;
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

        public bool CanBeHit => spawned && hitStunTimer <= 0f && health > 0;
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
            body.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.offset = new Vector2(0f, 0.9f);
            boxCollider.size = new Vector2(1.7f, 1.75f);

            transform.localScale = Vector3.one * visualScale;
            SnapToRoad();
        }

        private void Start()
        {
            player = FindObjectOfType<GeraltController>();
            bossAnimator.PlaySpawn();
        }

        private void Update()
        {
            SnapToRoad();

            if (!spawned)
            {
                spawned = !bossAnimator.IsOneShotPlaying;
                return;
            }

            if (health <= 0)
            {
                bossAnimator.PlayLocomotion(false);
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
            float distance = Mathf.Abs(dx);
            spriteRenderer.flipX = dx < 0f;

            if (bossAnimator.CurrentAnimation == WildHuntAnimation.Attack && bossAnimator.IsOneShotPlaying)
            {
                TryApplyAttackDamage(distance);
                return;
            }

            if (distance <= attackRange && attackCooldownTimer <= 0f)
            {
                attackCooldownTimer = attackCooldown;
                attackDamageApplied = false;
                bossAnimator.PlayAttack();
                return;
            }

            if (distance > stopDistance)
            {
                float direction = Mathf.Sign(dx);
                transform.position += new Vector3(direction * moveSpeed * Time.deltaTime, 0f, 0f);
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
            hitStunTimer = hitStunDuration;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.18f, 0f, 0f);
            spriteRenderer.flipX = attackerX < transform.position.x;
            bossAnimator.PlayHurt();
            SnapToRoad();
        }

        private void TryApplyAttackDamage(float currentDistance)
        {
            if (attackDamageApplied || bossAnimator.NormalizedFrame < 0.55f)
            {
                return;
            }

            attackDamageApplied = true;
            if (player != null && currentDistance <= attackRange + 0.25f)
            {
                player.TakeBossHit(transform.position.x);
            }
        }

        private void SnapToRoad()
        {
            Vector3 position = transform.position;
            position.y = roadY;
            transform.position = position;
        }
    }
}
