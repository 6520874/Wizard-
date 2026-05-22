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
        [SerializeField] private int flameManaCost = 18;
        [SerializeField] private int flameBaseDamage = 26;
        [SerializeField] private int potionHealAmount = 32;
        [SerializeField] private int startingPotionCount = 3;
        [SerializeField, Range(0f, 1f)] private float escapeChance = 0.62f;

        private const float TurnActionThreshold = 100f;
        private const int PlayerTurnSpeed = 116;

        private static TurnBasedBattleManager instance;

        private readonly List<TurnBasedEnemyState> enemies = new List<TurnBasedEnemyState>();
        private readonly List<BattleTurnUnit> turnUnits = new List<BattleTurnUnit>();
        private readonly List<CameraRenderState> pausedRenderCameras = new List<CameraRenderState>();
        private GeraltController player;
        private GeraltAnimator playerAnimator;
        private TurnBasedBattleHud battleHud;
        private BattleEncounterTrigger currentEncounter;
        private BattleTurnUnit activeTurnUnit;
        private bool battleActive;
        private bool resolvingTurn;
        private bool defending;
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
            defending = false;
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

            if (!playerTurnConsumed)
            {
                resolvingTurn = false;
                battleHud.SetCommandsEnabled(true);
                yield break;
            }

            yield return DispatchNextTurn(0.18f);
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
            yield return battleHud.PlayPlayerAttack(targetIndex);
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
            yield return battleHud.PlayPlayerCast();
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
            int damage = defending ? Mathf.Max(1, Mathf.CeilToInt(rawDamage * 0.45f)) : rawDamage;
            battleHud.SetMessage($"{enemy.Name} 抢到先机，造成 {damage} 点伤害！");
            yield return battleHud.PlayEnemyAttack(enemyIndex);
            player.TakeTurnBasedDamage(damage, player.transform.position.x + 1f);
            yield return battleHud.PlayPlayerHurt(damage);
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
            defending = false;
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
