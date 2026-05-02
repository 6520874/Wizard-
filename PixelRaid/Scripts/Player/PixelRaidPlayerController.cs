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

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private PixelRaidGeraltAnimator geraltAnimator;
        private bool controlsEnabled = true;
        private float hurtLockTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            geraltAnimator = GetComponent<PixelRaidGeraltAnimator>();
            transform.localScale = Vector3.one * visualScale;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            SnapToRoad();
        }

        private void Update()
        {
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

            float moveInput = Input.GetAxisRaw("Horizontal");
            body.velocity = new Vector2(moveInput * moveSpeed, 0f);
            SnapToRoad();

            if (moveInput != 0f)
            {
                spriteRenderer.flipX = moveInput < 0f;
            }

            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J))
            {
                geraltAnimator.PlaySlash();
                TryHitBoss();
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

        public void SetControlEnabled(bool isEnabled)
        {
            controlsEnabled = isEnabled;
        }

        public void TakeBossHit(float attackerX)
        {
            if (hurtLockTimer > 0f)
            {
                return;
            }

            hurtLockTimer = hurtLockDuration;
            float knockDirection = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(knockDirection * 0.18f, 0f, 0f);
            body.velocity = Vector2.zero;
            geraltAnimator.PlayHurt();
            SnapToRoad();
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
    }
}
