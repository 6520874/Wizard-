using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：驱动“猎魔委托 Roguelite”的短局循环：接委托、选伙伴、有限调查、战前准备、战斗和结局收集。
    public class WitcherContractRunManager : MonoBehaviour
    {
        private const string ManagerName = "Witcher Contract Run Manager";
        private const int BaseInvestigationLimit = 3;
        private const int MaxOptionCount = 8;
        private static readonly WeaknessKind[] WeaknessPool =
        {
            WeaknessKind.Silver,
            WeaknessKind.Fire,
            WeaknessKind.BlackBlood,
            WeaknessKind.RavenCharm
        };

        private static WitcherContractRunManager instance;

        [SerializeField] private GeraltController player;

        private readonly List<ContractDefinition> contracts = new List<ContractDefinition>();
        private readonly List<PartnerDefinition> partners = new List<PartnerDefinition>();
        private readonly List<LoadoutDefinition> loadouts = new List<LoadoutDefinition>();
        private readonly List<Text> optionTexts = new List<Text>();
        private readonly ReputationLedger ledger = new ReputationLedger();

        private EndingCollection endingCollection;
        private ContractRunState run;
        private ContractScreen screen;
        private GameObject root;
        private Text titleText;
        private Text bodyText;
        private Text footerText;

        public static bool HasOpenUi => instance != null && instance.IsOpen;
        public bool IsOpen => root != null && root.activeSelf;

        public static WitcherContractRunManager CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            WitcherContractRunManager existing = FindObjectOfType<WitcherContractRunManager>();
            if (existing != null)
            {
                instance = existing;
                existing.SetPlayer(target);
                return existing;
            }

            WitcherContractRunManager manager = new GameObject(ManagerName).AddComponent<WitcherContractRunManager>();
            manager.SetPlayer(target);
            return manager;
        }

        private void Awake()
        {
            instance = this;
            BuildContent();
            endingCollection = new EndingCollection(contracts);
            EnsureUi();
            root.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || DialogueManager.IsDialogueActive)
            {
                return;
            }

            switch (screen)
            {
                case ContractScreen.Board:
                    HandleBoardInput();
                    break;
                case ContractScreen.Partner:
                    HandlePartnerInput();
                    break;
                case ContractScreen.Investigation:
                    HandleInvestigationInput();
                    break;
                case ContractScreen.Loadout:
                    HandleLoadoutInput();
                    break;
                case ContractScreen.Judgement:
                    HandleJudgementInput();
                    break;
                case ContractScreen.Result:
                    HandleResultInput();
                    break;
            }
        }

        public void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
        }

        public void OpenBoard()
        {
            if (FindActiveBattle())
            {
                return;
            }

            BuildContent();
            endingCollection = endingCollection ?? new EndingCollection(contracts);
            ledger.Load();
            EnsureUi();
            root.SetActive(true);
            screen = ContractScreen.Board;
            run = null;
            ShowBoard();
            PlayerInputController.RefreshPlayerControl();
        }

        private void Close()
        {
            if (screen == ContractScreen.Battle)
            {
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.UiClick, 0.45f);
            screen = ContractScreen.Idle;
            run = null;
            if (root != null)
            {
                root.SetActive(false);
            }

            PlayerInputController.RefreshPlayerControl();
        }

        private void HandleBoardInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            int choice = ReadNumberKey(contracts.Count);
            if (choice >= 0)
            {
                StartContract(contracts[choice]);
            }
        }

        private void HandlePartnerInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowBoard();
                return;
            }

            int choice = ReadNumberKey(partners.Count);
            if (choice >= 0)
            {
                SelectPartner(partners[choice]);
            }
        }

        private void HandleInvestigationInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowBoard();
                return;
            }

            if (run == null)
            {
                ShowBoard();
                return;
            }

            if (run.InvestigationsUsed >= run.MaxInvestigations)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    ShowLoadout();
                }

                return;
            }

            int choice = ReadNumberKey(InvestigationCount);
            if (choice >= 0)
            {
                Investigate(choice);
            }
        }

        private void HandleLoadoutInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowInvestigation(string.Empty);
                return;
            }

            int choice = ReadNumberKey(loadouts.Count);
            if (choice >= 0)
            {
                SelectLoadout(loadouts[choice]);
            }
        }

        private void HandleJudgementInput()
        {
            int choice = ReadNumberKey(4);
            if (choice >= 0)
            {
                ResolveJudgement(choice);
            }
        }

        private void HandleResultInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ShowBoard();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void StartContract(ContractDefinition contract)
        {
            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.58f);
            run = new ContractRunState(contract);
            run.Culprit = Pick(contract.Culprits);
            run.Weakness = WeaknessPool[Random.Range(0, WeaknessPool.Length)];
            run.VillagerAttitude = Pick(contract.VillagerAttitudes);
            run.Weather = Pick(contract.Weather);
            run.Cost = Pick(contract.Costs);
            StartRunQuest("选择一名同行者。");
            ShowPartnerChoice();
        }

        private void SelectPartner(PartnerDefinition partner)
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.58f);
            run.Partner = partner;
            run.MaxInvestigations = partner.Kind == PartnerKind.Hound ? BaseInvestigationLimit + 1 : BaseInvestigationLimit;

            if (partner.Kind == PartnerKind.Smith)
            {
                ledger.Gold = Mathf.Max(0, ledger.Gold - 5);
                ledger.Save();
                run.AddClue("铁匠把五枚金币压进掌心，只留下一句：别让刀口卷了。");
            }
            else if (partner.Kind == PartnerKind.NightCrow && ledger.Conscience >= 3)
            {
                run.WeaknessKnown = true;
                run.AddClue("夜鸦女术士没有问报酬。她说怪物身上的裂口会回应" + GetWeaknessName(run.Weakness) + "。");
            }

            SetCurrentObjective($"调查线索（{run.InvestigationsUsed}/{run.MaxInvestigations}）。");
            ShowInvestigation(string.Empty);
        }

        private void Investigate(int index)
        {
            if (run == null || run.SelectedInvestigationIds.Contains(index) || run.InvestigationsUsed >= run.MaxInvestigations)
            {
                return;
            }

            run.SelectedInvestigationIds.Add(index);
            run.InvestigationsUsed++;
            WitcherSfxPlayer.Play(WitcherSfxCue.ClueFound, 0.65f);

            string clue = ResolveInvestigation(index);
            run.AddClue(clue);
            SetCurrentObjective(run.InvestigationsUsed >= run.MaxInvestigations
                ? "选择战前准备。"
                : $"调查线索（{run.InvestigationsUsed}/{run.MaxInvestigations}）。");
            ShowInvestigation(clue);
        }

        private string ResolveInvestigation(int index)
        {
            switch (index)
            {
                case 0:
                    run.CulpritKnown = true;
                    return $"尸体的伤口不像求救，倒像临死前抓住了{run.Culprit}留下的东西。";
                case 1:
                    run.AttitudeKnown = true;
                    if (ledger.Fear >= 3 && run.Partner.Kind != PartnerKind.NightCrow)
                    {
                        return $"村长把每句话都说得很慢。{run.VillagerAttitude}不在词里，在他不肯抬头的那一下。";
                    }

                    if (run.Partner.Kind == PartnerKind.NightCrow)
                    {
                        run.CulpritKnown = true;
                        return $"夜鸦女术士听完证词后笑了一下：村民在{run.VillagerAttitude}，真凶的名字绕回了{run.Culprit}。";
                    }

                    return $"村长说村民只想活到天亮，可门缝后的眼神更像{run.VillagerAttitude}。";
                case 2:
                    run.CostKnown = true;
                    if (run.Weakness == WeaknessKind.Fire || run.Weakness == WeaknessKind.RavenCharm)
                    {
                        run.WeaknessKnown = true;
                    }

                    return $"教堂蜡泪结成{GetWeaknessName(run.Weakness)}的形状，祭台下压着一张写着“{run.Cost}”的旧账。";
                case 3:
                    run.WeatherKnown = true;
                    if (run.Weakness == WeaknessKind.BlackBlood)
                    {
                        run.WeaknessKnown = true;
                    }

                    return $"井口的水没有倒影。今晚是{run.Weather}，黑血药剂在瓶底轻轻发热。";
                default:
                    run.WeaknessKnown = true;
                    return $"怪物脚印到河边突然变浅，像被{GetWeaknessName(run.Weakness)}烫过。它不是只会吃人的东西。";
            }
        }

        private void ShowBoard()
        {
            screen = ContractScreen.Board;
            root.SetActive(true);
            StringBuilder body = new StringBuilder();
            body.AppendLine("接委托不是读一条固定剧情。每次会抽出新的真凶、弱点、村民态度、天气和代价。");
            body.AppendLine("每局只给有限调查次数，线索不完整也要拔剑。");
            body.AppendLine();
            body.AppendLine(BuildLedgerLine());
            body.AppendLine($"结局图鉴：{endingCollection.GetTotalUnlocked()}/{contracts.Count * EndingCollection.EndingsPerContract}");

            List<string> options = new List<string>();
            for (int i = 0; i < contracts.Count; i++)
            {
                ContractDefinition contract = contracts[i];
                options.Add($"{contract.Title}  已解锁 {endingCollection.GetUnlockedCount(contract.Id)}/{EndingCollection.EndingsPerContract}");
            }

            SetUi("猎魔委托", body.ToString(), options, "数字键接委托；Esc 关闭");
            PlayerInputController.RefreshPlayerControl();
        }

        private void ShowPartnerChoice()
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            screen = ContractScreen.Partner;
            root.SetActive(true);
            StringBuilder body = new StringBuilder();
            body.AppendLine(run.Contract.Title);
            body.AppendLine(run.Contract.Briefing);
            body.AppendLine();
            body.AppendLine("同行者会改变调查或战斗，但也会把村子推向不同的表情。");
            body.AppendLine(BuildReputationPressure());

            List<string> options = new List<string>();
            foreach (PartnerDefinition partner in partners)
            {
                options.Add($"{partner.Name} - {partner.Description}");
            }

            SetUi("选择同行者", body.ToString(), options, "数字键选择；Esc 返回委托板");
        }

        private void ShowInvestigation(string newestClue)
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            screen = ContractScreen.Investigation;
            root.SetActive(true);
            StringBuilder body = new StringBuilder();
            body.AppendLine($"{run.Contract.Title}  调查 {run.InvestigationsUsed}/{run.MaxInvestigations}");
            body.AppendLine($"同行者：{run.Partner.Name}");
            body.AppendLine();
            if (!string.IsNullOrWhiteSpace(newestClue))
            {
                body.AppendLine("新线索：");
                body.AppendLine(newestClue);
                body.AppendLine();
            }

            body.Append(BuildKnownFacts());

            List<string> options = new List<string>();
            for (int i = 0; i < InvestigationCount; i++)
            {
                string state = run.SelectedInvestigationIds.Contains(i) ? "（已查）" : string.Empty;
                options.Add($"{GetInvestigationName(i)}{state}");
            }

            string footer = run.InvestigationsUsed >= run.MaxInvestigations
                ? "调查机会已用完；Enter 选择战前准备"
                : "数字键调查；调查次数有限";
            SetUi("有限调查", body.ToString(), options, footer);
        }

        private void ShowLoadout()
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            screen = ContractScreen.Loadout;
            root.SetActive(true);
            SetCurrentObjective("选择战前准备，然后进入战斗。");

            StringBuilder body = new StringBuilder();
            body.AppendLine("战斗会很短，但战前准备会兑现你调查到的东西。");
            body.AppendLine(run.WeaknessKnown
                ? $"你确信它怕{GetWeaknessName(run.Weakness)}。"
                : "你还没有确认弱点，只能带着猜测进雾里。");
            body.AppendLine();
            body.Append(BuildKnownFacts());

            List<string> options = new List<string>();
            foreach (LoadoutDefinition loadout in loadouts)
            {
                options.Add($"{loadout.Name} - {loadout.Description}");
            }

            SetUi("战前准备", body.ToString(), options, "数字键选择并开战；Esc 返回调查页");
        }

        private void SelectLoadout(LoadoutDefinition loadout)
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.BossSpawn, 0.72f);
            run.Loadout = loadout;
            root.SetActive(false);
            screen = ContractScreen.Battle;
            SetCurrentObjective("击败怪物，然后审判真相。");
            PlayerInputController.RefreshPlayerControl();
            StartCoroutine(BeginBattleNextFrame());
        }

        private IEnumerator BeginBattleNextFrame()
        {
            yield return null;

            if (run == null)
            {
                ShowBoard();
                yield break;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            TurnBasedBattleManager battleManager = TurnBasedBattleManager.CreateIfMissing(player);
            battleManager.BattleFinished -= HandleBattleFinished;
            battleManager.BattleFinished += HandleBattleFinished;

            BattleEncounterTrigger trigger = CreateContractEncounter();
            if (trigger == null || !battleManager.TryBeginBattle(trigger))
            {
                battleManager.BattleFinished -= HandleBattleFinished;
                if (trigger != null)
                {
                    Destroy(trigger.gameObject);
                }

                ShowFailedBattleStart();
            }
        }

        private BattleEncounterTrigger CreateContractEncounter()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null)
            {
                return null;
            }

            GameObject encounterObject = new GameObject("ContractEncounter_" + run.Contract.Id);
            encounterObject.transform.position = player.transform.position + new Vector3(1.4f, 0f, 0f);
            encounterObject.AddComponent<SpriteRenderer>();
            encounterObject.AddComponent<BoxCollider2D>();
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();

            if (run.Contract.VisualKind == TurnBasedEnemyVisualKind.BlackNailPuppet)
            {
                trigger.ConfigureContractBoss(null, run.Contract.WaveBonus, run.Contract.MonsterName, run.Contract.MonsterName, TurnBasedEnemyVisualKind.BlackNailPuppet);
            }
            else
            {
                trigger.ConfigureMonster(run.Contract.VisualKind, null, 1, run.Contract.WaveBonus);
                trigger.OverrideEncounterIdentity(run.Contract.MonsterName, run.Contract.MonsterName);
            }

            BattlePrepModifiers modifiers = BuildBattlePrepModifiers();
            trigger.ApplyInvestigationModifiers(
                modifiers.HealthPenalty,
                modifiers.AttackPenalty,
                modifiers.DefensePenalty,
                modifiers.ShieldAdjustment,
                new[] { GetWeaknessName(run.Weakness), "剑", "印", "药", "？" },
                new[] { run.WeaknessKnown, true, false, run.Weakness == WeaknessKind.BlackBlood && run.WeaknessKnown, false },
                modifiers.OpeningNote);
            return trigger;
        }

        private BattlePrepModifiers BuildBattlePrepModifiers()
        {
            BattlePrepModifiers modifiers = new BattlePrepModifiers
            {
                HealthPenalty = run.InvestigationsUsed * 2,
                AttackPenalty = run.CulpritKnown ? 1 : 0,
                DefensePenalty = run.WeaknessKnown ? 1 : 0,
                ShieldAdjustment = 0
            };

            bool matched = run.Loadout.Matches(run.Weakness);
            if (matched)
            {
                modifiers.HealthPenalty += 24;
                modifiers.AttackPenalty += 4;
                modifiers.DefensePenalty += 2;
                modifiers.ShieldAdjustment -= 2;
            }
            else if (run.Loadout.IsWeaknessPrep)
            {
                modifiers.HealthPenalty += run.WeaknessKnown ? 4 : 0;
                modifiers.ShieldAdjustment += 1;
            }
            else if (run.Loadout.IsDefense)
            {
                modifiers.AttackPenalty += 3;
                modifiers.ShieldAdjustment -= 1;
            }

            if (run.Partner.Kind == PartnerKind.Pyromancer && run.Loadout.Weakness == WeaknessKind.Fire)
            {
                modifiers.HealthPenalty += 8;
                if (run.AttitudeKnown && run.VillagerAttitude == "求救")
                {
                    modifiers.AttackPenalty += 1;
                }
            }
            else if (run.Partner.Kind == PartnerKind.Smith && run.Loadout.Weakness == WeaknessKind.Silver)
            {
                modifiers.HealthPenalty += 8;
            }
            else if (run.Partner.Kind == PartnerKind.NightCrow && run.CulpritKnown)
            {
                modifiers.DefensePenalty += 1;
            }
            else if (!run.Loadout.IsWeaknessPrep && !run.Loadout.IsDefense && run.CulpritKnown)
            {
                modifiers.AttackPenalty += 2;
                modifiers.DefensePenalty += 1;
            }

            if (ledger.Gold >= 60 && run.Loadout.IsWeaknessPrep)
            {
                modifiers.HealthPenalty += 6;
            }

            if (run.Weather == "暴雨" && run.Loadout.Weakness == WeaknessKind.Fire)
            {
                modifiers.HealthPenalty = Mathf.Max(0, modifiers.HealthPenalty - 6);
            }
            else if (run.Weather == "血月")
            {
                modifiers.AttackPenalty = Mathf.Max(0, modifiers.AttackPenalty - 2);
            }

            modifiers.OpeningNote = BuildBattleOpeningNote(matched);
            return modifiers;
        }

        private string BuildBattleOpeningNote(bool matched)
        {
            StringBuilder note = new StringBuilder();
            note.Append(run.WeatherKnown ? $"天气兑现：{run.Weather}压在屋顶上。 " : "你没有弄清今晚的天色，只能听见远处的水声。 ");
            note.Append(matched
                ? $"准备兑现：{run.Loadout.Name}正好咬住它怕的{GetWeaknessName(run.Weakness)}。"
                : $"准备偏了：你带着{run.Loadout.Name}进场，怪物没有退。");
            if (run.CulpritKnown)
            {
                note.Append($" 你记得线索指向{run.Culprit}，剑没有先急着落下。");
            }

            return note.ToString();
        }

        private void HandleBattleFinished(bool won)
        {
            TurnBasedBattleManager battleManager = FindObjectOfType<TurnBasedBattleManager>();
            if (battleManager != null)
            {
                battleManager.BattleFinished -= HandleBattleFinished;
            }

            if (run == null)
            {
                ShowBoard();
                return;
            }

            run.WonBattle = won;
            if (won)
            {
                ShowJudgement();
            }
            else
            {
                ShowDefeatResult();
            }
        }

        private void ShowJudgement()
        {
            screen = ContractScreen.Judgement;
            root.SetActive(true);
            SetCurrentObjective("审判真相。");

            StringBuilder body = new StringBuilder();
            body.AppendLine($"{run.Contract.MonsterName}倒下了，但委托还没结束。");
            body.AppendLine();
            body.Append(BuildKnownFacts());
            body.AppendLine("你可以让村子得到一个答案，也可以让答案继续待在黑水里。");

            List<string> options = new List<string>
            {
                "按委托斩杀，领取赏金",
                "揭穿真凶，把名字说出来",
                "收钱沉默，让账本替你说话",
                "放走怪物，换一条没人愿听的线索"
            };

            SetUi("审判真相", body.ToString(), options, "数字键选择结局");
            PlayerInputController.RefreshPlayerControl();
        }

        private void ResolveJudgement(int choiceIndex)
        {
            if (run == null)
            {
                ShowBoard();
                return;
            }

            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.68f);
            EndingResult result = BuildEndingResult(choiceIndex);
            ledger.Apply(result.GoldDelta, result.ConscienceDelta, result.FearDelta, result.FameDelta);
            ledger.Save();
            endingCollection.Unlock(run.Contract.Id, result.EndingId);
            SetCurrentObjective("委托结束。", true);
            ShowResult(result);
        }

        private EndingResult BuildEndingResult(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 1:
                    return new EndingResult(
                        "truth",
                        "真相结局",
                        BuildTruthEndingText(),
                        4,
                        run.CulpritKnown ? 2 : 0,
                        run.CulpritKnown ? 1 : 2,
                        run.CulpritKnown ? 2 : 0);
                case 2:
                    return new EndingResult(
                        "dark",
                        "黑暗结局",
                        $"钱袋落进掌心时，{run.Contract.Place}终于安静。没有人再提{run.Culprit}，也没有人敢问怪物为什么学会了人的敲门声。",
                        35,
                        -2,
                        1,
                        0);
                case 3:
                    return new EndingResult(
                        "hidden",
                        "隐藏结局",
                        $"你放走了{run.Contract.MonsterName}。它没有回头，只在泥里留下{run.Contract.HiddenClue}。第二天，村民把门槛洗了三遍。",
                        0,
                        1,
                        2,
                        -1);
                default:
                    int conscience = run.Culprit == "怪物" ? 1 : -1;
                    return new EndingResult(
                        "bounty",
                        "委托结局",
                        $"你按委托斩下怪物，把赏金放进袋里。{run.Contract.Place}亮起几盏灯，灯下的人没有互相看。",
                        25,
                        conscience,
                        -1,
                        1);
            }
        }

        private string BuildTruthEndingText()
        {
            if (!run.CulpritKnown)
            {
                return $"你把推断说出口，却缺了最硬的一枚钉子。{run.Contract.Place}有人点头，有人冷笑，真相只被掀起一角。";
            }

            return $"你说出{run.Culprit}的名字。{run.Contract.DeepTruth}没有立刻变成证词，只像一根刺留在每个人喉咙里。";
        }

        private void ShowDefeatResult()
        {
            screen = ContractScreen.Result;
            root.SetActive(true);
            SetCurrentObjective("委托失败。", true);

            StringBuilder body = new StringBuilder();
            body.AppendLine("你从夜色里退出来，伤口比答案更先抵达村口。");
            body.AppendLine($"{run.Contract.Title}还挂在委托板上，下面多了一道没人承认的抓痕。");
            body.AppendLine();
            body.AppendLine(BuildLedgerLine());

            SetUi("委托失败", body.ToString(), new List<string> { "返回委托板" }, "Enter 返回；Esc 关闭");
            PlayerInputController.RefreshPlayerControl();
        }

        private void ShowFailedBattleStart()
        {
            screen = ContractScreen.Result;
            root.SetActive(true);
            SetUi("无法开战", "没有找到可用的猎魔人角色，委托暂时中止。", new List<string> { "返回委托板" }, "Enter 返回；Esc 关闭");
            PlayerInputController.RefreshPlayerControl();
        }

        private void ShowResult(EndingResult result)
        {
            screen = ContractScreen.Result;
            root.SetActive(true);

            StringBuilder body = new StringBuilder();
            body.AppendLine(result.Title);
            body.AppendLine(result.Narrative);
            body.AppendLine();
            body.AppendLine($"变化：金币 {Signed(result.GoldDelta)}  良知 {Signed(result.ConscienceDelta)}  恐惧 {Signed(result.FearDelta)}  名声 {Signed(result.FameDelta)}");
            body.AppendLine(BuildLedgerLine());
            body.AppendLine($"《{run.Contract.Title}》已解锁：{endingCollection.GetUnlockedCount(run.Contract.Id)}/{EndingCollection.EndingsPerContract}");
            body.AppendLine($"总图鉴：{endingCollection.GetTotalUnlocked()}/{contracts.Count * EndingCollection.EndingsPerContract}");

            SetUi("结局收集", body.ToString(), new List<string> { "返回委托板" }, "Enter 返回；Esc 关闭");
            PlayerInputController.RefreshPlayerControl();
        }

        private void StartRunQuest(string objective)
        {
            if (run == null)
            {
                return;
            }

            QuestManager.QuestData quest = new QuestManager.QuestData
            {
                title = "委托：" + run.Contract.Title,
                description = "有限调查、战前准备、审判真相。不是所有答案都能一次拿全。"
            };
            quest.objectives.Add(new QuestManager.QuestObjective(objective));
            QuestManager.CreateIfMissing().StartQuest(quest);
        }

        private void SetCurrentObjective(string objective, bool completed = false)
        {
            QuestManager manager = QuestManager.CreateIfMissing();
            if (manager.ActiveQuest == null)
            {
                return;
            }

            manager.SetCurrentObjective(objective, completed);
        }

        private string BuildKnownFacts()
        {
            if (run == null)
            {
                return string.Empty;
            }

            StringBuilder facts = new StringBuilder();
            facts.AppendLine("已知：");
            facts.AppendLine(run.CulpritKnown ? $"- 真凶影子：{run.Culprit}" : "- 真凶影子：未知");
            facts.AppendLine(run.WeaknessKnown ? $"- 怪物弱点：{GetWeaknessName(run.Weakness)}" : "- 怪物弱点：未知");
            facts.AppendLine(run.AttitudeKnown ? $"- 村民态度：{run.VillagerAttitude}" : "- 村民态度：未知");
            facts.AppendLine(run.WeatherKnown ? $"- 夜晚天气：{run.Weather}" : "- 夜晚天气：未知");
            facts.AppendLine(run.CostKnown ? $"- 结局代价：{run.Cost}" : "- 结局代价：未知");
            if (run.Clues.Count > 0)
            {
                facts.AppendLine();
                facts.AppendLine("线索：");
                int start = Mathf.Max(0, run.Clues.Count - 4);
                for (int i = start; i < run.Clues.Count; i++)
                {
                    facts.AppendLine("- " + run.Clues[i]);
                }
            }

            return facts.ToString();
        }

        private string BuildLedgerLine()
        {
            return $"声望：金币 {ledger.Gold}  良知 {ledger.Conscience}  恐惧 {ledger.Fear}  名声 {ledger.Fame}";
        }

        private string BuildReputationPressure()
        {
            if (ledger.Fear >= 3)
            {
                return "恐惧已经压进村民舌头里，询问会更难。";
            }

            if (ledger.Conscience >= 3)
            {
                return "有人听说你曾经少收过钱，愿意在门后多留一盏灯。";
            }

            if (ledger.Gold >= 60)
            {
                return "钱足够买好油和新银，但钱不会替你判断谁在撒谎。";
            }

            return "现在还没人知道你会成为哪一种猎魔人。";
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = GothicUiFactory.EnsureCanvas("Contract Run Canvas", 180);
            root = new GameObject("Contract Run Root");
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GothicUiFactory.CreatePanel("Contract Dim", root.transform, new Vector2(1400f, 820f), Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(1, 2, 4, 205));
            GameObject panel = GothicUiFactory.CreatePanel("Contract Panel", root.transform, new Vector2(1020f, 620f), Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(6, 8, 12, 242));
            GothicUiFactory.AddOutline(panel, new Color32(126, 101, 54, 255), new Vector2(2f, -2f));
            GothicUiFactory.CreatePanel("Contract Rule", panel.transform, new Vector2(920f, 2f), new Vector2(50f, -72f), new Vector2(0f, 1f), new Color32(132, 22, 30, 230));

            titleText = GothicUiFactory.CreateText("Contract Title", panel.transform, string.Empty, 32, TextAnchor.UpperLeft, new Vector2(48f, -26f), new Vector2(920f, 44f), new Color32(246, 214, 142, 255));
            bodyText = GothicUiFactory.CreateText("Contract Body", panel.transform, string.Empty, 19, TextAnchor.UpperLeft, new Vector2(54f, -92f), new Vector2(910f, 258f), new Color32(220, 216, 199, 255));

            for (int i = 0; i < MaxOptionCount; i++)
            {
                Text option = GothicUiFactory.CreateText($"Contract Option {i + 1}", panel.transform, string.Empty, 20, TextAnchor.MiddleLeft, new Vector2(74f, -368f - i * 34f), new Vector2(860f, 30f), new Color32(232, 226, 205, 255));
                optionTexts.Add(option);
            }

            footerText = GothicUiFactory.CreateText("Contract Footer", panel.transform, string.Empty, 16, TextAnchor.LowerRight, new Vector2(54f, -580f), new Vector2(910f, 30f), new Color32(164, 154, 128, 255));
        }

        private void SetUi(string title, string body, List<string> options, string footer)
        {
            titleText.text = title;
            bodyText.text = body;
            footerText.text = footer;

            for (int i = 0; i < optionTexts.Count; i++)
            {
                if (i < options.Count)
                {
                    optionTexts[i].text = $"{i + 1}. {options[i]}";
                    optionTexts[i].color = new Color32(232, 226, 205, 255);
                }
                else
                {
                    optionTexts[i].text = string.Empty;
                }
            }
        }

        private void BuildContent()
        {
            if (contracts.Count > 0)
            {
                return;
            }

            contracts.Add(new ContractDefinition(
                "black_marsh_cry",
                "黑沼村的夜哭声",
                "黑沼村",
                "黑沼村连续三晚传出婴儿哭声。村民说水鬼回来了，井边却摆着新娘的红线。",
                "沼泽水鬼",
                "被献祭的新娘把自己的名字藏在水草里",
                "一枚刻着双姓的婚戒",
                TurnBasedEnemyVisualKind.BloodWraith,
                6));
            contracts.Add(new ContractDefinition(
                "silver_pine_beast",
                "银松路的兽痕",
                "银松驿路",
                "商队在银松路失踪，路边都是狼爪，车厢里却没有一滴马血。",
                "银松狼人",
                "被诅咒的猎人仍记得自己守过哪条路",
                "半张被火烧过的猎人契约",
                TurnBasedEnemyVisualKind.CorruptedWolf,
                7));
            contracts.Add(new ContractDefinition(
                "black_nail_forge",
                "铁匠铺的黑钉案",
                "灰鸦铁匠铺",
                "铁匠铺夜里自己打铁。第二天，砧板上多出一排还在渗血的黑钉。",
                "黑钉傀儡",
                "教会失败的实验没有埋进墓地，只换了一个名字",
                "一张没有教会印章的封钉配方",
                TurnBasedEnemyVisualKind.BlackNailPuppet,
                1));
            contracts.Add(new ContractDefinition(
                "crowbone_grave",
                "乌鸦坡的缝尸人",
                "乌鸦坡",
                "乱葬坡的乌鸦不再啄眼睛，只把骨头排成字。守墓人说，那些字在叫他的旧名。",
                "鸦骨缝尸",
                "被缝起来的不是尸体，是一群没人愿意认领的名字",
                "一截打结的黑羽缝线",
                TurnBasedEnemyVisualKind.CrowboneStitcher,
                4));

            partners.Add(new PartnerDefinition(PartnerKind.Pyromancer, "赤焰术师", "火焰准备更强，但容易让村民害怕"));
            partners.Add(new PartnerDefinition(PartnerKind.NightCrow, "夜鸦女术士", "能听出谎言，教会和胆小村民会更紧张"));
            partners.Add(new PartnerDefinition(PartnerKind.Hound, "老猎犬", "调查次数 +1，战斗没有直接帮助"));
            partners.Add(new PartnerDefinition(PartnerKind.Smith, "沉默铁匠", "银器准备更强，先收 5 金币"));

            loadouts.Add(new LoadoutDefinition("银剑油", "适合怕银的怪物", WeaknessKind.Silver, true, false));
            loadouts.Add(new LoadoutDefinition("火焰符文", "适合怕火的怪物", WeaknessKind.Fire, true, false));
            loadouts.Add(new LoadoutDefinition("黑血药剂", "适合会吸血或入体的怪物", WeaknessKind.BlackBlood, true, false));
            loadouts.Add(new LoadoutDefinition("鸦羽符咒", "适合被名字或誓言束缚的怪物", WeaknessKind.RavenCharm, true, false));
            loadouts.Add(new LoadoutDefinition("防御护符", "不知道弱点时更稳", WeaknessKind.Silver, false, true));
            loadouts.Add(new LoadoutDefinition("女巫伙伴", "让已发现的真相更容易压垮怪物", WeaknessKind.RavenCharm, false, false));
        }

        private static int ReadNumberKey(int max)
        {
            int clampedMax = Mathf.Clamp(max, 0, 9);
            for (int i = 0; i < clampedMax; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Pick(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return string.Empty;
            }

            return values[Random.Range(0, values.Length)];
        }

        private static bool FindActiveBattle()
        {
            TurnBasedBattleManager battleManager = FindObjectOfType<TurnBasedBattleManager>();
            return battleManager != null && battleManager.BattleActive;
        }

        private static string GetInvestigationName(int index)
        {
            switch (index)
            {
                case 0:
                    return "调查尸体";
                case 1:
                    return "询问村长";
                case 2:
                    return "去教堂";
                case 3:
                    return "查看井口";
                default:
                    return "检查怪物脚印";
            }
        }

        private static string GetWeaknessName(WeaknessKind weakness)
        {
            switch (weakness)
            {
                case WeaknessKind.Fire:
                    return "火焰";
                case WeaknessKind.BlackBlood:
                    return "黑血药剂";
                case WeaknessKind.RavenCharm:
                    return "鸦羽符咒";
                default:
                    return "银剑";
            }
        }

        private static string Signed(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private const int InvestigationCount = 5;

        private enum ContractScreen
        {
            Idle,
            Board,
            Partner,
            Investigation,
            Loadout,
            Battle,
            Judgement,
            Result
        }

        private enum WeaknessKind
        {
            Silver,
            Fire,
            BlackBlood,
            RavenCharm
        }

        private enum PartnerKind
        {
            Pyromancer,
            NightCrow,
            Hound,
            Smith
        }

        private class ContractDefinition
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Place;
            public readonly string Briefing;
            public readonly string MonsterName;
            public readonly string DeepTruth;
            public readonly string HiddenClue;
            public readonly TurnBasedEnemyVisualKind VisualKind;
            public readonly int WaveBonus;
            public readonly string[] Culprits = { "村长", "女巫", "怪物", "被害人自己" };
            public readonly string[] VillagerAttitudes = { "隐瞒", "求救", "欺骗", "出卖你" };
            public readonly string[] Weather = { "大雾", "暴雨", "血月", "无月夜" };
            public readonly string[] Costs = { "救孩子", "保村子", "拿赏金", "放走怪物" };

            public ContractDefinition(string id, string title, string place, string briefing, string monsterName, string deepTruth, string hiddenClue, TurnBasedEnemyVisualKind visualKind, int waveBonus)
            {
                Id = id;
                Title = title;
                Place = place;
                Briefing = briefing;
                MonsterName = monsterName;
                DeepTruth = deepTruth;
                HiddenClue = hiddenClue;
                VisualKind = visualKind;
                WaveBonus = waveBonus;
            }
        }

        private class PartnerDefinition
        {
            public readonly PartnerKind Kind;
            public readonly string Name;
            public readonly string Description;

            public PartnerDefinition(PartnerKind kind, string name, string description)
            {
                Kind = kind;
                Name = name;
                Description = description;
            }
        }

        private class LoadoutDefinition
        {
            public readonly string Name;
            public readonly string Description;
            public readonly WeaknessKind Weakness;
            public readonly bool IsWeaknessPrep;
            public readonly bool IsDefense;

            public LoadoutDefinition(string name, string description, WeaknessKind weakness, bool isWeaknessPrep, bool isDefense)
            {
                Name = name;
                Description = description;
                Weakness = weakness;
                IsWeaknessPrep = isWeaknessPrep;
                IsDefense = isDefense;
            }

            public bool Matches(WeaknessKind weakness)
            {
                return IsWeaknessPrep && Weakness == weakness;
            }
        }

        private class ContractRunState
        {
            public readonly ContractDefinition Contract;
            public readonly List<string> Clues = new List<string>();
            public readonly HashSet<int> SelectedInvestigationIds = new HashSet<int>();
            public PartnerDefinition Partner;
            public LoadoutDefinition Loadout;
            public string Culprit;
            public WeaknessKind Weakness;
            public string VillagerAttitude;
            public string Weather;
            public string Cost;
            public int InvestigationsUsed;
            public int MaxInvestigations = BaseInvestigationLimit;
            public bool CulpritKnown;
            public bool WeaknessKnown;
            public bool AttitudeKnown;
            public bool WeatherKnown;
            public bool CostKnown;
            public bool WonBattle;

            public ContractRunState(ContractDefinition contract)
            {
                Contract = contract;
            }

            public void AddClue(string clue)
            {
                if (!string.IsNullOrWhiteSpace(clue))
                {
                    Clues.Add(clue);
                }
            }
        }

        private struct BattlePrepModifiers
        {
            public int HealthPenalty;
            public int AttackPenalty;
            public int DefensePenalty;
            public int ShieldAdjustment;
            public string OpeningNote;
        }

        private class EndingResult
        {
            public readonly string EndingId;
            public readonly string Title;
            public readonly string Narrative;
            public readonly int GoldDelta;
            public readonly int ConscienceDelta;
            public readonly int FearDelta;
            public readonly int FameDelta;

            public EndingResult(string endingId, string title, string narrative, int goldDelta, int conscienceDelta, int fearDelta, int fameDelta)
            {
                EndingId = endingId;
                Title = title;
                Narrative = narrative;
                GoldDelta = goldDelta;
                ConscienceDelta = conscienceDelta;
                FearDelta = fearDelta;
                FameDelta = fameDelta;
            }
        }

        private class ReputationLedger
        {
            private const string Prefix = "witcher_contract_ledger_";

            public int Gold;
            public int Conscience;
            public int Fear;
            public int Fame;

            public void Load()
            {
                Gold = PlayerPrefs.GetInt(Prefix + "gold", 20);
                Conscience = PlayerPrefs.GetInt(Prefix + "conscience", 0);
                Fear = PlayerPrefs.GetInt(Prefix + "fear", 0);
                Fame = PlayerPrefs.GetInt(Prefix + "fame", 0);
            }

            public void Apply(int gold, int conscience, int fear, int fame)
            {
                Gold = Mathf.Max(0, Gold + gold);
                Conscience += conscience;
                Fear = Mathf.Max(0, Fear + fear);
                Fame += fame;
            }

            public void Save()
            {
                PlayerPrefs.SetInt(Prefix + "gold", Gold);
                PlayerPrefs.SetInt(Prefix + "conscience", Conscience);
                PlayerPrefs.SetInt(Prefix + "fear", Fear);
                PlayerPrefs.SetInt(Prefix + "fame", Fame);
                PlayerPrefs.Save();
            }
        }

        private class EndingCollection
        {
            public const int EndingsPerContract = 4;
            private const string Prefix = "witcher_contract_ending_";
            private static readonly string[] EndingIds = { "bounty", "truth", "dark", "hidden" };
            private readonly List<ContractDefinition> knownContracts;

            public EndingCollection(List<ContractDefinition> contracts)
            {
                knownContracts = contracts;
            }

            public void Unlock(string contractId, string endingId)
            {
                PlayerPrefs.SetInt(Key(contractId, endingId), 1);
                PlayerPrefs.Save();
            }

            public int GetUnlockedCount(string contractId)
            {
                int count = 0;
                foreach (string endingId in EndingIds)
                {
                    if (PlayerPrefs.GetInt(Key(contractId, endingId), 0) == 1)
                    {
                        count++;
                    }
                }

                return count;
            }

            public int GetTotalUnlocked()
            {
                int count = 0;
                foreach (ContractDefinition contract in knownContracts)
                {
                    count += GetUnlockedCount(contract.Id);
                }

                return count;
            }

            private static string Key(string contractId, string endingId)
            {
                return Prefix + contractId + "_" + endingId;
            }
        }
    }
}
