using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(GeraltAnimator))]
    public class GeraltController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float verticalMoveSpeed = 3.25f;
        [SerializeField] private float minStageX = -8.2f;
        [SerializeField] private float maxStageX = 236.8f;
        [SerializeField] private float minStageY = -2.55f;
        [SerializeField] private float maxStageY = 5f;
        [SerializeField] private float visualScale = 0.65f;
        [SerializeField] private float attackRange = 1.45f;
        [SerializeField] private float laneAttackTolerance = 0.72f;
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
        private GeraltAnimator geraltAnimator;
        private bool controlsEnabled = true;
        private float hurtLockTimer;
        private int currentHealth;
        private float currentMana;
        private float dashTimer;
        private float dashCooldownTimer;
        private float invulnerableTimer;
        private float lastFacingDirection = 1f;
        private bool defeatHandled;

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
            geraltAnimator = GetComponent<GeraltAnimator>();
            currentHealth = maxHealth;
            currentMana = maxMana;
            transform.localScale = Vector3.one * visualScale;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            ClampToStage();
        }

        private void Start()
        {
            WitcherHud.CreateIfMissing(this);
            WitcherWorldDirector.CreateIfMissing(this);
        }

        private void Update()
        {
            RegenerateMana();
            dashCooldownTimer -= Time.deltaTime;
            invulnerableTimer -= Time.deltaTime;

            if (!IsAlive)
            {
                body.velocity = Vector2.zero;
                ClampToStage();
                return;
            }

            if (!controlsEnabled)
            {
                body.velocity = Vector2.zero;
                geraltAnimator.ForceIdle();
                ClampToStage();
                return;
            }

            if (hurtLockTimer > 0f)
            {
                hurtLockTimer -= Time.deltaTime;
                body.velocity = Vector2.zero;
                ClampToStage();
                return;
            }

            if (dashTimer > 0f)
            {
                dashTimer -= Time.deltaTime;
                body.velocity = new Vector2(lastFacingDirection * dashSpeed, 0f);
                geraltAnimator.PlayLocomotion(true);
                ClampToStage();
                return;
            }

            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 movement = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            body.velocity = new Vector2(movement.x * moveSpeed, movement.y * verticalMoveSpeed);
            ClampToStage();

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                lastFacingDirection = Mathf.Sign(moveInput.x);
                spriteRenderer.flipX = moveInput.x < 0f;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                TryDash(moveInput.x);
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
                geraltAnimator.PlayLocomotion(movement.sqrMagnitude > 0.01f);
            }

            UpdateDepthSorting();
        }

        private void LateUpdate()
        {
            ClampToStage();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            MonsterPatrol enemy = other.GetComponent<MonsterPatrol>();
            if (enemy != null && enemy.CanBeHit && Mathf.Abs(enemy.transform.position.y - transform.position.y) <= laneAttackTolerance)
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
                WitcherCombatText.Spawn($"+{restoredAmount}", transform.position, new Color32(97, 231, 151, 255));
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
                WitcherCombatText.Spawn($"+{restoredAmount} MP", transform.position, new Color32(89, 181, 255, 255));
                StatsChanged?.Invoke();
            }
        }

        public void ConfigureStage(float minX, float maxX, float minY, float maxY)
        {
            minStageX = minX;
            maxStageX = maxX;
            minStageY = minY;
            maxStageY = maxY;
            ClampToStage();
        }

        public void WarpTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            body.velocity = Vector2.zero;
            ClampToStage();
        }

        private void TrySlash()
        {
            if (geraltAnimator.IsSlashPlaying || geraltAnimator.IsHurtPlaying || geraltAnimator.IsDeathPlaying)
            {
                return;
            }

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
            WitcherCombatText.Spawn($"-{damage}", transform.position, new Color32(255, 72, 82, 255));
            if (currentHealth != previousHealth)
            {
                StatsChanged?.Invoke();
            }
            hurtLockTimer = hurtLockDuration;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.18f, 0f, 0f);
            body.velocity = Vector2.zero;
            if (currentHealth <= 0)
            {
                HandleDefeat();
            }
            else
            {
                geraltAnimator.PlayHurt();
            }

            controlsEnabled = currentHealth > 0;
            ClampToStage();
        }

        private void HandleDefeat()
        {
            if (defeatHandled)
            {
                return;
            }

            defeatHandled = true;
            controlsEnabled = false;
            geraltAnimator.PlayDeath();
            WitcherHud hud = FindObjectOfType<WitcherHud>();
            if (hud != null)
            {
                hud.ShowDefeatScreen();
            }
        }

        private void RegenerateMana()
        {
            if (currentMana >= maxMana)
            {
                return;
            }

            currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerSecond * Time.deltaTime);
        }

        private void ClampToStage()
        {
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, minStageX, maxStageX);
            position.y = Mathf.Clamp(position.y, minStageY, maxStageY);

            transform.position = position;
        }

        private void UpdateDepthSorting()
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt((maxStageY - transform.position.y) * 100f) + 20;
        }

        private void TryHitBoss()
        {
            WildHuntBossController boss = FindObjectOfType<WildHuntBossController>();
            if (boss == null || !boss.CanBeHit)
            {
                return;
            }

            float horizontalDistance = Mathf.Abs(boss.transform.position.x - transform.position.x);
            float verticalDistance = Mathf.Abs(boss.transform.position.y - transform.position.y);
            if (horizontalDistance <= attackRange && verticalDistance <= laneAttackTolerance)
            {
                boss.TakeHit(transform.position.x);
            }
        }

        private void TryHitCommonEnemies()
        {
            MonsterPatrol[] enemies = FindObjectsOfType<MonsterPatrol>();
            for (int i = 0; i < enemies.Length; i++)
            {
                MonsterPatrol enemy = enemies[i];
                if (enemy == null || !enemy.CanBeHit)
                {
                    continue;
                }

                float horizontalDistance = Mathf.Abs(enemy.transform.position.x - transform.position.x);
                float verticalDistance = Mathf.Abs(enemy.transform.position.y - transform.position.y);
                if (horizontalDistance <= attackRange && verticalDistance <= laneAttackTolerance)
                {
                    enemy.TakeHit(transform.position.x);
                }
            }
        }
    }
}
