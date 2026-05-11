using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public enum TurnBattleAction
    {
        Attack,
        FlameSign,
        Defend,
        Item,
        Escape
    }

    public class TurnBasedEnemyState
    {
        public string Name;
        public int MaxHealth;
        public int Health;
        public int Attack;
        public int Defense;
        public int ExperienceReward;
        public TurnBasedEnemyVisualKind VisualKind;
        public Sprite Sprite;
        public Sprite[] IdleFrames;
        public Sprite[] AttackFrames;
        public Sprite[] HurtFrames;
        public GameObject SourceObject;

        public bool IsAlive => Health > 0;
    }

    public class TurnBasedBattleManager : MonoBehaviour
    {
        private const string ManagerName = "Turn Based Battle Manager";

        [SerializeField] private int playerAttack = 18;
        [SerializeField] private int playerDefense = 4;
        [SerializeField] private int flameManaCost = 18;
        [SerializeField] private int flameBaseDamage = 26;
        [SerializeField] private int potionHealAmount = 32;
        [SerializeField] private int startingPotionCount = 3;
        [SerializeField, Range(0f, 1f)] private float escapeChance = 0.62f;

        private static TurnBasedBattleManager instance;

        private readonly List<TurnBasedEnemyState> enemies = new List<TurnBasedEnemyState>();
        private GeraltController player;
        private GeraltAnimator playerAnimator;
        private TurnBasedBattleHud battleHud;
        private BattleEncounterTrigger currentEncounter;
        private bool battleActive;
        private bool resolvingTurn;
        private bool defending;
        private int potionCount;

        public bool BattleActive => battleActive;

        public static TurnBasedBattleManager CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            TurnBasedBattleManager existing = FindObjectOfType<TurnBasedBattleManager>();
            if (existing != null)
            {
                instance = existing;
                instance.SetPlayer(target);
                return existing;
            }

            GameObject managerObject = new GameObject(ManagerName);
            TurnBasedBattleManager manager = managerObject.AddComponent<TurnBasedBattleManager>();
            manager.SetPlayer(target);
            return manager;
        }

        private void Awake()
        {
            instance = this;
            potionCount = startingPotionCount;
        }

        private void Update()
        {
            if (!battleActive || resolvingTurn)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectAction(TurnBattleAction.Attack);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectAction(TurnBattleAction.FlameSign);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectAction(TurnBattleAction.Defend);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectAction(TurnBattleAction.Item);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SelectAction(TurnBattleAction.Escape);
            }
        }

        public bool TryBeginBattle(BattleEncounterTrigger encounter)
        {
            if (battleActive || encounter == null)
            {
                return false;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null || !player.IsAlive)
            {
                return false;
            }

            currentEncounter = encounter;
            enemies.Clear();
            enemies.AddRange(encounter.CreateBattleEnemies());
            if (enemies.Count == 0)
            {
                return false;
            }

            playerAnimator = player.GetComponent<GeraltAnimator>();
            player.SetControlEnabled(false);
            currentEncounter.PrepareForBattle();
            battleActive = true;
            resolvingTurn = false;
            defending = false;

            battleHud = TurnBasedBattleHud.CreateIfMissing(this);
            battleHud.Show(enemies, player, potionCount);
            battleHud.SetMessage($"遭遇 {currentEncounter.EncounterTitle}！");
            return true;
        }

        public void SelectAction(TurnBattleAction action)
        {
            if (!battleActive || resolvingTurn)
            {
                return;
            }

            StartCoroutine(ResolvePlayerAction(action));
        }

        private void SetPlayer(GeraltController target)
        {
            player = target;
            playerAnimator = player == null ? null : player.GetComponent<GeraltAnimator>();
        }

        private IEnumerator ResolvePlayerAction(TurnBattleAction action)
        {
            resolvingTurn = true;
            battleHud.SetCommandsEnabled(false);
            defending = false;

            bool playerTurnConsumed = true;
            switch (action)
            {
                case TurnBattleAction.Attack:
                    yield return PlayerAttack();
                    break;
                case TurnBattleAction.FlameSign:
                    playerTurnConsumed = player != null && player.TrySpendMana(flameManaCost);
                    if (playerTurnConsumed)
                    {
                        yield return PlayerFlameSign();
                    }
                    else
                    {
                        battleHud.SetMessage("魔力不足，无法释放火焰法印。");
                        yield return Wait(0.55f);
                    }
                    break;
                case TurnBattleAction.Defend:
                    defending = true;
                    battleHud.SetMessage("猎魔人架起银剑，准备承受攻击。");
                    yield return Wait(0.55f);
                    break;
                case TurnBattleAction.Item:
                    playerTurnConsumed = UsePotion();
                    yield return Wait(0.55f);
                    break;
                case TurnBattleAction.Escape:
                    yield return TryEscape();
                    if (!battleActive)
                    {
                        yield break;
                    }
                    break;
            }

            battleHud.Refresh(enemies, player, potionCount);
            if (CheckBattleEnded())
            {
                yield break;
            }

            if (playerTurnConsumed)
            {
                yield return EnemyPhase();
            }

            if (CheckBattleEnded())
            {
                yield break;
            }

            resolvingTurn = false;
            battleHud.SetCommandsEnabled(true);
            battleHud.SetMessage("选择行动。  1攻击  2火焰  3防御  4物品  5逃跑");
        }

        private IEnumerator PlayerAttack()
        {
            int targetIndex = GetFirstLivingEnemyIndex();
            TurnBasedEnemyState target = targetIndex < 0 ? null : enemies[targetIndex];
            if (target == null)
            {
                yield break;
            }

            playerAnimator?.PlaySlash();
            int damage = Mathf.Max(1, playerAttack + Random.Range(-3, 4) - target.Defense);
            target.Health = Mathf.Max(0, target.Health - damage);
            battleHud.SetMessage($"猎魔人攻击 {target.Name}，造成 {damage} 点伤害！");
            WitcherCombatFeedback.EnemyHit(player.transform.position + Vector3.right * 1.2f, 0.06f, 0.025f);
            yield return battleHud.PlayEnemyHurt(targetIndex, 0f, damage);
            battleHud.Refresh(enemies, player, potionCount);
            if (!target.IsAlive)
            {
                battleHud.SetMessage($"{target.Name} 被斩倒了。");
                yield return Wait(0.45f);
            }
        }

        private IEnumerator PlayerFlameSign()
        {
            playerAnimator?.PlaySlash();
            int hitCount = 0;
            int[] damages = new int[enemies.Count];
            for (int i = 0; i < enemies.Count; i++)
            {
                TurnBasedEnemyState enemy = enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                int damage = Mathf.Max(1, flameBaseDamage + Random.Range(-4, 5) - enemy.Defense / 2);
                enemy.Health = Mathf.Max(0, enemy.Health - damage);
                damages[i] = damage;
                hitCount++;
            }

            battleHud.SetMessage(hitCount <= 1 ? "火焰法印吞噬了敌人！" : "火焰法印横扫敌群！");
            WitcherCombatFeedback.HeavyEnemyHit(player.transform.position + Vector3.right * 1.8f);
            yield return battleHud.PlayFlameSignEffect();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (damages[i] > 0)
                {
                    yield return battleHud.PlayEnemyHurt(i, 0.08f, damages[i]);
                }
            }

            battleHud.Refresh(enemies, player, potionCount);
        }

        private bool UsePotion()
        {
            if (potionCount <= 0)
            {
                battleHud.SetMessage("药剂已经用完了。");
                return false;
            }

            if (player == null || player.CurrentHealth >= player.MaxHealth)
            {
                battleHud.SetMessage("现在还不需要喝药。");
                return false;
            }

            potionCount--;
            player.RestoreHealth(potionHealAmount);
            battleHud.SetMessage($"喝下燕子药剂，恢复 {potionHealAmount} 点生命。");
            return true;
        }

        private IEnumerator TryEscape()
        {
            if (Random.value <= escapeChance)
            {
                battleHud.SetMessage("猎魔人撤出战斗，重新寻找机会。");
                yield return Wait(0.65f);
                EndBattle(false, false);
            }
            else
            {
                battleHud.SetMessage("撤退失败，怪物逼了上来！");
                yield return Wait(0.55f);
            }
        }

        private IEnumerator EnemyPhase()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                TurnBasedEnemyState enemy = enemies[i];
                if (!enemy.IsAlive || player == null || !player.IsAlive)
                {
                    continue;
                }

                int rawDamage = Mathf.Max(1, enemy.Attack + Random.Range(-2, 3) - playerDefense);
                int damage = defending ? Mathf.Max(1, Mathf.CeilToInt(rawDamage * 0.45f)) : rawDamage;
                battleHud.SetMessage($"{enemy.Name} 发起攻击，造成 {damage} 点伤害！");
                yield return battleHud.PlayEnemyAttack(i);
                player.TakeTurnBasedDamage(damage, player.transform.position.x + 1f);
                battleHud.Refresh(enemies, player, potionCount);
                yield return Wait(0.28f);
            }
        }

        private bool CheckBattleEnded()
        {
            if (player == null || !player.IsAlive)
            {
                battleHud.SetMessage("你失败了……");
                resolvingTurn = true;
                return true;
            }

            if (GetFirstLivingEnemy() != null)
            {
                return false;
            }

            StartCoroutine(WinBattle());
            return true;
        }

        private IEnumerator WinBattle()
        {
            resolvingTurn = true;
            int reward = 0;
            foreach (TurnBasedEnemyState enemy in enemies)
            {
                reward += Mathf.Max(1, enemy.ExperienceReward);
            }

            battleHud.SetMessage($"胜利！获得 {reward} 点猎魔经验。");
            WitcherCombatText.Spawn($"+{reward} XP", player.transform.position + Vector3.up * 1.2f, new Color32(255, 219, 91, 255));
            yield return Wait(0.9f);
            EndBattle(true, true);
        }

        private void EndBattle(bool won, bool consumeEncounter)
        {
            if (consumeEncounter && currentEncounter != null)
            {
                currentEncounter.ConsumeEncounter();
            }
            else if (currentEncounter != null)
            {
                currentEncounter.ReleaseAfterEscape();
            }

            battleHud?.Hide();
            battleActive = false;
            resolvingTurn = false;
            defending = false;
            currentEncounter = null;
            enemies.Clear();

            if (player != null && player.IsAlive)
            {
                player.SetControlEnabled(true);
            }
        }

        private TurnBasedEnemyState GetFirstLivingEnemy()
        {
            int index = GetFirstLivingEnemyIndex();
            return index < 0 ? null : enemies[index];
        }

        private int GetFirstLivingEnemyIndex()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return i;
                }
            }

            return -1;
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
