using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：驱动“一晚一个猎魔委托”的调查、线索、真假判断、遭遇战和结算流程。
    public class NightContractManager : MonoBehaviour
    {
        private const string ManagerName = "Night Contract Manager";
        private const string ObjectiveInvestigateWell = "调查老井旁的黑血，确认怪物弱点";
        private const string ObjectiveInvestigateMill = "调查老磨坊的爪痕，削弱护盾";
        private const string ObjectiveInvestigateWidow = "询问寡妇家的假供词，判断真相";
        private const string ObjectiveClearAmbush = "击败被哭声吸引来的怪物";
        private const string ObjectiveTruthChoice = "判断哭声真正来源";
        private const string ObjectiveDefeatBoss = "击败井底哭魂";
        private const string ObjectiveReturnVillage = "回村结算，等待第二晚异变";
        private const string ObjectiveSecondInspectCorpse = "检查村口铁匠围裙尸体";
        private const string ObjectiveSecondInspectHammer = "调查铁匠铺染黑的锤子";
        private const string ObjectiveSecondClearAmbush = "击败被黑血引来的怪物";
        private const string ObjectiveSecondInspectChapel = "调查教堂门口的黑蜡";
        private const string ObjectiveSecondTruthChoice = "判断第二具尸体为何会行走";
        private const string ObjectiveSecondDefeatBoss = "击败黑钉傀儡";
        private const string ObjectiveSecondReturnVillage = "回村结算，记录第三晚异变";

        private static NightContractManager instance;

        [Header("First Night Nodes")]
        [SerializeField] private Vector2 oldWellPosition = new Vector2(-1.08f, -0.82f);
        [SerializeField] private Vector2 oldMillPosition = new Vector2(-5.75f, -2.02f);
        [SerializeField] private Vector2 widowHousePosition = new Vector2(4.25f, -1.08f);
        [SerializeField] private Vector2 firstAmbushPosition = new Vector2(-3.65f, -1.82f);
        [SerializeField] private Vector2 secondAmbushPosition = new Vector2(3.25f, -2.34f);
        [SerializeField] private Vector2 bossPosition = new Vector2(7.45f, -0.92f);
        [Header("Second Night Nodes")]
        [SerializeField] private Vector2 forgeCorpsePosition = new Vector2(1.25f, -2.18f);
        [SerializeField] private Vector2 blackenedHammerPosition = new Vector2(6.75f, -1.42f);
        [SerializeField] private Vector2 chapelWaxPosition = new Vector2(10.4f, -0.92f);
        [SerializeField] private Vector2 secondNightFirstAmbushPosition = new Vector2(3.55f, -1.9f);
        [SerializeField] private Vector2 secondNightSecondAmbushPosition = new Vector2(9.25f, -1.55f);
        [SerializeField] private Vector2 secondNightBossPosition = new Vector2(12.85f, -1.18f);
        [SerializeField] private float nodeMarkerScale = 0.38f;
        [SerializeField] private float contractMonsterScale = 0.28f;
        [SerializeField] private float contractBossScale = 0.34f;

        private readonly Dictionary<NightInvestigationNodeId, NightInvestigationNode> nodes = new Dictionary<NightInvestigationNodeId, NightInvestigationNode>();
        private readonly List<GameObject> spawnedContractObjects = new List<GameObject>();
        private GeraltController player;
        private QuestManager questManager;
        private DialogueManager dialogueManager;
        private bool contractStarted;
        private bool foundBlackBlood;
        private bool foundClawMarks;
        private bool foundFalseTestimony;
        private bool truthChoiceResolved;
        private bool truthCorrect;
        private bool bossSpawned;
        private bool settlementShown;
        private int clearedAmbushCount;
        private bool secondNightStarted;
        private bool foundIronCorpse;
        private bool foundBlackenedHammer;
        private bool foundChapelWax;
        private bool secondTruthChoiceResolved;
        private bool secondTruthCorrect;
        private bool secondBossSpawned;
        private bool secondSettlementShown;
        private int secondClearedAmbushCount;
        private GameObject choicePanel;
        private Text choiceSummaryText;
        private Text choiceAText;
        private Text choiceBText;
        private Text choiceCText;

        public static NightContractManager CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            NightContractManager existing = FindObjectOfType<NightContractManager>();
            if (existing != null)
            {
                instance = existing;
                instance.SetPlayer(target);
                return existing;
            }

            NightContractManager manager = new GameObject(ManagerName).AddComponent<NightContractManager>();
            manager.SetPlayer(target);
            return manager;
        }

        public void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
        }

        public bool TryGetNavigationTarget(string objectiveText, out Vector2 targetPosition)
        {
            switch (objectiveText)
            {
                case ObjectiveInvestigateWell:
                    targetPosition = oldWellPosition;
                    return true;
                case ObjectiveInvestigateMill:
                    targetPosition = oldMillPosition;
                    return true;
                case ObjectiveInvestigateWidow:
                case ObjectiveTruthChoice:
                    targetPosition = widowHousePosition;
                    return true;
                case ObjectiveClearAmbush:
                    targetPosition = foundClawMarks ? secondAmbushPosition : firstAmbushPosition;
                    return true;
                case ObjectiveDefeatBoss:
                    targetPosition = bossPosition;
                    return true;
                case ObjectiveReturnVillage:
                    targetPosition = Vector2.zero;
                    return true;
                case ObjectiveSecondInspectCorpse:
                    targetPosition = forgeCorpsePosition;
                    return true;
                case ObjectiveSecondInspectHammer:
                    targetPosition = blackenedHammerPosition;
                    return true;
                case ObjectiveSecondInspectChapel:
                case ObjectiveSecondTruthChoice:
                    targetPosition = chapelWaxPosition;
                    return true;
                case ObjectiveSecondClearAmbush:
                    targetPosition = foundChapelWax ? secondNightSecondAmbushPosition : secondNightFirstAmbushPosition;
                    return true;
                case ObjectiveSecondDefeatBoss:
                    targetPosition = secondNightBossPosition;
                    return true;
                case ObjectiveSecondReturnVillage:
                    targetPosition = Vector2.zero;
                    return true;
                default:
                    targetPosition = default;
                    return false;
            }
        }

        public void BeginFirstNightContract()
        {
            if (contractStarted)
            {
                return;
            }

            contractStarted = true;
            questManager = QuestManager.CreateIfMissing();
            dialogueManager = DialogueManager.CreateIfMissing();
            StartFirstNightQuest();
            CreateInvestigationNodes();
            StartCoroutine(ShowContractBriefingWhenReady());
        }

        public void BeginSecondNightContract()
        {
            if (secondNightStarted)
            {
                return;
            }

            secondNightStarted = true;
            questManager = QuestManager.CreateIfMissing();
            dialogueManager = DialogueManager.CreateIfMissing();
            StartSecondNightQuest();
            CreateSecondNightNodes();
            StartCoroutine(ShowSecondNightBriefingWhenReady());
        }

        public void InteractWithNode(NightInvestigationNodeId nodeId)
        {
            if (!contractStarted || DialogueManager.IsDialogueActive)
            {
                return;
            }

            if (ShouldBlockOutOfOrderNode(nodeId))
            {
                dialogueManager = dialogueManager == null ? DialogueManager.CreateIfMissing() : dialogueManager;
                dialogueManager.StartDialogue(new[]
                {
                    new DialogueLine("猎魔人", "先按当前委托目标来。线索如果乱了，真相也会乱。")
                });
                return;
            }

            switch (nodeId)
            {
                case NightInvestigationNodeId.OldWell:
                    InvestigateOldWell();
                    break;
                case NightInvestigationNodeId.OldMill:
                    InvestigateOldMill();
                    break;
                case NightInvestigationNodeId.WidowHouse:
                    InvestigateWidowHouse();
                    break;
                case NightInvestigationNodeId.ForgeCorpse:
                    InvestigateForgeCorpse();
                    break;
                case NightInvestigationNodeId.BlackenedHammer:
                    InvestigateBlackenedHammer();
                    break;
                case NightInvestigationNodeId.ChapelWax:
                    InvestigateChapelWax();
                    break;
            }
        }

        public bool ShouldAutoTriggerNode(NightInvestigationNodeId nodeId)
        {
            return contractStarted
                && !DialogueManager.IsDialogueActive
                && !ShouldBlockOutOfOrderNode(nodeId);
        }

        private bool ShouldBlockOutOfOrderNode(NightInvestigationNodeId nodeId)
        {
            if (secondNightStarted)
            {
                return ShouldBlockSecondNightNode(nodeId);
            }

            if (!foundBlackBlood)
            {
                return nodeId != NightInvestigationNodeId.OldWell;
            }

            if (clearedAmbushCount < 1)
            {
                return true;
            }

            if (!foundClawMarks)
            {
                return nodeId != NightInvestigationNodeId.OldMill;
            }

            if (clearedAmbushCount < 2)
            {
                return true;
            }

            if (!foundFalseTestimony)
            {
                return nodeId != NightInvestigationNodeId.WidowHouse;
            }

            return true;
        }

        private bool ShouldBlockSecondNightNode(NightInvestigationNodeId nodeId)
        {
            if (!foundIronCorpse)
            {
                return nodeId != NightInvestigationNodeId.ForgeCorpse;
            }

            if (!foundBlackenedHammer)
            {
                return nodeId != NightInvestigationNodeId.BlackenedHammer;
            }

            if (secondClearedAmbushCount < 1)
            {
                return true;
            }

            if (!foundChapelWax)
            {
                return nodeId != NightInvestigationNodeId.ChapelWax;
            }

            if (secondClearedAmbushCount < 2)
            {
                return true;
            }

            return true;
        }

        public void NotifyEncounterCleared(NightContractEncounterRole role)
        {
            if (!contractStarted)
            {
                return;
            }

            if (secondNightStarted)
            {
                NotifySecondNightEncounterCleared(role);
                return;
            }

            if (role == NightContractEncounterRole.ClueAmbush)
            {
                clearedAmbushCount++;
                if (!foundClawMarks)
                {
                    SetCurrentQuestObjective(ObjectiveInvestigateMill);
                }
                else if (!foundFalseTestimony)
                {
                    SetCurrentQuestObjective(ObjectiveInvestigateWidow);
                }
                else if (!truthChoiceResolved)
                {
                    SetCurrentQuestObjective(ObjectiveTruthChoice);
                }

                return;
            }

            if (role == NightContractEncounterRole.Boss)
            {
                SetCurrentQuestObjective(ObjectiveReturnVillage, true);
                if (!settlementShown)
                {
                    settlementShown = true;
                    StartCoroutine(ShowSettlementWhenReady());
                }
            }
        }

        private void NotifySecondNightEncounterCleared(NightContractEncounterRole role)
        {
            if (role == NightContractEncounterRole.ClueAmbush)
            {
                secondClearedAmbushCount++;
                if (!foundChapelWax)
                {
                    SetCurrentQuestObjective(ObjectiveSecondInspectChapel);
                }
                else if (!secondTruthChoiceResolved)
                {
                    SetCurrentQuestObjective(ObjectiveSecondTruthChoice);
                    StartCoroutine(ShowSecondTruthChoiceWhenReady());
                }

                return;
            }

            if (role == NightContractEncounterRole.Boss)
            {
                SetCurrentQuestObjective(ObjectiveSecondReturnVillage, true);
                if (!secondSettlementShown)
                {
                    secondSettlementShown = true;
                    StartCoroutine(ShowSecondNightSettlementWhenReady());
                }
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            if (choicePanel == null || !choicePanel.activeSelf)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.A))
            {
                ResolveActiveTruthChoice(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.B))
            {
                ResolveActiveTruthChoice(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.C))
            {
                ResolveActiveTruthChoice(2);
            }
        }

        private void ResolveActiveTruthChoice(int choiceIndex)
        {
            if (secondNightStarted)
            {
                ResolveSecondTruthChoice(choiceIndex);
                return;
            }

            ResolveTruthChoice(choiceIndex);
        }

        private void StartFirstNightQuest()
        {
            QuestManager.QuestData quest = new QuestManager.QuestData
            {
                title = "第一晚：老井哭声案",
                description = "调查灰鸦村夜里传出的女人哭声。每条线索都会削弱最终怪物：找出真相，比直接拔剑更重要。",
                objectives = new List<QuestManager.QuestObjective>
                {
                    new QuestManager.QuestObjective(ObjectiveInvestigateWell)
                }
            };

            questManager.StartQuest(quest);
        }

        private void StartSecondNightQuest()
        {
            QuestManager.QuestData quest = new QuestManager.QuestData
            {
                title = "第二晚：铁匠黑血案",
                description = "第一晚之后，村口出现了穿着铁匠围裙的尸体。调查黑血、铁锤和教堂蜡泪，找出是谁让尸体重新站了起来。",
                objectives = new List<QuestManager.QuestObjective>
                {
                    new QuestManager.QuestObjective(ObjectiveSecondInspectCorpse)
                }
            };

            questManager.StartQuest(quest);
        }

        private IEnumerator ShowContractBriefingWhenReady()
        {
            yield return new WaitForSeconds(0.2f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            DialogueLine[] briefing =
            {
                new DialogueLine("委托", "第一晚：老井哭声案。村口尸体还没凉，井里却传来女人哭声。"),
                new DialogueLine("猎魔人", "先查线索。黑血、爪痕、假供词……它们会告诉我该用什么杀死它。")
            };
            dialogueManager.StartDialogue(briefing);
        }

        private IEnumerator ShowSecondNightBriefingWhenReady()
        {
            yield return new WaitForSeconds(0.35f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            DialogueLine[] briefing =
            {
                new DialogueLine("委托", "第二晚：铁匠黑血案。天亮前，村口多了一具穿着铁匠围裙的尸体。"),
                new DialogueLine("猎魔人", "铁匠未必死了。先看尸体、锤子和教堂。死人不会自己换衣服，除非有人想让它看起来像铁匠。")
            };
            dialogueManager.StartDialogue(briefing);
        }

        private void CreateInvestigationNodes()
        {
            CreateNode(NightInvestigationNodeId.OldWell, "老井", oldWellPosition, new Color32(68, 118, 132, 230));
            CreateNode(NightInvestigationNodeId.OldMill, "老磨坊", oldMillPosition, new Color32(139, 101, 48, 230));
            CreateNode(NightInvestigationNodeId.WidowHouse, "寡妇家", widowHousePosition, new Color32(114, 72, 118, 230));
        }

        private void CreateSecondNightNodes()
        {
            CreateNode(NightInvestigationNodeId.ForgeCorpse, "围裙尸体", forgeCorpsePosition, new Color32(126, 55, 50, 230));
            CreateNode(NightInvestigationNodeId.BlackenedHammer, "黑血铁锤", blackenedHammerPosition, new Color32(116, 97, 72, 230));
            CreateNode(NightInvestigationNodeId.ChapelWax, "教堂黑蜡", chapelWaxPosition, new Color32(80, 70, 116, 230));
        }

        private void CreateNode(NightInvestigationNodeId id, string displayName, Vector2 position, Color32 color)
        {
            if (nodes.ContainsKey(id))
            {
                return;
            }

            GameObject nodeObject = new GameObject($"NightContractNode_{displayName}");
            nodeObject.transform.position = new Vector3(position.x, position.y, -0.1f);

            SpriteRenderer marker = nodeObject.AddComponent<SpriteRenderer>();
            marker.sprite = CreateDiamondSprite(color);
            marker.sortingOrder = 35;
            nodeObject.transform.localScale = Vector3.one * nodeMarkerScale;

            BoxCollider2D collider = nodeObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.3f, 1.3f);

            NightInvestigationNode node = nodeObject.AddComponent<NightInvestigationNode>();
            node.Configure(this, id, displayName);
            CreateNodeLabel(displayName, nodeObject.transform);
            nodes[id] = node;
            spawnedContractObjects.Add(nodeObject);
        }

        private void CreateNodeLabel(string displayName, Transform parent)
        {
            GameObject labelObject = new GameObject($"{displayName}_Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.42f, 0f);
            labelObject.transform.localScale = Vector3.one / Mathf.Max(0.01f, nodeMarkerScale);

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = $"{displayName}\n点击调查";
            label.fontSize = 38;
            label.characterSize = 0.035f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color32(218, 201, 148, 230);

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 36;
        }

        private void InvestigateOldWell()
        {
            if (foundBlackBlood)
            {
                return;
            }

            foundBlackBlood = true;
            CompleteNode(NightInvestigationNodeId.OldWell);
            SetCurrentQuestObjective(ObjectiveClearAmbush);
            DialogueLine[] lines =
            {
                new DialogueLine("老井", "井沿凝着黑色血迹，银粉碰上去时发出轻微嘶鸣。"),
                new DialogueLine("猎魔人", "不是普通亡魂。银剑能伤它。")
            };
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(firstAmbushPosition, TurnBasedEnemyVisualKind.CorruptedWolf, 1, "井边腐兽"));
        }

        private void InvestigateOldMill()
        {
            if (foundClawMarks)
            {
                return;
            }

            foundClawMarks = true;
            CompleteNode(NightInvestigationNodeId.OldMill);
            SetCurrentQuestObjective(ObjectiveClearAmbush);
            DialogueLine[] lines =
            {
                new DialogueLine("老磨坊", "木门上有反复抓挠的沟痕，却没有从外面破门的痕迹。"),
                new DialogueLine("猎魔人", "它被困过。它的护盾不会完整。")
            };
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(secondAmbushPosition, TurnBasedEnemyVisualKind.BloodWraith, 1, "磨坊哭影"));
        }

        private void InvestigateWidowHouse()
        {
            if (foundFalseTestimony)
            {
                return;
            }

            foundFalseTestimony = true;
            CompleteNode(NightInvestigationNodeId.WidowHouse);
            SetCurrentQuestObjective(ObjectiveTruthChoice);
            DialogueLine[] lines =
            {
                new DialogueLine("寡妇", "我听见哭声从井底来……不，从磨坊来。别问了，猎魔人。"),
                new DialogueLine("猎魔人", "她在撒谎。哭声不是引诱，是警告。有人还想继续献祭。")
            };
            dialogueManager.StartDialogue(lines, ShowTruthChoice);
        }

        private void InvestigateForgeCorpse()
        {
            if (foundIronCorpse)
            {
                return;
            }

            foundIronCorpse = true;
            CompleteNode(NightInvestigationNodeId.ForgeCorpse);
            SetCurrentQuestObjective(ObjectiveSecondInspectHammer);
            DialogueLine[] lines =
            {
                new DialogueLine("围裙尸体", "尸体穿着铁匠的围裙，手指却细得像从未握过锤。脖颈后钉着一枚发黑银钉。"),
                new DialogueLine("猎魔人", "这不是铁匠。有人把死者装成他，想把我的剑引向错的人。")
            };
            dialogueManager.StartDialogue(lines);
        }

        private void InvestigateBlackenedHammer()
        {
            if (foundBlackenedHammer)
            {
                return;
            }

            foundBlackenedHammer = true;
            CompleteNode(NightInvestigationNodeId.BlackenedHammer);
            SetCurrentQuestObjective(ObjectiveSecondClearAmbush);
            DialogueLine[] lines =
            {
                new DialogueLine("黑血铁锤", "铁锤缝隙里有黑血，火星碰到血迹时炸出一圈蓝白色寒光。"),
                new DialogueLine("猎魔人", "黑血怕火，也怕银。今晚的东西不是肉身，是被钉住的怨念。")
            };
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(secondNightFirstAmbushPosition, TurnBasedEnemyVisualKind.CorruptedWolf, 2, "黑血腐狼"));
        }

        private void InvestigateChapelWax()
        {
            if (foundChapelWax)
            {
                return;
            }

            foundChapelWax = true;
            CompleteNode(NightInvestigationNodeId.ChapelWax);
            SetCurrentQuestObjective(ObjectiveSecondClearAmbush);
            DialogueLine[] lines =
            {
                new DialogueLine("教堂黑蜡", "教堂门口的蜡泪混着铁屑，凝成倒置的祷文。祷文最后一行写着：死者替活人赎罪。"),
                new DialogueLine("猎魔人", "神父在用银钉操控尸体。找到蜡源，傀儡的护盾会裂开。")
            };
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(secondNightSecondAmbushPosition, TurnBasedEnemyVisualKind.BloodWraith, 1, "黑蜡哭影"));
        }

        private void CompleteNode(NightInvestigationNodeId id)
        {
            if (nodes.TryGetValue(id, out NightInvestigationNode node))
            {
                node.SetCompleted(true);
            }
        }

        private void SetCurrentQuestObjective(string objectiveText, bool completed = false)
        {
            questManager = questManager == null ? QuestManager.CreateIfMissing() : questManager;
            questManager.SetCurrentObjective(objectiveText, completed);
        }

        private void ShowTruthChoice()
        {
            if (truthChoiceResolved)
            {
                return;
            }

            EnsureChoiceUi();
            choiceSummaryText.text = "哭声真正来源是？\nA 女鬼复仇   B 水鬼诱捕   C 村民伪装献祭";
            SetChoiceLabels("A 女鬼", "B 水鬼", "C 献祭");
            choicePanel.SetActive(true);
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
        }

        private IEnumerator ShowSecondTruthChoiceWhenReady()
        {
            yield return new WaitForSeconds(0.25f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            ShowSecondTruthChoice();
        }

        private void ShowSecondTruthChoice()
        {
            if (secondTruthChoiceResolved)
            {
                return;
            }

            EnsureChoiceUi();
            choiceSummaryText.text = "第二具尸体为何会行走？\nA 铁匠杀人   B 神父驱魔失败   C 银钉操控尸体";
            SetChoiceLabels("A 铁匠", "B 神父", "C 银钉");
            choicePanel.SetActive(true);
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
        }

        private void ResolveTruthChoice(int choiceIndex)
        {
            if (truthChoiceResolved)
            {
                return;
            }

            truthChoiceResolved = true;
            truthCorrect = choiceIndex == 2;
            choicePanel.SetActive(false);
            SetCurrentQuestObjective(ObjectiveDefeatBoss);

            DialogueLine[] lines = truthCorrect
                ? new[]
                {
                    new DialogueLine("猎魔人", "不是女鬼，也不是水鬼。是活人在借哭声掩盖献祭。"),
                    new DialogueLine("委托", "真相刺穿诅咒，井底怪物露出破绽。")
                }
                : new[]
                {
                    new DialogueLine("猎魔人", "判断还不完整，但线索足够让我活下来。"),
                    new DialogueLine("委托", "诅咒因此加深，不过黑血和爪痕仍然削弱了它。")
                };

            dialogueManager.StartDialogue(lines, SpawnContractBoss);
        }

        private void ResolveSecondTruthChoice(int choiceIndex)
        {
            if (secondTruthChoiceResolved)
            {
                return;
            }

            secondTruthChoiceResolved = true;
            secondTruthCorrect = choiceIndex == 2;
            choicePanel.SetActive(false);
            SetCurrentQuestObjective(ObjectiveSecondDefeatBoss);

            DialogueLine[] lines = secondTruthCorrect
                ? new[]
                {
                    new DialogueLine("猎魔人", "不是铁匠，也不是驱魔失败。尸体是被银钉和黑蜡牵起来的。"),
                    new DialogueLine("委托", "你说出真相时，远处教堂钟声断了一拍。黑钉傀儡的护盾碎开一层。")
                }
                : new[]
                {
                    new DialogueLine("猎魔人", "还差一块线索。但黑血、铁锤和黑蜡已经足够让我找到操控者。"),
                    new DialogueLine("委托", "错误判断让傀儡更凶，但它仍留下了火与银的破绽。")
                };

            dialogueManager.StartDialogue(lines, SpawnSecondNightBoss);
        }

        private void SpawnAmbush(Vector2 position, TurnBasedEnemyVisualKind kind, int count, string title)
        {
            GameObject encounterObject = CreateEncounterObject(title, position, kind);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureMonster(kind, encounterObject.GetComponent<SpriteRenderer>().sprite, Mathf.Max(1, count), 0);
            trigger.ApplyMapScale(contractMonsterScale);
            NightContractEncounterWatcher watcher = encounterObject.AddComponent<NightContractEncounterWatcher>();
            watcher.Configure(this, NightContractEncounterRole.ClueAmbush);
            spawnedContractObjects.Add(encounterObject);
        }

        private void SpawnContractBoss()
        {
            if (bossSpawned)
            {
                return;
            }

            bossSpawned = true;
            GameObject encounterObject = CreateEncounterObject("Well Crying Soul Boss", bossPosition, TurnBasedEnemyVisualKind.BlackMoonKnight);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureContractBoss(encounterObject.GetComponent<SpriteRenderer>().sprite, 0, "井底哭魂", "井底哭魂");

            int healthPenalty = foundBlackBlood ? 10 : 0;
            int attackPenalty = truthCorrect ? 3 : 1;
            int defensePenalty = foundFalseTestimony ? 2 : 0;
            int shieldAdjustment = foundClawMarks ? -1 : 0;
            if (truthCorrect)
            {
                healthPenalty += 8;
                shieldAdjustment -= 1;
            }

            string[] weaknesses = foundBlackBlood
                ? new[] { "银", "火", "印", "剑", "？" }
                : new[] { "？", "火", "印", "剑", "？" };
            bool[] discovered = foundBlackBlood
                ? new[] { true, false, false, true, false }
                : new[] { false, false, false, true, false };
            string openingNote = truthCorrect
                ? "调查结论正确：井底哭魂开局破绽，护盾削弱。"
                : "判断有误：诅咒加深，但线索仍削弱了怪物。";

            trigger.ApplyInvestigationModifiers(healthPenalty, attackPenalty, defensePenalty, shieldAdjustment, weaknesses, discovered, openingNote);
            trigger.ApplyMapScale(contractBossScale);
            NightContractEncounterWatcher watcher = encounterObject.AddComponent<NightContractEncounterWatcher>();
            watcher.Configure(this, NightContractEncounterRole.Boss);
            spawnedContractObjects.Add(encounterObject);
        }

        private void SpawnSecondNightBoss()
        {
            if (secondBossSpawned)
            {
                return;
            }

            secondBossSpawned = true;
            GameObject encounterObject = CreateEncounterObject("Black Nail Puppet Boss", secondNightBossPosition, TurnBasedEnemyVisualKind.BlackMoonKnight);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureContractBoss(encounterObject.GetComponent<SpriteRenderer>().sprite, 1, "黑钉傀儡", "黑钉傀儡");

            int healthPenalty = foundIronCorpse ? 8 : 0;
            int attackPenalty = secondTruthCorrect ? 3 : 1;
            int defensePenalty = foundBlackenedHammer ? 2 : 0;
            int shieldAdjustment = foundChapelWax ? -1 : 0;
            if (secondTruthCorrect)
            {
                healthPenalty += 10;
                shieldAdjustment -= 1;
            }

            string[] weaknesses = foundBlackenedHammer
                ? new[] { "银", "火", "剑", "印", "？" }
                : new[] { "银", "？", "剑", "印", "？" };
            bool[] discovered = foundBlackenedHammer
                ? new[] { true, true, true, false, false }
                : new[] { true, false, true, false, false };
            string openingNote = secondTruthCorrect
                ? "调查结论正确：黑钉傀儡失去操控节奏，护盾削弱。"
                : "判断有误：黑钉傀儡攻势更狠，但火与银的线索仍然有效。";

            trigger.ApplyInvestigationModifiers(healthPenalty, attackPenalty, defensePenalty, shieldAdjustment, weaknesses, discovered, openingNote);
            trigger.ApplyMapScale(contractBossScale);
            NightContractEncounterWatcher watcher = encounterObject.AddComponent<NightContractEncounterWatcher>();
            watcher.Configure(this, NightContractEncounterRole.Boss);
            spawnedContractObjects.Add(encounterObject);
        }

        private IEnumerator ShowSettlementWhenReady()
        {
            yield return new WaitForSeconds(0.2f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            SetCurrentQuestObjective(ObjectiveReturnVillage, true);
            DialogueLine[] lines =
            {
                new DialogueLine("委托结算", "村民把最后的银币放在桌上。没人欢呼，因为井口的哭声停得太突然。"),
                new DialogueLine("第二晚钩子", "清晨，村口又多了一具尸体。这一次，尸体穿着铁匠的围裙。")
            };
            dialogueManager.StartDialogue(lines, BeginSecondNightContract);
        }

        private IEnumerator ShowSecondNightSettlementWhenReady()
        {
            yield return new WaitForSeconds(0.2f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            SetCurrentQuestObjective(ObjectiveSecondReturnVillage, true);
            DialogueLine[] lines =
            {
                new DialogueLine("委托结算", "黑钉傀儡倒下后，银钉没有融化，而是全都指向教堂地下。"),
                new DialogueLine("第三晚钩子", "夜里，神父房间的灯亮着。可守夜人说，神父三天前就已经死了。")
            };
            dialogueManager.StartDialogue(lines);
        }

        private GameObject CreateEncounterObject(string objectName, Vector2 position, TurnBasedEnemyVisualKind kind)
        {
            GameObject encounterObject = new GameObject(objectName);
            encounterObject.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer = encounterObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetEnemyMapSprite(kind);
            renderer.sortingOrder = 6;

            Rigidbody2D body = encounterObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = encounterObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return encounterObject;
        }

        private static Sprite GetEnemyMapSprite(TurnBasedEnemyVisualKind kind)
        {
            TurnBasedEnemyState preview = new TurnBasedEnemyState();
            TurnBasedEnemyAnimationLibrary.FillAnimations(preview, kind);
            if (preview.Sprite != null)
            {
                return preview.Sprite;
            }

            return WitcherSpriteLibrary.GetSolidSprite(kind == TurnBasedEnemyVisualKind.BlackMoonKnight
                ? new Color32(62, 116, 156, 230)
                : new Color32(112, 64, 76, 230));
        }

        private void EnsureChoiceUi()
        {
            if (choicePanel != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Night Contract Choice Canvas", 185);
            choicePanel = CreatePanel("TruthChoicePanel", canvas.transform, new Vector2(620f, 252f), new Vector2(0f, 26f), new Vector2(0.5f, 0.5f), new Color32(3, 8, 12, 236));
            AddOutline(choicePanel, new Color32(104, 82, 44, 255), new Vector2(2f, -2f));
            choiceSummaryText = CreateText("TruthChoiceSummary", choicePanel.transform, string.Empty, 25, TextAnchor.MiddleCenter, new Vector2(0f, 74f), new Vector2(560f, 86f), new Color32(235, 222, 172, 255));
            choiceAText = CreateChoiceButton("Choice A", "A 女鬼", new Vector2(-194f, -48f), () => ResolveActiveTruthChoice(0));
            choiceBText = CreateChoiceButton("Choice B", "B 水鬼", new Vector2(0f, -48f), () => ResolveActiveTruthChoice(1));
            choiceCText = CreateChoiceButton("Choice C", "C 献祭", new Vector2(194f, -48f), () => ResolveActiveTruthChoice(2));
            choicePanel.SetActive(false);
        }

        private Text CreateChoiceButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = CreatePanel(name, choicePanel.transform, new Vector2(166f, 58f), position, new Vector2(0.5f, 0.5f), new Color32(12, 19, 22, 245));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            AddOutline(buttonObject, new Color32(79, 111, 119, 255), new Vector2(1f, -1f));
            return CreateText(name + " Text", buttonObject.transform, text, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 46f), new Color32(233, 221, 181, 255));
        }

        private void SetChoiceLabels(string a, string b, string c)
        {
            if (choiceAText != null)
            {
                choiceAText.text = a;
            }

            if (choiceBText != null)
            {
                choiceBText.text = b;
            }

            if (choiceCText != null)
            {
                choiceCText.text = c;
            }
        }

        private static Canvas EnsureCanvas(string name, int sortingOrder)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            {
                existingCanvas.sortingOrder = Mathf.Max(existingCanvas.sortingOrder, sortingOrder);
                return existingCanvas;
            }

            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color32 color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = panel.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
            image.color = color;
            return panel;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color32 color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Text label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.color = color;
            return label;
        }

        private static void AddOutline(GameObject target, Color32 color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Sprite CreateDiamondSprite(Color32 fillColor)
        {
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    int distance = Mathf.Abs(x - 16) + Mathf.Abs(y - 16);
                    if (distance <= 14)
                    {
                        bool border = distance >= 12;
                        texture.SetPixel(x, y, border ? new Color32(235, 205, 121, 255) : fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
            sprite.name = "NightContract_DiamondMarker";
            return sprite;
        }
    }
}
