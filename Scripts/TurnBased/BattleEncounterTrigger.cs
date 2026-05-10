using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(Collider2D))]
    public class BattleEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private string encounterTitle = "怪物";
        [SerializeField] private string enemyName = "腐化野兽";
        [SerializeField] private int enemyCount = 1;
        [SerializeField] private int enemyHealth = 28;
        [SerializeField] private int enemyAttack = 8;
        [SerializeField] private int enemyDefense = 2;
        [SerializeField] private int experienceReward = 2;
        [SerializeField] private bool destroyOnWin = true;

        private Sprite battleSprite;
        private TurnBasedEnemyVisualKind visualKind = TurnBasedEnemyVisualKind.CorruptedWolf;
        private bool consumed;
        private Collider2D encounterCollider;

        public string EncounterTitle => encounterTitle;

        private void Awake()
        {
            encounterCollider = GetComponent<Collider2D>();
            encounterCollider.isTrigger = true;
        }

        public void ConfigureMonster(WitcherHordeMonsterKind kind, Sprite sprite, int count, int waveBonus)
        {
            battleSprite = sprite;
            visualKind = kind == WitcherHordeMonsterKind.BloodWraith ? TurnBasedEnemyVisualKind.BloodWraith : TurnBasedEnemyVisualKind.CorruptedWolf;
            enemyCount = Mathf.Clamp(count, 1, 3);
            int bonus = Mathf.Max(0, waveBonus);
            if (kind == WitcherHordeMonsterKind.BloodWraith)
            {
                encounterTitle = enemyCount > 1 ? "吸血女妖群" : "吸血女妖";
                enemyName = "吸血女妖";
                enemyHealth = 34 + bonus * 5;
                enemyAttack = 10 + bonus;
                enemyDefense = 3;
                experienceReward = 4;
                return;
            }

            encounterTitle = enemyCount > 1 ? "腐化狼群" : "腐化狼";
            enemyName = "腐化狼";
            enemyHealth = 26 + bonus * 4;
            enemyAttack = 8 + bonus;
            enemyDefense = 2;
            experienceReward = 3;
        }

        public void ConfigureBoss(Sprite sprite, int waveBonus)
        {
            battleSprite = sprite;
            visualKind = TurnBasedEnemyVisualKind.BlackMoonKnight;
            enemyCount = 1;
            encounterTitle = "黑月骑士";
            enemyName = "黑月骑士";
            enemyHealth = 72 + Mathf.Max(0, waveBonus) * 8;
            enemyAttack = 13 + Mathf.Max(0, waveBonus);
            enemyDefense = 5;
            experienceReward = 9;
        }

        public List<TurnBasedEnemyState> CreateBattleEnemies()
        {
            List<TurnBasedEnemyState> result = new List<TurnBasedEnemyState>();
            for (int i = 0; i < Mathf.Max(1, enemyCount); i++)
            {
                TurnBasedEnemyState enemy = new TurnBasedEnemyState
                {
                    Name = enemyCount <= 1 ? enemyName : $"{enemyName} {i + 1}",
                    MaxHealth = enemyHealth,
                    Health = enemyHealth,
                    Attack = enemyAttack,
                    Defense = enemyDefense,
                    ExperienceReward = experienceReward,
                    Sprite = battleSprite,
                    SourceObject = gameObject
                };
                TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, visualKind);
                result.Add(enemy);
            }

            return result;
        }

        public void PrepareForBattle()
        {
            consumed = true;
            SetWorldLogicEnabled(false);
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
            SetWorldLogicEnabled(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other.GetComponent<GeraltController>() == null)
            {
                return;
            }

            TurnBasedBattleManager manager = TurnBasedBattleManager.CreateIfMissing(other.GetComponent<GeraltController>());
            manager.TryBeginBattle(this);
        }

        private void SetWorldLogicEnabled(bool enabled)
        {
            WitcherHordeMonsterController hordeMonster = GetComponent<WitcherHordeMonsterController>();
            if (hordeMonster != null)
            {
                hordeMonster.enabled = enabled;
            }

            WildHuntBossController boss = GetComponent<WildHuntBossController>();
            if (boss != null)
            {
                boss.enabled = enabled;
            }

            MonsterPatrol patrol = GetComponent<MonsterPatrol>();
            if (patrol != null)
            {
                patrol.enabled = enabled;
            }

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }
}
