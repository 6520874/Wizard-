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

    // 中文说明：保存敌人进入回合制战斗后的属性和动画资源引用。
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

    // 中文说明：负责回合制战斗流程、玩家行动、敌人回合和战斗结算。
    public class TurnBasedBattleManager : MonoBehaviour
    {
        private const string ManagerName = "Turn Based Battle Manager";

        [SerializeField] private int playerAttack = 18;
        [SerializeField] private int playerDefense = 4;
        [SerializeField] private int potionHealAmount = 32;
        [SerializeField] private int startingPotionCount = 3;
        [SerializeField, Range(0f, 1f)] private float escapeChance = 0.62f;

        private const float TurnActionThreshold = 100f;
        private const int PlayerTurnSpeed = 116;

        private static TurnBasedBattleManager instance;

        private readonly List<TurnBasedEnemyState> enemies = new List<TurnBasedEnemyState>();
        private readonly List<BattleTurnUnit> turnUnits = new List<BattleTurnUnit>();
        private readonly List<CameraRenderState> pausedRenderCameras = new List<CameraRenderState>();
        private readonly List<BattleStatusEffect> playerStatuses = new List<BattleStatusEffect>();
        private GeraltController player;
        private GeraltAnimator playerAnimator;
        private TurnBasedBattleHud battleHud;
        private BattleEncounterTrigger currentEncounter;
        private BattleTurnUnit activeTurnUnit;
        private bool battleActive;
        private bool resolvingTurn;
        private bool worldRenderingPaused;
        private int potionCount;

        public bool BattleActive => battleActive;

        private struct CameraRenderState
        {
            public Camera Camera;
            public int CullingMask;
            public CameraClearFlags ClearFlags;
            public Color BackgroundColor;
        }

        // 中文说明：保存一个参战单位的速度进度，让玩家和怪物共用同一套出手顺序。
        private class BattleTurnUnit
        {
            public bool IsPlayer;
            public int EnemyIndex;
            public int Speed;
            public float ActionValue;
        }

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
            resolvingTurn = true;
            playerStatuses.Clear();
            BuildTurnUnits();

            battleHud = TurnBasedBattleHud.CreateIfMissing(this);
            battleHud.Show(enemies, player, potionCount);
            battleHud.SetCommandsEnabled(false);
            PauseWorldRendering();
            battleHud.SetMessage($"遭遇 {currentEncounter.EncounterTitle}！");
            StartCoroutine(DispatchNextTurn(0.42f));
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

            bool playerTurnConsumed = true;
            switch (action)
            {
                case TurnBattleAction.Attack:
                    yield return PlayerUseSkill(WitcherSkillBook.GetPlayerSkill(action));
                    break;
                case TurnBattleAction.FlameSign:
                    SkillDefinition flameSkill = WitcherSkillBook.GetPlayerSkill(action);
                    if (player != null && player.CurrentMana >= flameSkill.ManaCost)
                    {
                        yield return PlayerUseSkill(flameSkill);
                    }
                    else
                    {
                        playerTurnConsumed = false;
                        battleHud.SetMessage("魔力不足，无法释放火焰法印。");
                        yield return Wait(0.55f);
                    }
                    break;
                case TurnBattleAction.Defend:
                    yield return PlayerUseSkill(WitcherSkillBook.GetPlayerSkill(action));
                    break;
                case TurnBattleAction.Item:
                    playerTurnConsumed = CanUsePotion();
                    if (playerTurnConsumed)
                    {
                        potionCount--;
                        yield return PlayerUseSkill(WitcherSkillBook.CreatePotion(potionHealAmount));
                    }
                    else
                    {
                        yield return Wait(0.55f);
                    }
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

            if (!playerTurnConsumed)
            {
                resolvingTurn = false;
                battleHud.SetCommandsEnabled(true);
                yield break;
            }

            yield return DispatchNextTurn(0.18f);
        }

        private IEnumerator PlayerUseSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                yield break;
            }

            BattleSkillUnit caster = CreatePlayerSkillUnit();
            List<BattleSkillUnit> targets = CreateSkillTargets(skill, caster);
            if (targets.Count == 0 && skill.TargetKind != BattleSkillTargetKind.Self)
            {
                yield break;
            }

            if (skill.ManaCost > 0 && (player == null || !player.TrySpendMana(skill.ManaCost)))
            {
                battleHud.SetMessage($"魔力不足，无法释放{skill.DisplayName}。");
                yield return Wait(0.55f);
                yield break;
            }

            SkillResult result = SkillExecutor.Execute(skill, caster, targets);
            battleHud.SetMessage(result.Message);
            switch (skill.AnimationKind)
            {
                case BattleSkillAnimationKind.Slash:
                    playerAnimator?.PlaySlash();
                    yield return battleHud.PlayPlayerAttack(GetFirstTargetEnemyIndex(result));
                    break;
                case BattleSkillAnimationKind.Flame:
                    playerAnimator?.PlaySlash();
                    yield return battleHud.PlayPlayerCast();
                    WitcherCombatFeedback.HeavyEnemyHit(player.transform.position + Vector3.right * 1.8f);
                    yield return battleHud.PlayFlameSignEffect();
                    break;
                case BattleSkillAnimationKind.Cast:
                    playerAnimator?.PlaySlash();
                    yield return battleHud.PlayPlayerCast();
                    break;
                case BattleSkillAnimationKind.Defend:
                case BattleSkillAnimationKind.Item:
                    yield return Wait(0.55f);
                    break;
            }

            ApplyPlayerSkillResult(caster, result);
            yield return PlaySkillResultFeedback(result);
            battleHud.Refresh(enemies, player, potionCount);
        }

        private bool CanUsePotion()
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

        private IEnumerator DispatchNextTurn(float delay)
        {
            resolvingTurn = true;
            battleHud.SetCommandsEnabled(false);
            if (delay > 0f)
            {
                yield return Wait(delay);
            }

            if (CheckBattleEnded())
            {
                yield break;
            }

            activeTurnUnit = TakeNextTurnUnit();
            if (activeTurnUnit == null)
            {
                resolvingTurn = false;
                yield break;
            }

            if (activeTurnUnit.IsPlayer)
            {
                resolvingTurn = false;
                battleHud.SetCommandsEnabled(true);
                battleHud.SetMessage("猎魔人准备行动。  1攻击  2火焰  3防御  4物品  5逃跑");
                yield break;
            }

            yield return ResolveEnemyTurn(activeTurnUnit);
            if (CheckBattleEnded())
            {
                yield break;
            }

            yield return DispatchNextTurn(0.18f);
        }

        private IEnumerator ResolveEnemyTurn(BattleTurnUnit enemyTurn)
        {
            int enemyIndex = enemyTurn.EnemyIndex;
            if (!IsLivingEnemyTurn(enemyTurn) || player == null || !player.IsAlive)
            {
                yield break;
            }

            TurnBasedEnemyState enemy = enemies[enemyIndex];
            int rawDamage = Mathf.Max(1, enemy.Attack + Random.Range(-2, 3) - playerDefense);
            int damage = CalculatePlayerIncomingDamage(rawDamage);
            battleHud.SetMessage($"{enemy.Name} 抢到先机，造成 {damage} 点伤害！");
            yield return battleHud.PlayEnemyAttack(enemyIndex);
            player.TakeTurnBasedDamage(damage, player.transform.position.x + 1f);
            yield return battleHud.PlayPlayerHurt(damage);
            ConsumePlayerStatusTurns();
            battleHud.Refresh(enemies, player, potionCount);
            yield return Wait(0.24f);
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
            ResumeWorldRendering();
            battleActive = false;
            resolvingTurn = false;
            playerStatuses.Clear();
            currentEncounter = null;
            enemies.Clear();
            turnUnits.Clear();
            activeTurnUnit = null;

            if (player != null && player.IsAlive)
            {
                player.SetControlEnabled(true);
            }
        }

        private void PauseWorldRendering()
        {
            if (worldRenderingPaused)
            {
                return;
            }

            pausedRenderCameras.Clear();
            Camera[] cameras = FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || !camera.enabled)
                {
                    continue;
                }

                pausedRenderCameras.Add(new CameraRenderState
                {
                    Camera = camera,
                    CullingMask = camera.cullingMask,
                    ClearFlags = camera.clearFlags,
                    BackgroundColor = camera.backgroundColor
                });
                camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(3, 5, 8, 255);
            }

            worldRenderingPaused = true;
        }

        private void ResumeWorldRendering()
        {
            if (!worldRenderingPaused)
            {
                return;
            }

            for (int i = 0; i < pausedRenderCameras.Count; i++)
            {
                CameraRenderState state = pausedRenderCameras[i];
                if (state.Camera != null)
                {
                    state.Camera.cullingMask = state.CullingMask;
                    state.Camera.clearFlags = state.ClearFlags;
                    state.Camera.backgroundColor = state.BackgroundColor;
                }
            }

            pausedRenderCameras.Clear();
            worldRenderingPaused = false;
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

        private BattleSkillUnit CreatePlayerSkillUnit()
        {
            BattleSkillUnit unit = BattleSkillUnit.CreatePlayer("猎魔人", player.MaxHealth, player.MaxMana, playerAttack, playerDefense);
            unit.SetHealth(player.CurrentHealth);
            unit.SetMana(player.CurrentMana);
            for (int i = 0; i < playerStatuses.Count; i++)
            {
                unit.AddStatusInstance(playerStatuses[i]);
            }

            return unit;
        }

        private BattleSkillUnit CreateEnemySkillUnit(int index)
        {
            TurnBasedEnemyState enemy = enemies[index];
            BattleSkillUnit unit = BattleSkillUnit.CreateEnemy(enemy.Name, index, enemy.MaxHealth, enemy.Attack, enemy.Defense);
            unit.SetHealth(enemy.Health);
            return unit;
        }

        private List<BattleSkillUnit> CreateSkillTargets(SkillDefinition skill, BattleSkillUnit caster)
        {
            List<BattleSkillUnit> targets = new List<BattleSkillUnit>();
            switch (skill.TargetKind)
            {
                case BattleSkillTargetKind.Self:
                    targets.Add(caster);
                    break;
                case BattleSkillTargetKind.AllLivingEnemies:
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (enemies[i].IsAlive)
                        {
                            targets.Add(CreateEnemySkillUnit(i));
                        }
                    }
                    break;
                default:
                    int targetIndex = GetFirstLivingEnemyIndex();
                    if (targetIndex >= 0)
                    {
                        targets.Add(CreateEnemySkillUnit(targetIndex));
                    }
                    break;
            }

            return targets;
        }

        private void ApplyPlayerSkillResult(BattleSkillUnit caster, SkillResult result)
        {
            if (caster != null)
            {
                playerStatuses.Clear();
                for (int i = 0; i < caster.Statuses.Count; i++)
                {
                    playerStatuses.Add(caster.Statuses[i]);
                }
            }

            for (int i = 0; i < result.TargetResults.Count; i++)
            {
                SkillTargetResult targetResult = result.TargetResults[i];
                if (targetResult.IsPlayerTarget)
                {
                    if (targetResult.Healing > 0)
                    {
                        player.RestoreHealth(targetResult.Healing);
                    }

                    if (targetResult.ManaRestored > 0)
                    {
                        player.RestoreMana(targetResult.ManaRestored);
                    }

                    continue;
                }

                if (targetResult.EnemyIndex >= 0 && targetResult.EnemyIndex < enemies.Count)
                {
                    enemies[targetResult.EnemyIndex].Health = Mathf.Clamp(targetResult.FinalHealth, 0, enemies[targetResult.EnemyIndex].MaxHealth);
                }
            }
        }

        private IEnumerator PlaySkillResultFeedback(SkillResult result)
        {
            for (int i = 0; i < result.TargetResults.Count; i++)
            {
                SkillTargetResult targetResult = result.TargetResults[i];
                if (targetResult.IsPlayerTarget || targetResult.Damage <= 0)
                {
                    continue;
                }

                yield return battleHud.PlayEnemyHurt(targetResult.EnemyIndex, 0.08f, targetResult.Damage);
                if (targetResult.Defeated)
                {
                    battleHud.SetMessage($"{targetResult.TargetName} 被击倒了。");
                    yield return Wait(0.28f);
                }
            }
        }

        private int GetFirstTargetEnemyIndex(SkillResult result)
        {
            for (int i = 0; i < result.TargetResults.Count; i++)
            {
                if (!result.TargetResults[i].IsPlayerTarget && result.TargetResults[i].EnemyIndex >= 0)
                {
                    return result.TargetResults[i].EnemyIndex;
                }
            }

            return GetFirstLivingEnemyIndex();
        }

        private int CalculatePlayerIncomingDamage(int rawDamage)
        {
            int damage = Mathf.Max(1, rawDamage);
            for (int i = 0; i < playerStatuses.Count; i++)
            {
                damage = Mathf.Max(1, Mathf.CeilToInt(damage * playerStatuses[i].IncomingDamageMultiplier));
            }

            return damage;
        }

        private void ConsumePlayerStatusTurns()
        {
            for (int i = playerStatuses.Count - 1; i >= 0; i--)
            {
                playerStatuses[i].ConsumeTurn();
                if (playerStatuses[i].Expired)
                {
                    playerStatuses.RemoveAt(i);
                }
            }
        }

        private void BuildTurnUnits()
        {
            turnUnits.Clear();
            turnUnits.Add(new BattleTurnUnit
            {
                IsPlayer = true,
                EnemyIndex = -1,
                Speed = PlayerTurnSpeed
            });

            for (int i = 0; i < enemies.Count; i++)
            {
                turnUnits.Add(new BattleTurnUnit
                {
                    IsPlayer = false,
                    EnemyIndex = i,
                    Speed = GetEnemyTurnSpeed(enemies[i], i)
                });
            }
        }

        // 中文说明：推进速度槽到下一名单位出手，速度更高的单位会更频繁抢到行动机会。
        private BattleTurnUnit TakeNextTurnUnit()
        {
            for (int step = 0; step < 64; step++)
            {
                BattleTurnUnit readyUnit = FindReadyTurnUnit();
                if (readyUnit != null)
                {
                    readyUnit.ActionValue -= TurnActionThreshold;
                    return readyUnit;
                }

                for (int i = 0; i < turnUnits.Count; i++)
                {
                    BattleTurnUnit unit = turnUnits[i];
                    if (!IsTurnUnitAlive(unit))
                    {
                        continue;
                    }

                    unit.ActionValue += Mathf.Max(1, unit.Speed);
                }
            }

            return null;
        }

        private BattleTurnUnit FindReadyTurnUnit()
        {
            BattleTurnUnit readyUnit = null;
            for (int i = 0; i < turnUnits.Count; i++)
            {
                BattleTurnUnit unit = turnUnits[i];
                if (!IsTurnUnitAlive(unit) || unit.ActionValue < TurnActionThreshold)
                {
                    continue;
                }

                if (readyUnit == null || unit.ActionValue > readyUnit.ActionValue)
                {
                    readyUnit = unit;
                }
            }

            return readyUnit;
        }

        private bool IsTurnUnitAlive(BattleTurnUnit unit)
        {
            if (unit == null)
            {
                return false;
            }

            return unit.IsPlayer ? player != null && player.IsAlive : IsLivingEnemyTurn(unit);
        }

        private bool IsLivingEnemyTurn(BattleTurnUnit unit)
        {
            return unit != null
                && !unit.IsPlayer
                && unit.EnemyIndex >= 0
                && unit.EnemyIndex < enemies.Count
                && enemies[unit.EnemyIndex].IsAlive;
        }

        private static int GetEnemyTurnSpeed(TurnBasedEnemyState enemy, int slotIndex)
        {
            int baseSpeed;
            switch (enemy.VisualKind)
            {
                case TurnBasedEnemyVisualKind.CorruptedWolf:
                    baseSpeed = 108;
                    break;
                case TurnBasedEnemyVisualKind.BloodWraith:
                    baseSpeed = 98;
                    break;
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    baseSpeed = 84;
                    break;
                default:
                    baseSpeed = 94;
                    break;
            }

            return Mathf.Max(56, baseSpeed - Mathf.Max(0, slotIndex) * 3);
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
