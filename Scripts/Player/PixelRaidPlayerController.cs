using UnityEngine;

namespace PixelRaid
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PixelRaidGeraltAnimator))]
    public class PixelRaidPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float roadY = -1.65f;
        [SerializeField] private float minRoadX = -7.6f;
        [SerializeField] private float maxRoadX = 7.6f;
        [SerializeField] private float visualScale = 0.65f;
        [SerializeField] private float attackRange = 1.45f;
        [SerializeField] private float hurtLockDuration = 0.35f;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int maxMana = 100;
        [SerializeField] private int slashManaCost = 12;
        [SerializeField] private float manaRegenPerSecond = 9f;
        [SerializeField] private int bossHitDamage = 16;
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float dashCooldown = 0.55f;
        [SerializeField] private int dashManaCost = 18;
        [SerializeField] private int healManaCost = 35;
        [SerializeField] private int healAmount = 22;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private PixelRaidGeraltAnimator geraltAnimator;
        private bool controlsEnabled = true;
        private float hurtLockTimer;
        private int currentHealth;
        private float currentMana;
        private float dashTimer;
        private float dashCooldownTimer;
        private float invulnerableTimer;
        private float lastFacingDirection = 1f;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int CurrentMana => Mathf.RoundToInt(currentMana);
        public int MaxMana => maxMana;
        public bool IsAlive => currentHealth > 0;
        public System.Action StatsChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            geraltAnimator = GetComponent<PixelRaidGeraltAnimator>();
            currentHealth = maxHealth;
            currentMana = maxMana;
            transform.localScale = Vector3.one * visualScale;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            SnapToRoad();
        }

        private void Start()
        {
            PixelRaidPlayerHud.CreateIfMissing(this);
            PixelRaidWorldDirector.CreateIfMissing(this);
        }

        private void Update()
        {
            RegenerateMana();
            dashCooldownTimer -= Time.deltaTime;
            invulnerableTimer -= Time.deltaTime;

            if (!controlsEnabled)
            {
                body.velocity = Vector2.zero;
                geraltAnimator.ForceIdle();
                SnapToRoad();
                return;
            }

            if (hurtLockTimer > 0f)
            {
                hurtLockTimer -= Time.deltaTime;
                body.velocity = Vector2.zero;
                SnapToRoad();
                return;
            }

            if (dashTimer > 0f)
            {
                dashTimer -= Time.deltaTime;
                body.velocity = new Vector2(lastFacingDirection * dashSpeed, 0f);
                geraltAnimator.PlayLocomotion(true);
                SnapToRoad();
                return;
            }

            float moveInput = Input.GetAxisRaw("Horizontal");
            body.velocity = new Vector2(moveInput * moveSpeed, 0f);
            SnapToRoad();

            if (moveInput != 0f)
            {
                lastFacingDirection = Mathf.Sign(moveInput);
                spriteRenderer.flipX = moveInput < 0f;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                TryDash(moveInput);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                TryHeal();
            }
            else if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J))
            {
                TrySlash();
            }
            else
            {
                geraltAnimator.PlayLocomotion(Mathf.Abs(moveInput) > 0.01f);
            }
        }

        private void LateUpdate()
        {
            SnapToRoad();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PixelRaidEnemyPatrol enemy = other.GetComponent<PixelRaidEnemyPatrol>();
            if (enemy != null && enemy.CanBeHit)
            {
                TakeEnemyHit(enemy.ContactDamage, enemy.transform.position.x);
            }
        }

        public void SetControlEnabled(bool isEnabled)
        {
            controlsEnabled = isEnabled;
        }

        public void TakeBossHit(float attackerX)
        {
            TakeDamage(bossHitDamage, attackerX);
        }

        public void TakeEnemyHit(int damage, float attackerX)
        {
            TakeDamage(damage, attackerX);
        }

        public void RestoreHealth(int amount)
        {
            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            int restoredAmount = currentHealth - previousHealth;
            if (restoredAmount > 0)
            {
                PixelRaidCombatText.Spawn($"+{restoredAmount}", transform.position, new Color32(97, 231, 151, 255));
                StatsChanged?.Invoke();
            }
        }

        public void RestoreMana(int amount)
        {
            float previousMana = currentMana;
            currentMana = Mathf.Clamp(currentMana + amount, 0f, maxMana);
            int restoredAmount = Mathf.RoundToInt(currentMana - previousMana);
            if (restoredAmount > 0)
            {
                PixelRaidCombatText.Spawn($"+{restoredAmount} MP", transform.position, new Color32(89, 181, 255, 255));
                StatsChanged?.Invoke();
            }
        }

        public void ConfigureRoad(float roadYValue, float minX, float maxX)
        {
            roadY = roadYValue;
            minRoadX = minX;
            maxRoadX = maxX;
            SnapToRoad();
        }

        public void WarpTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            SnapToRoad();
        }

        private void TrySlash()
        {
            if (currentMana < slashManaCost)
            {
                geraltAnimator.PlayLocomotion(false);
                return;
            }

            currentMana = Mathf.Max(0f, currentMana - slashManaCost);
            StatsChanged?.Invoke();
            geraltAnimator.PlaySlash();
            TryHitBoss();
            TryHitCommonEnemies();
        }

        private void TryDash(float moveInput)
        {
            if (dashCooldownTimer > 0f || currentMana < dashManaCost)
            {
                return;
            }

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                lastFacingDirection = Mathf.Sign(moveInput);
            }

            currentMana = Mathf.Max(0f, currentMana - dashManaCost);
            StatsChanged?.Invoke();
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            invulnerableTimer = dashDuration + 0.08f;
            hurtLockTimer = 0f;
        }

        private void TryHeal()
        {
            if (currentHealth >= maxHealth || currentMana < healManaCost)
            {
                return;
            }

            currentMana = Mathf.Max(0f, currentMana - healManaCost);
            StatsChanged?.Invoke();
            RestoreHealth(healAmount);
            body.velocity = Vector2.zero;
            geraltAnimator.ForceIdle();
        }

        private void TakeDamage(int damage, float attackerX)
        {
            if (hurtLockTimer > 0f || invulnerableTimer > 0f || !IsAlive)
            {
                return;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(0, damage), 0, maxHealth);
            PixelRaidCombatText.Spawn($"-{damage}", transform.position, new Color32(255, 72, 82, 255));
            if (currentHealth != previousHealth)
            {
                StatsChanged?.Invoke();
            }
            hurtLockTimer = hurtLockDuration;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.18f, 0f, 0f);
            body.velocity = Vector2.zero;
            geraltAnimator.PlayHurt();
            controlsEnabled = currentHealth > 0;
            SnapToRoad();
        }

        private void RegenerateMana()
        {
            if (currentMana >= maxMana)
            {
                return;
            }

            currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerSecond * Time.deltaTime);
        }

        private void SnapToRoad()
        {
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, minRoadX, maxRoadX);
            position.y = roadY;
            transform.position = position;
        }

        private void TryHitBoss()
        {
            PixelRaidWildHuntBossController boss = FindObjectOfType<PixelRaidWildHuntBossController>();
            if (boss == null || !boss.CanBeHit)
            {
                return;
            }

            float horizontalDistance = Mathf.Abs(boss.transform.position.x - transform.position.x);
            if (horizontalDistance <= attackRange)
            {
                boss.TakeHit(transform.position.x);
            }
        }

        private void TryHitCommonEnemies()
        {
            PixelRaidEnemyPatrol[] enemies = FindObjectsOfType<PixelRaidEnemyPatrol>();
            for (int i = 0; i < enemies.Length; i++)
            {
                PixelRaidEnemyPatrol enemy = enemies[i];
                if (enemy == null || !enemy.CanBeHit)
                {
                    continue;
                }

                float horizontalDistance = Mathf.Abs(enemy.transform.position.x - transform.position.x);
                if (horizontalDistance <= attackRange)
                {
                    enemy.TakeHit(transform.position.x);
                }
            }
        }
    }
}
