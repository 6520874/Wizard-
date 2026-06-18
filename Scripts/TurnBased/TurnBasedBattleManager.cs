using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：定义玩家在回合制战斗菜单中可以选择的基础行动。
    public enum TurnBattleAction
    {
        Attack,
        FlameSign,
        Defend,
        Item,
        Escape
    }

    // 中文说明：给战斗 HUD 使用的行动顺序显示数据，不参与真正的战斗结算。
    public struct TurnBattleTimelineEntry
    {
        public string Name;
        public bool IsPlayer;
        public int EnemyIndex;
        public string PartyMemberName;
        public float Readiness;
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
        public int GoldReward;
        public string LootName;
        public float LootChance;
        public TurnBasedEnemyVisualKind VisualKind;
        public Sprite Sprite;
        public Sprite[] IdleFrames;
        public Sprite[] AttackFrames;
        public Sprite[] HurtFrames;
        public Sprite[] DefeatFrames;
        public GameObject SourceObject;
        public int ShieldAdjustment;
        public string[] WeaknessLabelsOverride;
        public bool[] WeaknessDiscoveryOverride;
        public string BattleOpeningNote;
        public readonly List<BattleStatusEffect> Statuses = new List<BattleStatusEffect>();

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
        private static readonly TurnBattleAction[] PlayerCommandOrder =
        {
            TurnBattleAction.Attack,
            TurnBattleAction.FlameSign,
            TurnBattleAction.Item,
            TurnBattleAction.Defend
        };

        private static TurnBasedBattleManager instance;

        private readonly List<TurnBasedEnemyState> enemies = new List<TurnBasedEnemyState>();
        private readonly List<BattleTurnUnit> turnUnits = new List<BattleTurnUnit>();
        private readonly List<CameraRenderState> pausedRenderCameras = new List<CameraRenderState>();
        private readonly List<BattleStatusEffect> playerStatuses = new List<BattleStatusEffect>();
        private readonly List<SkillDefinition> currentSkillSlots = new List<SkillDefinition>();
        private GeraltController player;
        private GeraltAnimator playerAnimator;
        private PlayerInventory playerInventory;
        private TurnBasedBattleHud battleHud;
        private TurnBasedBattleTransition battleTransition;
        private BattleEncounterTrigger currentEncounter;
        private BattleTurnUnit activeTurnUnit;
        private bool battleActive;
        private bool resolvingTurn;
        private bool worldRenderingPaused;
        private bool battleEndSequenceStarted;
        private int potionCount;
        private int turnNumber;
        private int selectedCommandIndex;

        public bool BattleActive => battleActive;
        public int TurnNumber => Mathf.Max(1, turnNumber);
        public event Action<bool> BattleFinished;

        // 中文说明：记录进入战斗前相机的渲染设置，战斗结束后用于恢复地图画面。
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
            public PartyMember Member;
            public int Speed;
            public float ActionValue;
        }

        // 中文说明：用于预演行动条顺序的临时单位数据，不直接改变真实回合状态。
        private struct TimelineSimulationUnit
        {
            public BattleTurnUnit Source;
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

            if (battleHud != null && battleHud.SkillMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SelectSkillSlot(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SelectSkillSlot(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    SelectSkillSlot(2);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    SelectSkillSlot(3);
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    battleHud.HideSkillMenu();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveSelectedCommand(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelectedCommand(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                SelectAction(PlayerCommandOrder[selectedCommandIndex]);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectAction(TurnBattleAction.Attack);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectAction(TurnBattleAction.FlameSign);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectAction(TurnBattleAction.Item);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectAction(TurnBattleAction.Defend);
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

            WitcherSfxPlayer.Play(WitcherSfxCue.EncounterSpawn, 0.55f);
            playerAnimator = player.GetComponent<GeraltAnimator>();
            playerInventory = PlayerInventory.CreateIfMissing(player);
            player.SetControlEnabled(false);
            SetQuestPanelHiddenForBattle(true);
            currentEncounter.PrepareForBattle();
            battleActive = true;
            resolvingTurn = true;
            battleEndSequenceStarted = false;
            selectedCommandIndex = 0;
            playerStatuses.Clear();
            turnNumber = 1;
            BuildTurnUnits();

            StartCoroutine(BeginBattleSequence(currentEncounter.EncounterTitle));
            return true;
        }

        private IEnumerator BeginBattleSequence(string encounterTitle)
        {
            battleTransition = TurnBasedBattleTransition.CreateIfMissing();
            yield return battleTransition.PlayEncounterTransition(encounterTitle, () =>
            {
                if (!battleActive || currentEncounter == null)
                {
                    return;
                }

                battleHud = TurnBasedBattleHud.CreateIfMissing(this);
                battleHud.Show(enemies, player, potionCount);
                battleHud.SetSelectedCommand(selectedCommandIndex);
                battleHud.SetCommandsEnabled(false);
                PauseWorldRendering();
                string openingNote = string.IsNullOrWhiteSpace(currentEncounter.BattleOpeningNote)
                    ? GameText.Battle.Encounter(encounterTitle)
                    : currentEncounter.BattleOpeningNote;
                battleHud.SetMessage(openingNote);
            });

            if (!battleActive || currentEncounter == null)
            {
                yield break;
            }

            StartCoroutine(DispatchNextTurn(0.24f));
        }

        public void SelectAction(TurnBattleAction action)
        {
            if (!battleActive || resolvingTurn)
            {
                return;
            }

            if (action == TurnBattleAction.FlameSign)
            {
                WitcherSfxPlayer.Play(WitcherSfxCue.UiClick, 0.46f);
                ShowActiveSkillMenu();
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.46f);
            StartCoroutine(ResolvePlayerAction(action));
        }

        public void SelectSkill(BattleSkillId skillId)
        {
            if (!battleActive || resolvingTurn)
            {
                return;
            }

            SkillDefinition skill = WitcherSkillBook.GetPlayerSkill(skillId);
            if (skill == null)
            {
                return;
            }

            if (!ActiveFriendlyHasMana(skill.ManaCost))
            {
                battleHud.SetMessage(GameText.Battle.NotEnoughMana(skill.DisplayName));
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.46f);
            StartCoroutine(ResolveSelectedSkill(skill));
        }

        public void SelectSkillSlot(int slotIndex)
        {
            if (!battleActive || resolvingTurn || slotIndex < 0 || slotIndex >= currentSkillSlots.Count)
            {
                return;
            }

            SkillDefinition skill = currentSkillSlots[slotIndex];
            if (skill == null)
            {
                return;
            }

            if (!ActiveFriendlyHasMana(skill.ManaCost))
            {
                battleHud.SetMessage(GameText.Battle.NotEnoughMana(skill.DisplayName));
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.46f);
            StartCoroutine(ResolveSelectedSkill(skill));
        }

        private void ShowActiveSkillMenu()
        {
            currentSkillSlots.Clear();
            PartyMember member = GetActiveFriendlyMember();
            currentSkillSlots.AddRange(WitcherSkillBook.GetFriendlySkills(member));
            WitcherSfxPlayer.Play(WitcherSfxCue.UiClick, 0.45f);
            battleHud.ShowSkillMenu(GetFriendlyDisplayName(member), currentSkillSlots);
        }

        private void MoveSelectedCommand(int delta)
        {
            if (PlayerCommandOrder.Length == 0)
            {
                return;
            }

            selectedCommandIndex = (selectedCommandIndex + delta + PlayerCommandOrder.Length) % PlayerCommandOrder.Length;
            WitcherSfxPlayer.Play(WitcherSfxCue.UiClick, 0.36f);
            battleHud?.SetSelectedCommand(selectedCommandIndex);
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
                    yield return FriendlyUseSkill(GetDefaultAttackSkill(GetActiveFriendlyMember()));
                    break;
                case TurnBattleAction.Defend:
                    yield return FriendlyUseSkill(GetDefaultDefendSkill(GetActiveFriendlyMember()));
                    break;
                case TurnBattleAction.Item:
                    playerTurnConsumed = CanUsePotion();
                    if (playerTurnConsumed)
                    {
                        potionCount--;
                        yield return FriendlyUseSkill(WitcherSkillBook.CreatePotion(potionHealAmount));
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
                battleHud.SetSelectedCommand(selectedCommandIndex);
                yield break;
            }

            turnNumber++;
            yield return DispatchNextTurn(0.18f);
        }

        private IEnumerator ResolveSelectedSkill(SkillDefinition skill)
        {
            resolvingTurn = true;
            battleHud.SetCommandsEnabled(false);
            battleHud.HideSkillMenu();

            yield return FriendlyUseSkill(skill);
            battleHud.Refresh(enemies, player, potionCount);
            if (CheckBattleEnded())
            {
                yield break;
            }

            turnNumber++;
            yield return DispatchNextTurn(0.18f);
        }

        private IEnumerator FriendlyUseSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                yield break;
            }

            PartyMember activeMember = GetActiveFriendlyMember();
            BattleSkillUnit caster = CreateFriendlySkillUnit(activeMember);
            List<BattleSkillUnit> targets = CreateFriendlySkillTargets(skill, caster, activeMember);
            if (targets.Count == 0 && skill.TargetKind != BattleSkillTargetKind.Self)
            {
                yield break;
            }

            if (skill.ManaCost > 0 && !TrySpendActiveFriendlyMana(activeMember, skill.ManaCost))
            {
                battleHud.SetMessage(GameText.Battle.NotEnoughMana(skill.DisplayName));
                yield return Wait(0.55f);
                yield break;
            }

            SkillResult result = SkillExecutor.Execute(skill, caster, targets);
            battleHud.SetMessage(result.Message);
            battleHud.ShowSkillName(skill.DisplayName);
            PlayFriendlySkillSfx(skill);
            switch (skill.AnimationKind)
            {
                case BattleSkillAnimationKind.Slash:
                    if (IsHunter(activeMember))
                    {
                        playerAnimator?.PlaySkillAnimation(GetGeraltAnimationForSkill(skill));
                        if (skill.Id == BattleSkillId.ExecuteSlash)
                        {
                            yield return battleHud.PlayPlayerComboSlash(GetFirstTargetEnemyIndex(result), 3);
                            yield return battleHud.PlaySkillEffect(skill.Id, GetFirstTargetEnemyIndex(result));
                        }
                        else
                        {
                            yield return battleHud.PlayPlayerAttack(GetFirstTargetEnemyIndex(result));
                        }
                    }
                    else
                    {
                        yield return battleHud.PlayPartyMemberSkill(activeMember, skill, GetFirstTargetEnemyIndex(result));
                    }
                    break;
                case BattleSkillAnimationKind.Flame:
                    if (IsHunter(activeMember))
                    {
                        playerAnimator?.PlaySkillAnimation(GetGeraltAnimationForSkill(skill));
                        yield return battleHud.PlayPlayerSkill(skill.Id, skill.AnimationKind);
                        WitcherCombatFeedback.HeavyEnemyHit(player.transform.position + Vector3.right * 1.8f);
                    }
                    else
                    {
                        yield return battleHud.PlayPartyMemberSkill(activeMember, skill, GetFirstTargetEnemyIndex(result));
                    }

                    yield return battleHud.PlaySkillEffect(skill.Id, GetFirstTargetEnemyIndex(result));
                    break;
                case BattleSkillAnimationKind.Cast:
                    if (IsHunter(activeMember))
                    {
                        playerAnimator?.PlaySkillAnimation(GetGeraltAnimationForSkill(skill));
                        yield return battleHud.PlayPlayerSkill(skill.Id, skill.AnimationKind);
                    }
                    else
                    {
                        yield return battleHud.PlayPartyMemberSkill(activeMember, skill, GetFirstTargetEnemyIndex(result));
                    }

                    yield return battleHud.PlaySkillEffect(skill.Id, GetFirstTargetEnemyIndex(result));
                    break;
                case BattleSkillAnimationKind.Defend:
                    if (IsHunter(activeMember))
                    {
                        playerAnimator?.PlaySkillAnimation(GetGeraltAnimationForSkill(skill));
                        yield return battleHud.PlayPlayerSkill(skill.Id, skill.AnimationKind);
                    }
                    else
                    {
                        yield return battleHud.PlayPartyMemberSkill(activeMember, skill, GetFirstTargetEnemyIndex(result));
                    }

                    break;
                case BattleSkillAnimationKind.Item:
                    yield return Wait(0.55f);
                    break;
            }

            ApplyFriendlySkillResult(caster, targets, result);
            yield return PlaySkillResultFeedback(result);
            battleHud.Refresh(enemies, player, potionCount);
        }

        private static void PlayFriendlySkillSfx(SkillDefinition skill)
        {
            if (skill == null)
            {
                return;
            }

            switch (skill.AnimationKind)
            {
                case BattleSkillAnimationKind.Slash:
                    WitcherSfxPlayer.Play(WitcherSfxCue.SwordHit, 0.74f);
                    break;
                case BattleSkillAnimationKind.Flame:
                    WitcherSfxPlayer.Play(WitcherSfxCue.FireCast, 0.78f);
                    break;
                case BattleSkillAnimationKind.Cast:
                    WitcherSfxPlayer.Play(IsFireSkill(skill) ? WitcherSfxCue.FireCast : WitcherSfxCue.MagicWard, 0.7f);
                    break;
                case BattleSkillAnimationKind.Defend:
                    WitcherSfxPlayer.Play(WitcherSfxCue.MagicWard, 0.66f);
                    break;
                case BattleSkillAnimationKind.Item:
                    WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.48f);
                    break;
            }
        }

        private static bool IsFireSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.Id == BattleSkillId.FlameSign
                || skill.Id == BattleSkillId.Fireball
                || skill.Id == BattleSkillId.TrissFirebolt
                || skill.Id == BattleSkillId.TrissMeltingSigil
                || skill.Id == BattleSkillId.TrissMeteorFlare)
            {
                return true;
            }

            for (int i = 0; i < skill.Effects.Count; i++)
            {
                if (skill.Effects[i] is DamageSkillEffect damage && damage.DamageType == BattleDamageType.Fire)
                {
                    return true;
                }
            }

            return false;
        }

        private static GeraltAnimation GetGeraltAnimationForSkill(SkillDefinition skill)
        {
            switch (skill.AnimationKind)
            {
                case BattleSkillAnimationKind.Flame:
                    return GeraltAnimation.FlameSign;
                case BattleSkillAnimationKind.Defend:
                    return GeraltAnimation.ShieldSign;
                case BattleSkillAnimationKind.Cast:
                    return GeraltAnimation.PurpleSign;
                case BattleSkillAnimationKind.Slash:
                    return GeraltAnimation.Slash;
                default:
                    return GeraltAnimation.Idle;
            }
        }

        private bool CanUsePotion()
        {
            if (potionCount <= 0)
            {
                battleHud.SetMessage(GameText.Battle.PotionEmpty);
                return false;
            }

            if (player == null || player.CurrentHealth >= player.MaxHealth)
            {
                battleHud.SetMessage(GameText.Battle.PotionNotNeeded);
                return false;
            }

            return true;
        }

        private IEnumerator TryEscape()
        {
            if (UnityEngine.Random.value <= escapeChance)
            {
                battleHud.SetMessage(GameText.Battle.EscapeSuccess);
                yield return Wait(0.65f);
                EndBattle(false, false);
            }
            else
            {
                battleHud.SetMessage(GameText.Battle.EscapeFailed);
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

            battleHud.Refresh(enemies, player, potionCount);

            if (activeTurnUnit.IsPlayer)
            {
                resolvingTurn = false;
                selectedCommandIndex = 0;
                currentSkillSlots.Clear();
                battleHud.SetCommandsEnabled(true);
                battleHud.SetSelectedCommand(selectedCommandIndex);
                battleHud.SetMessage(GameText.Battle.TurnPrompt(GetTurnUnitName(activeTurnUnit)));
                yield break;
            }

            yield return ResolveEnemyTurn(activeTurnUnit);
            if (CheckBattleEnded())
            {
                yield break;
            }

            turnNumber++;
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
            SkillDefinition skill = ChooseEnemySkill(enemy, enemyIndex);
            BattleSkillUnit caster = CreateEnemySkillUnit(enemyIndex);
            BattleSkillUnit target = CreatePlayerTargetSkillUnit();
            SkillResult result = SkillExecutor.Execute(skill, caster, new[] { target });
            SkillTargetResult playerResult = GetPlayerTargetResult(result);
            int damage = playerResult == null ? 0 : playerResult.Damage;

            battleHud.SetMessage(GameText.Battle.EnemyUseSkill(enemy.Name, skill.DisplayName));
            battleHud.ShowSkillName(skill.DisplayName);
            WitcherSfxPlayer.Play(WitcherSfxCue.EnemyAttack, 0.68f);
            yield return battleHud.PlayEnemyAttack(enemyIndex);
            yield return battleHud.PlayEnemySkillEffect(skill.Id, enemyIndex);
            ApplyEnemySkillResult(enemyIndex, target, result);
            if (damage > 0)
            {
                yield return battleHud.PlayPlayerHurt(damage);
            }

            yield return ConsumePlayerStatusTurnsWithFeedback();
            ConsumeEnemyStatusTurns(enemyIndex);
            battleHud.Refresh(enemies, player, potionCount);
            yield return Wait(0.24f);
        }

        private bool CheckBattleEnded()
        {
            if (player == null || !player.IsAlive)
            {
                if (!battleEndSequenceStarted)
                {
                    StartCoroutine(LoseBattle());
                }

                return true;
            }

            if (GetFirstLivingEnemy() != null)
            {
                return false;
            }

            if (!battleEndSequenceStarted)
            {
                StartCoroutine(WinBattle());
            }

            return true;
        }

        private IEnumerator WinBattle()
        {
            battleEndSequenceStarted = true;
            resolvingTurn = true;
            int reward = 0;
            int goldReward = 0;
            List<string> lootRewards = new List<string>();
            foreach (TurnBasedEnemyState enemy in enemies)
            {
                reward += Mathf.Max(1, enemy.ExperienceReward);
                goldReward += Mathf.Max(0, enemy.GoldReward);
                if (!string.IsNullOrEmpty(enemy.LootName) && UnityEngine.Random.value <= Mathf.Clamp01(enemy.LootChance))
                {
                    lootRewards.Add(enemy.LootName);
                }
            }

            playerInventory = playerInventory == null && player != null ? PlayerInventory.CreateIfMissing(player) : playerInventory;
            playerInventory?.AddBattleRewards(goldReward, reward, lootRewards);
            WitcherSfxPlayer.Play(WitcherSfxCue.BattleVictory, 0.76f);
            battleHud.ShowVictoryRewards(reward, goldReward, lootRewards);
            battleHud.SetMessage(GameText.Battle.VictoryMessage(reward, goldReward));
            WitcherCombatText.Spawn(GameText.Stats.ExperienceGain(reward), player.transform.position + Vector3.up * 1.2f, new Color32(255, 219, 91, 255));
            if (goldReward > 0)
            {
                WitcherCombatText.Spawn(GameText.Stats.GoldGain(goldReward), player.transform.position + Vector3.up * 1.55f, new Color32(255, 203, 88, 255));
            }

            yield return Wait(1.25f);
            EndBattle(true, true);
        }

        private IEnumerator LoseBattle()
        {
            battleEndSequenceStarted = true;
            resolvingTurn = true;
            battleHud.SetCommandsEnabled(false);
            battleHud.SetMessage(GameText.Battle.BattleFailure);
            yield return Wait(1.1f);
            EndBattle(false, false);
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
            SetQuestPanelHiddenForBattle(false);
            battleActive = false;
            resolvingTurn = false;
            battleEndSequenceStarted = false;
            playerStatuses.Clear();
            currentEncounter = null;
            enemies.Clear();
            turnUnits.Clear();
            activeTurnUnit = null;

            if (player != null && player.IsAlive)
            {
                player.SetControlEnabled(true);
            }

            BattleFinished?.Invoke(won);
        }

        private static void SetQuestPanelHiddenForBattle(bool hidden)
        {
            QuestManager questManager = QuestManager.CreateIfMissing();
            questManager.SetQuestPanelSuppressed(hidden);
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

        private BattleSkillUnit CreateFriendlySkillUnit(PartyMember member)
        {
            if (IsHunter(member))
            {
                return CreatePlayerTargetSkillUnit();
            }

            int maxHp = Mathf.Max(1, member.TotalMaxHP);
            int maxMp = Mathf.Max(0, member.TotalMaxMP);
            int attack = IsSorceress(member) ? member.TotalMagic : member.TotalAttack;
            BattleSkillUnit unit = BattleSkillUnit.CreatePlayer(member.Name, maxHp, maxMp, attack, member.TotalDefense);
            unit.SetHealth(Mathf.Clamp(member.HP, 0, maxHp));
            unit.SetMana(Mathf.Clamp(member.MP, 0, maxMp));
            return unit;
        }

        private BattleSkillUnit CreateEnemySkillUnit(int index)
        {
            TurnBasedEnemyState enemy = enemies[index];
            BattleSkillUnit unit = BattleSkillUnit.CreateEnemy(enemy.Name, index, enemy.MaxHealth, enemy.Attack, enemy.Defense);
            unit.SetHealth(enemy.Health);
            for (int i = 0; i < enemy.Statuses.Count; i++)
            {
                unit.AddStatusInstance(enemy.Statuses[i]);
            }

            return unit;
        }

        private BattleSkillUnit CreatePlayerTargetSkillUnit()
        {
            BattleSkillUnit unit = BattleSkillUnit.CreatePlayer(GameText.HunterName, player.MaxHealth, player.MaxMana, GetPlayerAttack(), GetPlayerDefense());
            unit.SetHealth(player.CurrentHealth);
            unit.SetMana(player.CurrentMana);
            for (int i = 0; i < playerStatuses.Count; i++)
            {
                unit.AddStatusInstance(playerStatuses[i]);
            }

            return unit;
        }

        private List<BattleSkillUnit> CreateFriendlySkillTargets(SkillDefinition skill, BattleSkillUnit caster, PartyMember member)
        {
            List<BattleSkillUnit> targets = new List<BattleSkillUnit>();
            switch (skill.TargetKind)
            {
                case BattleSkillTargetKind.Self:
                    targets.Add(IsHunter(member) ? caster : CreatePlayerTargetSkillUnit());
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

        private void ApplyFriendlySkillResult(BattleSkillUnit caster, List<BattleSkillUnit> targets, SkillResult result)
        {
            if (caster != null && IsHunter(GetActiveFriendlyMember()))
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
                    BattleSkillUnit playerTarget = FindPlayerTarget(targets);
                    if (playerTarget != null)
                    {
                        playerStatuses.Clear();
                        for (int statusIndex = 0; statusIndex < playerTarget.Statuses.Count; statusIndex++)
                        {
                            playerStatuses.Add(playerTarget.Statuses[statusIndex]);
                        }
                    }

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
                    BattleSkillUnit enemyTarget = FindEnemyTarget(targets, targetResult.EnemyIndex);
                    if (enemyTarget != null)
                    {
                        enemies[targetResult.EnemyIndex].Statuses.Clear();
                        for (int statusIndex = 0; statusIndex < enemyTarget.Statuses.Count; statusIndex++)
                        {
                            enemies[targetResult.EnemyIndex].Statuses.Add(enemyTarget.Statuses[statusIndex]);
                        }
                    }
                }
            }

            if (caster != null && !IsHunter(GetActiveFriendlyMember()))
            {
                PartyMember member = GetActiveFriendlyMember();
                member.HP = Mathf.Clamp(caster.Health, 0, member.TotalMaxHP);
                member.MP = Mathf.Clamp(caster.Mana, 0, member.TotalMaxMP);
            }
        }

        private static BattleSkillUnit FindPlayerTarget(List<BattleSkillUnit> targets)
        {
            if (targets == null)
            {
                return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].IsPlayer)
                {
                    return targets[i];
                }
            }

            return null;
        }

        private static BattleSkillUnit FindEnemyTarget(List<BattleSkillUnit> targets, int enemyIndex)
        {
            if (targets == null)
            {
                return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && !targets[i].IsPlayer && targets[i].EnemyIndex == enemyIndex)
                {
                    return targets[i];
                }
            }

            return null;
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

                if (targetResult.Defeated)
                {
                    yield return battleHud.PlayEnemyDefeat(targetResult.EnemyIndex, 0.08f, targetResult.Damage);
                    battleHud.SetMessage(GameText.Battle.TargetDefeated(targetResult.TargetName));
                    yield return Wait(0.28f);
                    continue;
                }

                yield return battleHud.PlayEnemyHurt(targetResult.EnemyIndex, 0.08f, targetResult.Damage);
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

        private int GetPlayerAttack()
        {
            return playerAttack;
        }

        private int GetPlayerDefense()
        {
            return playerDefense;
        }

        private IEnumerator ConsumePlayerStatusTurnsWithFeedback()
        {
            for (int i = playerStatuses.Count - 1; i >= 0; i--)
            {
                BattleStatusEffect status = playerStatuses[i];
                if (status.DamageOverTime > 0 && player != null && player.IsAlive)
                {
                    player.TakeTurnBasedDamage(status.DamageOverTime, player.transform.position.x + 1f);
                    battleHud.SetMessage(GameText.Battle.StatusDotDamage(status.DisplayName, status.DamageOverTime));
                    yield return battleHud.PlayPlayerHurt(status.DamageOverTime);
                }

                status.ConsumeTurn();
                if (status.Expired)
                {
                    playerStatuses.RemoveAt(i);
                }
            }
        }

        private void ConsumeEnemyStatusTurns(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                return;
            }

            List<BattleStatusEffect> statuses = enemies[enemyIndex].Statuses;
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                statuses[i].ConsumeTurn();
                if (statuses[i].Expired)
                {
                    statuses.RemoveAt(i);
                }
            }
        }

        private SkillDefinition ChooseEnemySkill(TurnBasedEnemyState enemy, int enemyIndex)
        {
            IReadOnlyList<SkillDefinition> skills = WitcherSkillBook.GetEnemySkills(enemy);
            if (skills.Count == 0)
            {
                return WitcherSkillBook.CreateCorruptedBite();
            }

            if ((enemy.VisualKind == TurnBasedEnemyVisualKind.BlackMoonKnight
                || enemy.VisualKind == TurnBasedEnemyVisualKind.BlackNailThrall
                || enemy.VisualKind == TurnBasedEnemyVisualKind.BlackWaxGateShade
                || enemy.VisualKind == TurnBasedEnemyVisualKind.BlackWaxAcolyte
                || enemy.VisualKind == TurnBasedEnemyVisualKind.BlackNailPuppet) && turnNumber % 3 == 0)
            {
                return WitcherSkillBook.CreateMoonbreaker();
            }

            int seed = Mathf.Abs((turnNumber * 37) + enemyIndex * 17 + enemy.Health);
            return skills[seed % skills.Count];
        }

        private void ApplyEnemySkillResult(int enemyIndex, BattleSkillUnit target, SkillResult result)
        {
            if (target != null)
            {
                playerStatuses.Clear();
                for (int i = 0; i < target.Statuses.Count; i++)
                {
                    playerStatuses.Add(target.Statuses[i]);
                }
            }

            SkillTargetResult playerResult = GetPlayerTargetResult(result);
            if (playerResult != null && playerResult.Damage > 0)
            {
                player.TakeTurnBasedDamage(playerResult.Damage, player.transform.position.x + 1f);
            }

            if (result.Skill.Id == BattleSkillId.BloodDrain && enemyIndex >= 0 && enemyIndex < enemies.Count && playerResult != null)
            {
                int healed = Mathf.Max(4, Mathf.CeilToInt(playerResult.Damage * 0.45f));
                enemies[enemyIndex].Health = Mathf.Clamp(enemies[enemyIndex].Health + healed, 0, enemies[enemyIndex].MaxHealth);
                battleHud.SetMessage(GameText.Battle.EnemyDrainLife(enemies[enemyIndex].Name, healed));
            }
        }

        private static SkillTargetResult GetPlayerTargetResult(SkillResult result)
        {
            if (result == null)
            {
                return null;
            }

            for (int i = 0; i < result.TargetResults.Count; i++)
            {
                if (result.TargetResults[i].IsPlayerTarget)
                {
                    return result.TargetResults[i];
                }
            }

            return null;
        }

        private PartyMember GetActiveFriendlyMember()
        {
            if (activeTurnUnit != null && activeTurnUnit.Member != null)
            {
                return activeTurnUnit.Member;
            }

            return PartyManager.CreateIfMissing().GetActiveMember(0);
        }

        private static bool IsHunter(PartyMember member)
        {
            return member == null || member.Name == GameText.HunterName;
        }

        private static bool IsSorceress(PartyMember member)
        {
            return member != null && (member.Name == GameText.TrissName || member.Name == GameText.YenneferName);
        }

        private static string GetFriendlyDisplayName(PartyMember member)
        {
            return IsHunter(member) ? GameText.HunterName : member.Name;
        }

        private SkillDefinition GetDefaultAttackSkill(PartyMember member)
        {
            if (IsHunter(member))
            {
                return WitcherSkillBook.CreateBasicAttack();
            }

            return member.Name == GameText.YenneferName ? WitcherSkillBook.CreateYenneferArcaneBolt() : WitcherSkillBook.CreateTrissFirebolt();
        }

        private SkillDefinition GetDefaultDefendSkill(PartyMember member)
        {
            if (IsHunter(member))
            {
                return WitcherSkillBook.CreateDefend();
            }

            return member.Name == GameText.YenneferName ? WitcherSkillBook.CreateYenneferAegis() : WitcherSkillBook.CreateTrissFlameWard();
        }

        private bool ActiveFriendlyHasMana(int cost)
        {
            PartyMember member = GetActiveFriendlyMember();
            if (IsHunter(member))
            {
                return player != null && player.CurrentMana >= Mathf.Max(0, cost);
            }

            return member.MP >= Mathf.Max(0, cost);
        }

        private bool TrySpendActiveFriendlyMana(PartyMember member, int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (IsHunter(member))
            {
                return player != null && player.TrySpendMana(cost);
            }

            if (member.MP < cost)
            {
                return false;
            }

            member.MP = Mathf.Max(0, member.MP - cost);
            return true;
        }

        public List<TurnBattleTimelineEntry> GetTimelinePreview(int count)
        {
            List<TurnBattleTimelineEntry> preview = new List<TurnBattleTimelineEntry>();
            if (!battleActive || count <= 0)
            {
                return preview;
            }

            if (activeTurnUnit != null && IsTurnUnitAlive(activeTurnUnit))
            {
                preview.Add(CreateTimelineEntry(activeTurnUnit, 1f));
            }

            List<TimelineSimulationUnit> simulation = CreateTimelineSimulation();
            for (int i = preview.Count; i < count; i++)
            {
                int readyIndex = FindReadySimulationUnit(simulation);
                if (readyIndex < 0)
                {
                    for (int step = 0; step < 64 && readyIndex < 0; step++)
                    {
                        for (int unitIndex = 0; unitIndex < simulation.Count; unitIndex++)
                        {
                            TimelineSimulationUnit unit = simulation[unitIndex];
                            unit.ActionValue += Mathf.Max(1, unit.Speed);
                            simulation[unitIndex] = unit;
                        }

                        readyIndex = FindReadySimulationUnit(simulation);
                    }
                }

                if (readyIndex < 0)
                {
                    break;
                }

                TimelineSimulationUnit readyUnit = simulation[readyIndex];
                preview.Add(CreateTimelineEntry(readyUnit.Source, i == 0 ? 1f : 0.68f));
                readyUnit.ActionValue -= TurnActionThreshold;
                simulation[readyIndex] = readyUnit;
            }

            return preview;
        }

        private List<TimelineSimulationUnit> CreateTimelineSimulation()
        {
            List<TimelineSimulationUnit> simulation = new List<TimelineSimulationUnit>();
            for (int i = 0; i < turnUnits.Count; i++)
            {
                BattleTurnUnit unit = turnUnits[i];
                if (!IsTurnUnitAlive(unit))
                {
                    continue;
                }

                simulation.Add(new TimelineSimulationUnit
                {
                    Source = unit,
                    Speed = unit.Speed,
                    ActionValue = unit.ActionValue
                });
            }

            return simulation;
        }

        private int FindReadySimulationUnit(List<TimelineSimulationUnit> simulation)
        {
            int readyIndex = -1;
            for (int i = 0; i < simulation.Count; i++)
            {
                TimelineSimulationUnit unit = simulation[i];
                if (unit.ActionValue < TurnActionThreshold)
                {
                    continue;
                }

                if (readyIndex < 0 || unit.ActionValue > simulation[readyIndex].ActionValue)
                {
                    readyIndex = i;
                }
            }

            return readyIndex;
        }

        private TurnBattleTimelineEntry CreateTimelineEntry(BattleTurnUnit unit, float readiness)
        {
            return new TurnBattleTimelineEntry
            {
                Name = GetTurnUnitName(unit),
                IsPlayer = unit != null && unit.IsPlayer,
                EnemyIndex = unit == null ? -1 : unit.EnemyIndex,
                PartyMemberName = unit?.Member == null ? string.Empty : unit.Member.Name,
                Readiness = Mathf.Clamp01(readiness)
            };
        }

        private string GetTurnUnitName(BattleTurnUnit unit)
        {
            if (unit == null)
            {
                return string.Empty;
            }

            if (unit.IsPlayer)
            {
                return GetFriendlyDisplayName(unit.Member);
            }

            return unit.EnemyIndex >= 0 && unit.EnemyIndex < enemies.Count ? enemies[unit.EnemyIndex].Name : GameText.MonsterFallbackName;
        }

        private void BuildTurnUnits()
        {
            turnUnits.Clear();
            IReadOnlyList<PartyMember> activeMembers = PartyManager.CreateIfMissing().ActiveParty;
            for (int i = 0; i < activeMembers.Count; i++)
            {
                PartyMember member = activeMembers[i];
                if (member == null || !member.IsJoined)
                {
                    continue;
                }

                turnUnits.Add(new BattleTurnUnit
                {
                    IsPlayer = true,
                    EnemyIndex = -1,
                    Member = member,
                    Speed = GetFriendlyTurnSpeed(member)
                });
            }

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

            return unit.IsPlayer ? player != null && player.IsAlive && unit.Member != null && unit.Member.IsJoined : IsLivingEnemyTurn(unit);
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
                case TurnBasedEnemyVisualKind.BlackNailThrall:
                    baseSpeed = 86;
                    break;
                case TurnBasedEnemyVisualKind.BlackWaxGateShade:
                    baseSpeed = 90;
                    break;
                case TurnBasedEnemyVisualKind.BlackWaxAcolyte:
                    baseSpeed = 82;
                    break;
                case TurnBasedEnemyVisualKind.BlackNailPuppet:
                    baseSpeed = 78;
                    break;
                default:
                    baseSpeed = 94;
                    break;
            }

            return Mathf.Max(56, baseSpeed - Mathf.Max(0, slotIndex) * 3);
        }

        private static int GetFriendlyTurnSpeed(PartyMember member)
        {
            if (member == null || member.Name == GameText.HunterName)
            {
                return PlayerTurnSpeed;
            }

            return Mathf.Clamp(member.TotalSpeed * 8, 72, 132);
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
