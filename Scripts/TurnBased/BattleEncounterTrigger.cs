using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(Collider2D))]
    // 中文说明：把地图上的怪物接触事件转换成一次回合制战斗遭遇。
    public class BattleEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private string encounterTitle = "怪物";
        [SerializeField] private string enemyName = "腐化野兽";
        [SerializeField] private int enemyCount = 1;
        [SerializeField] private int enemyHealth = 28;
        [SerializeField] private int enemyAttack = 8;
        [SerializeField] private int enemyDefense = 2;
        [SerializeField] private int experienceReward = 2;
        [SerializeField] private int goldReward = 8;
        [SerializeField] private string lootName = "怪物残骸";
        [SerializeField, Range(0f, 1f)] private float lootChance = 0.35f;
        [SerializeField] private bool destroyOnWin = true;

        private Sprite battleSprite;
        private TurnBasedEnemyVisualKind visualKind = TurnBasedEnemyVisualKind.CorruptedWolf;
        private int investigationHealthPenalty;
        private int investigationAttackPenalty;
        private int investigationDefensePenalty;
        private int investigationShieldAdjustment;
        private string[] investigationWeaknessLabels;
        private bool[] investigationWeaknessDiscovery;
        private string battleOpeningNote;
        private bool consumed;
        private Collider2D encounterCollider;
        private GeraltController player;
        private float triggerHalfWidth = 1.25f;
        private float triggerHalfHeight = 1.1f;

        public string EncounterTitle => encounterTitle;
        public string BattleOpeningNote => battleOpeningNote;

        private void Awake()
        {
            encounterCollider = GetComponent<Collider2D>();
            encounterCollider.isTrigger = true;
            EnsureKinematicBody();
        }

        public void ConfigureMonster(TurnBasedEnemyVisualKind kind, Sprite sprite, int count, int waveBonus)
        {
            battleSprite = sprite;
            visualKind = kind == TurnBasedEnemyVisualKind.BloodWraith ? TurnBasedEnemyVisualKind.BloodWraith : TurnBasedEnemyVisualKind.CorruptedWolf;
            EnsureMapIdleAnimator();
            enemyCount = Mathf.Clamp(count, 1, 3);
            int bonus = Mathf.Max(0, waveBonus);
            if (kind == TurnBasedEnemyVisualKind.BloodWraith)
            {
                encounterTitle = enemyCount > 1 ? "吸血女妖群" : "吸血女妖";
                enemyName = "吸血女妖";
                enemyHealth = 34 + bonus * 5;
                enemyAttack = 10 + bonus;
                enemyDefense = 3;
                experienceReward = 4;
                goldReward = 13 + bonus * 3;
                lootName = "女妖残纱";
                lootChance = 0.55f;
                ConfigureTriggerBounds(new Vector2(0f, 0.82f), new Vector2(1.8f, 1.85f), 1.35f, 1.45f);
                return;
            }

            encounterTitle = enemyCount > 1 ? "腐化狼群" : "腐化狼";
            enemyName = "腐化狼";
            enemyHealth = 26 + bonus * 4;
            enemyAttack = 8 + bonus;
            enemyDefense = 2;
            experienceReward = 3;
            goldReward = 8 + bonus * 2;
            lootName = "腐化狼牙";
            lootChance = 0.45f;
            ConfigureTriggerBounds(new Vector2(0f, 0.48f), new Vector2(2.35f, 1.25f), 1.55f, 1.05f);
        }

        public void ConfigureBoss(Sprite sprite, int waveBonus)
        {
            battleSprite = sprite;
            visualKind = TurnBasedEnemyVisualKind.BlackMoonKnight;
            EnsureMapIdleAnimator();
            enemyCount = 1;
            encounterTitle = "月夜骑士";
            enemyName = "月夜骑士";
            enemyHealth = 72 + Mathf.Max(0, waveBonus) * 8;
            enemyAttack = 13 + Mathf.Max(0, waveBonus);
            enemyDefense = 5;
            experienceReward = 9;
            goldReward = 38 + Mathf.Max(0, waveBonus) * 8;
            lootName = "月夜骑士残甲";
            lootChance = 1f;
            ConfigureTriggerBounds(new Vector2(0f, 0.95f), new Vector2(2.35f, 1.95f), 1.65f, 1.35f);
        }

        public void ConfigureContractBoss(Sprite sprite, int waveBonus, string title, string name)
        {
            ConfigureBoss(sprite, waveBonus);
            if (!string.IsNullOrWhiteSpace(title))
            {
                encounterTitle = title;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                enemyName = name;
            }
        }

        public void ApplyInvestigationModifiers(
            int healthPenalty,
            int attackPenalty,
            int defensePenalty,
            int shieldAdjustment,
            string[] weaknessLabels,
            bool[] weaknessDiscovery,
            string openingNote)
        {
            investigationHealthPenalty = Mathf.Max(0, healthPenalty);
            investigationAttackPenalty = Mathf.Max(0, attackPenalty);
            investigationDefensePenalty = Mathf.Max(0, defensePenalty);
            investigationShieldAdjustment = shieldAdjustment;
            investigationWeaknessLabels = weaknessLabels;
            investigationWeaknessDiscovery = weaknessDiscovery;
            battleOpeningNote = openingNote;
        }

        public void ApplyMapScale(float visualScale)
        {
            float safeScale = Mathf.Clamp(visualScale, 0.15f, 1.5f);
            transform.localScale = Vector3.one * safeScale;
            triggerHalfWidth *= safeScale;
            triggerHalfHeight *= safeScale;
        }

        public List<TurnBasedEnemyState> CreateBattleEnemies()
        {
            List<TurnBasedEnemyState> result = new List<TurnBasedEnemyState>();
            for (int i = 0; i < Mathf.Max(1, enemyCount); i++)
            {
                TurnBasedEnemyState enemy = new TurnBasedEnemyState
                {
                    Name = enemyCount <= 1 ? enemyName : $"{enemyName} {i + 1}",
                    MaxHealth = Mathf.Max(1, enemyHealth - investigationHealthPenalty),
                    Health = Mathf.Max(1, enemyHealth - investigationHealthPenalty),
                    Attack = Mathf.Max(1, enemyAttack - investigationAttackPenalty),
                    Defense = Mathf.Max(0, enemyDefense - investigationDefensePenalty),
                    ExperienceReward = experienceReward,
                    GoldReward = goldReward,
                    LootName = lootName,
                    LootChance = lootChance,
                    Sprite = battleSprite,
                    SourceObject = gameObject,
                    ShieldAdjustment = investigationShieldAdjustment,
                    WeaknessLabelsOverride = investigationWeaknessLabels,
                    WeaknessDiscoveryOverride = investigationWeaknessDiscovery,
                    BattleOpeningNote = battleOpeningNote
                };
                TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, visualKind);
                result.Add(enemy);
            }

            return result;
        }

        public void PrepareForBattle()
        {
            consumed = true;
            if (encounterCollider != null)
            {
                encounterCollider.enabled = false;
            }
        }

        public void ReleaseAfterEscape()
        {
            consumed = false;
            if (encounterCollider != null)
            {
                encounterCollider.enabled = true;
            }
        }

        public void ConsumeEncounter()
        {
            consumed = true;
            if (destroyOnWin)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            player = FindObjectOfType<GeraltController>();
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null || !player.IsAlive)
            {
                return;
            }

            float horizontalDistance = Mathf.Abs(player.transform.position.x - transform.position.x);
            float verticalDistance = Mathf.Abs(player.transform.position.y - transform.position.y);
            if (horizontalDistance <= triggerHalfWidth && verticalDistance <= triggerHalfHeight)
            {
                TryStartBattle(player);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GeraltController touchingPlayer = other.GetComponent<GeraltController>();
            TryStartBattle(touchingPlayer);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            GeraltController touchingPlayer = other.GetComponent<GeraltController>();
            TryStartBattle(touchingPlayer);
        }

        private void TryStartBattle(GeraltController touchingPlayer)
        {
            if (consumed || touchingPlayer == null || !touchingPlayer.IsAlive)
            {
                return;
            }

            TurnBasedBattleManager manager = TurnBasedBattleManager.CreateIfMissing(touchingPlayer);
            manager.TryBeginBattle(this);
        }

        private void ConfigureTriggerBounds(Vector2 offset, Vector2 size, float halfWidth, float halfHeight)
        {
            triggerHalfWidth = Mathf.Max(0.1f, halfWidth);
            triggerHalfHeight = Mathf.Max(0.1f, halfHeight);
            BoxCollider2D boxCollider = encounterCollider as BoxCollider2D;
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }

            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
                boxCollider.offset = offset;
                boxCollider.size = size;
            }
        }

        private void EnsureKinematicBody()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void EnsureMapIdleAnimator()
        {
            MapEncounterIdleAnimator idleAnimator = GetComponent<MapEncounterIdleAnimator>();
            if (idleAnimator == null)
            {
                idleAnimator = gameObject.AddComponent<MapEncounterIdleAnimator>();
            }

            idleAnimator.Configure(visualKind);
        }

        private void OnDisable()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }
        }
    }
}
