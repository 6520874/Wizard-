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
        private const string ObjectiveInvestigateWell = "firstNight.investigateWell";
        private const string ObjectiveInvestigateMill = "firstNight.investigateMill";
        private const string ObjectiveInvestigateWidow = "firstNight.investigateWidow";
        private const string ObjectiveClearAmbush = "firstNight.clearAmbush";
        private const string ObjectiveTruthChoice = "firstNight.truthChoice";
        private const string ObjectiveDefeatBoss = "firstNight.defeatBoss";
        private const string ObjectiveReturnVillage = "firstNight.returnVillage";
        private const string ObjectiveSecondInspectCorpse = "secondNight.inspectCorpse";
        private const string ObjectiveSecondInspectHammer = "secondNight.inspectHammer";
        private const string ObjectiveSecondClearAmbush = "secondNight.clearAmbush";
        private const string ObjectiveSecondInspectChapel = "secondNight.inspectChapel";
        private const string ObjectiveSecondTruthChoice = "secondNight.truthChoice";
        private const string ObjectiveSecondDefeatBoss = "secondNight.defeatBoss";
        private const string ObjectiveSecondReturnVillage = "secondNight.returnVillage";
        private const string ObjectiveThirdMeetTriss = "thirdNight.meetTriss";
        private const string ObjectiveThirdInspectCryptGate = "thirdNight.inspectCryptGate";
        private const string ObjectiveThirdClearAmbush = "thirdNight.clearAmbush";
        private const string ObjectiveThirdInspectAltar = "thirdNight.inspectAltar";
        private const string ObjectiveThirdInspectReliquary = "thirdNight.inspectReliquary";
        private const string ObjectiveThirdTruthChoice = "thirdNight.truthChoice";
        private const string ObjectiveThirdDefeatBoss = "thirdNight.defeatBoss";
        private const string ObjectiveThirdReturnVillage = "thirdNight.returnVillage";

        private static NightContractManager instance;

        [Header("First Night Nodes")]
        [SerializeField] private Vector2 oldWellPosition = new Vector2(-1.08f, -0.82f);
        [SerializeField] private Vector2 oldMillPosition = new Vector2(-5.75f, -2.02f);
        [SerializeField] private Vector2 widowHousePosition = new Vector2(4.25f, -1.08f);
        [SerializeField] private Vector2 firstAmbushPosition = new Vector2(-3.65f, -1.82f);
        [SerializeField] private Vector2 secondAmbushPosition = new Vector2(3.25f, -2.34f);
        [SerializeField] private Vector2 bossPosition = new Vector2(7.45f, -0.92f);
        [Header("Second Night Nodes")]
        [SerializeField] private Vector2 forgeCorpsePosition = new Vector2(-6.55f, -2.58f);
        [SerializeField] private Vector2 blackenedHammerPosition = new Vector2(-0.35f, -1.48f);
        [SerializeField] private Vector2 chapelWaxPosition = new Vector2(5.95f, -1.08f);
        [SerializeField] private Vector2 secondNightFirstAmbushPosition = new Vector2(-2.95f, -2.2f);
        [SerializeField] private Vector2 secondNightSecondAmbushPosition = new Vector2(3.75f, -1.5f);
        [SerializeField] private Vector2 secondNightBossPosition = new Vector2(7.28f, -1.72f);
        [Header("Third Night Nodes")]
        [SerializeField] private Vector2 cryptGatePosition = new Vector2(-6.65f, -2.16f);
        [SerializeField] private Vector2 blackWaxAltarPosition = new Vector2(-0.48f, -1.58f);
        [SerializeField] private Vector2 sealedReliquaryPosition = new Vector2(5.88f, -1.35f);
        [SerializeField] private Vector2 thirdNightFirstAmbushPosition = new Vector2(-3.35f, -2.28f);
        [SerializeField] private Vector2 thirdNightSecondAmbushPosition = new Vector2(3.46f, -1.82f);
        [SerializeField] private Vector2 thirdNightBossPosition = new Vector2(7.16f, -1.68f);
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
        private int firstNightChoiceIndex = -1;
        private bool bossSpawned;
        private bool settlementShown;
        private int clearedAmbushCount;
        private bool secondNightStarted;
        private bool foundIronCorpse;
        private bool foundBlackenedHammer;
        private bool foundChapelWax;
        private bool secondTruthChoiceResolved;
        private bool secondTruthCorrect;
        private int secondNightChoiceIndex = -1;
        private bool secondBossSpawned;
        private bool secondSettlementShown;
        private int secondClearedAmbushCount;
        private bool thirdNightStarted;
        private bool foundCryptGate;
        private bool foundBlackWaxAltar;
        private bool foundSealedReliquary;
        private bool thirdTruthChoiceResolved;
        private bool thirdTruthCorrect;
        private int thirdNightChoiceIndex = -1;
        private bool thirdBossSpawned;
        private bool thirdSettlementShown;
        private int thirdClearedAmbushCount;
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
                case ObjectiveThirdMeetTriss:
                case ObjectiveThirdInspectCryptGate:
                    targetPosition = cryptGatePosition;
                    return true;
                case ObjectiveThirdClearAmbush:
                    targetPosition = foundBlackWaxAltar ? thirdNightSecondAmbushPosition : thirdNightFirstAmbushPosition;
                    return true;
                case ObjectiveThirdInspectAltar:
                    targetPosition = blackWaxAltarPosition;
                    return true;
                case ObjectiveThirdInspectReliquary:
                case ObjectiveThirdTruthChoice:
                    targetPosition = sealedReliquaryPosition;
                    return true;
                case ObjectiveThirdDefeatBoss:
                    targetPosition = thirdNightBossPosition;
                    return true;
                case ObjectiveThirdReturnVillage:
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
            StartCoroutine(BeginFirstNightSequence());
        }

        private IEnumerator BeginFirstNightSequence()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
            NightPhaseTransition transition = NightPhaseTransition.CreateIfMissing();
            yield return transition.PlayTransition("第一晚", "老井哭声案", MoveToFirstNightMap);
            StartFirstNightQuest();
            CreateInvestigationNodes();
            StartCoroutine(ShowContractBriefingWhenReady());
            PlayerInputController.RefreshPlayerControl();
        }

        public void BeginSecondNightContract()
        {
            if (secondNightStarted)
            {
                return;
            }

            secondNightStarted = true;
            contractStarted = true;
            questManager = QuestManager.CreateIfMissing();
            dialogueManager = DialogueManager.CreateIfMissing();
            StartCoroutine(BeginSecondNightSequence());
        }

        private IEnumerator BeginSecondNightSequence()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
            NightPhaseTransition transition = NightPhaseTransition.CreateIfMissing();
            yield return transition.PlayTransition("第二晚", "铁匠铺的黑血案", MoveToSecondNightMap);
            StartSecondNightQuest();
            CreateSecondNightNodes();
            StartCoroutine(ShowSecondNightBriefingWhenReady());
            PlayerInputController.RefreshPlayerControl();
        }

        public void BeginThirdNightContract()
        {
            if (thirdNightStarted)
            {
                return;
            }

            thirdNightStarted = true;
            contractStarted = true;
            questManager = QuestManager.CreateIfMissing();
            dialogueManager = DialogueManager.CreateIfMissing();
            StartCoroutine(BeginThirdNightSequence());
        }

        private IEnumerator BeginThirdNightSequence()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
            NightPhaseTransition transition = NightPhaseTransition.CreateIfMissing();
            yield return transition.PlayTransition("第三晚", "黑蜡地下教堂", MoveToThirdNightMap);
            StartThirdNightQuest();
            CreateThirdNightNodes();
            StartCoroutine(ShowThirdNightBriefingWhenReady());
            PlayerInputController.RefreshPlayerControl();
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
                dialogueManager.StartDialogue(StoryDatabase.GetDialogue("contract.outOfOrder", new[]
                {
                    new DialogueLine("猎魔人", "先按当前委托目标来。线索如果乱了，真相也会乱。")
                }));
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
                case NightInvestigationNodeId.CryptGate:
                    InvestigateCryptGate();
                    break;
                case NightInvestigationNodeId.BlackWaxAltar:
                    InvestigateBlackWaxAltar();
                    break;
                case NightInvestigationNodeId.SealedReliquary:
                    InvestigateSealedReliquary();
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
            if (thirdNightStarted)
            {
                return ShouldBlockThirdNightNode(nodeId);
            }

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

        private bool ShouldBlockThirdNightNode(NightInvestigationNodeId nodeId)
        {
            if (!foundCryptGate)
            {
                return nodeId != NightInvestigationNodeId.CryptGate;
            }

            if (thirdClearedAmbushCount < 1)
            {
                return true;
            }

            if (!foundBlackWaxAltar)
            {
                return nodeId != NightInvestigationNodeId.BlackWaxAltar;
            }

            if (thirdClearedAmbushCount < 2)
            {
                return true;
            }

            if (!foundSealedReliquary)
            {
                return nodeId != NightInvestigationNodeId.SealedReliquary;
            }

            return true;
        }

        public void NotifyEncounterCleared(NightContractEncounterRole role)
        {
            if (!contractStarted)
            {
                return;
            }

            if (thirdNightStarted)
            {
                NotifyThirdNightEncounterCleared(role);
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

        private void NotifyThirdNightEncounterCleared(NightContractEncounterRole role)
        {
            if (role == NightContractEncounterRole.ClueAmbush)
            {
                thirdClearedAmbushCount++;
                if (!foundBlackWaxAltar)
                {
                    SetCurrentQuestObjective(ObjectiveThirdInspectAltar);
                }
                else if (!foundSealedReliquary)
                {
                    SetCurrentQuestObjective(ObjectiveThirdInspectReliquary);
                }
                else if (!thirdTruthChoiceResolved)
                {
                    SetCurrentQuestObjective(ObjectiveThirdTruthChoice);
                }

                return;
            }

            if (role == NightContractEncounterRole.Boss)
            {
                SetCurrentQuestObjective(ObjectiveThirdReturnVillage, true);
                if (!thirdSettlementShown)
                {
                    thirdSettlementShown = true;
                    StartCoroutine(ShowThirdNightSettlementWhenReady());
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
            WitcherSfxPlayer.Play(WitcherSfxCue.ChoiceConfirm, 0.72f);
            if (thirdNightStarted)
            {
                ResolveThirdTruthChoice(choiceIndex);
                return;
            }

            if (secondNightStarted)
            {
                ResolveSecondTruthChoice(choiceIndex);
                return;
            }

            ResolveTruthChoice(choiceIndex);
        }

        private void StartFirstNightQuest()
        {
            questManager.StartQuest(StoryDatabase.GetQuest(
                "firstNight",
                "第一晚：老井哭声案",
                "调查灰鸦村夜里传出的女人哭声。每条线索都会削弱最终怪物：找出真相，比直接拔剑更重要。",
                ObjectiveInvestigateWell));
        }

        private void StartSecondNightQuest()
        {
            questManager.StartQuest(StoryDatabase.GetQuest(
                "secondNight",
                "第二晚：铁匠黑血案",
                "第一晚之后，村口出现了穿着铁匠围裙的尸体。调查黑血、铁锤和教堂蜡泪，找出是谁让尸体重新站了起来。",
                ObjectiveSecondInspectCorpse));
        }

        private void StartThirdNightQuest()
        {
            questManager.StartQuest(StoryDatabase.GetQuest(
                "thirdNight",
                "第三晚：黑蜡地下教堂",
                "特莉丝循着黑蜡火光来到灰鸦村。和她一起调查地下教堂、封钉圣匣，以及银钉真正想锁住的东西。",
                ObjectiveThirdMeetTriss));
        }

        private IEnumerator ShowContractBriefingWhenReady()
        {
            yield return new WaitForSeconds(0.2f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            DialogueLine[] briefing = StoryDatabase.GetDialogue("firstNight.briefing", new[]
            {
                new DialogueLine("委托", "第一晚：老井哭声案。村口尸体还没凉，井里却传来女人哭声。"),
                new DialogueLine("猎魔人", "先查线索。黑血、爪痕、假供词……它们会告诉我该用什么杀死它。")
            });
            dialogueManager.StartDialogue(briefing);
        }

        private IEnumerator ShowSecondNightBriefingWhenReady()
        {
            yield return new WaitForSeconds(0.35f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            DialogueLine[] briefing = StoryDatabase.GetDialogue("secondNight.briefing", new[]
            {
                new DialogueLine("委托", "第二晚：铁匠黑血案。天亮前，村口多了一具穿着铁匠围裙的尸体。"),
                new DialogueLine("猎魔人", "铁匠未必死了。先看尸体、锤子和教堂。死人不会自己换衣服，除非有人想让它看起来像铁匠。")
            });
            dialogueManager.StartDialogue(briefing);
        }

        private IEnumerator ShowThirdNightBriefingWhenReady()
        {
            yield return new WaitForSeconds(0.35f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            DialogueLine[] briefing = StoryDatabase.GetDialogue("thirdNight.briefing", new[]
            {
                new DialogueLine(GameText.TrissName, "黑蜡不是为了让死人站起来。它在给活人留一条下去的路。"),
                new DialogueLine("猎魔人", "那就一起下去。你看火，我看刀。")
            });
            dialogueManager.StartDialogue(briefing, () =>
            {
                RecruitTrissForThirdNight();
                SetCurrentQuestObjective(ObjectiveThirdInspectCryptGate);
            });
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

        private void CreateThirdNightNodes()
        {
            CreateNode(NightInvestigationNodeId.CryptGate, "地下门", cryptGatePosition, new Color32(94, 87, 128, 230));
            CreateNode(NightInvestigationNodeId.BlackWaxAltar, "黑蜡祭坛", blackWaxAltarPosition, new Color32(135, 72, 56, 230));
            CreateNode(NightInvestigationNodeId.SealedReliquary, "封钉圣匣", sealedReliquaryPosition, new Color32(108, 103, 83, 230));
        }

        private void MoveToFirstNightMap()
        {
            WitcherWorldDirector worldDirector = FindObjectOfType<WitcherWorldDirector>();
            if (worldDirector != null)
            {
                worldDirector.EnterFirstNightMap();
            }
        }

        private void MoveToSecondNightMap()
        {
            ClearContractObjects();
            WitcherWorldDirector worldDirector = FindObjectOfType<WitcherWorldDirector>();
            if (worldDirector != null)
            {
                worldDirector.EnterSecondNightMap();
            }
        }

        private void MoveToThirdNightMap()
        {
            ClearContractObjects();
            WitcherWorldDirector worldDirector = FindObjectOfType<WitcherWorldDirector>();
            if (worldDirector != null)
            {
                worldDirector.EnterThirdNightMap();
            }
        }

        private void RecruitTrissForThirdNight()
        {
            PartyManager partyManager = PartyManager.CreateIfMissing();
            partyManager.UnlockStoryAllyForBattle(GameText.TrissName);
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            PartyFollowManager.CreateIfMissing(player);
        }

        private void ClearContractObjects()
        {
            foreach (GameObject contractObject in spawnedContractObjects)
            {
                if (contractObject != null)
                {
                    Destroy(contractObject);
                }
            }

            spawnedContractObjects.Clear();
            nodes.Clear();
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("firstNight.oldWell", new[]
            {
                new DialogueLine("老井", "井沿凝着黑色血迹，银粉碰上去时发出轻微嘶鸣。"),
                new DialogueLine("猎魔人", "不是普通亡魂。银剑能伤它。")
            });
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("firstNight.oldMill", new[]
            {
                new DialogueLine("老磨坊", "木门上有反复抓挠的沟痕，却没有从外面破门的痕迹。"),
                new DialogueLine("猎魔人", "它被困过。它的护盾不会完整。")
            });
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("firstNight.widowHouse", new[]
            {
                new DialogueLine("寡妇", "我听见哭声从井底来……不，从磨坊来。别问了，猎魔人。"),
                new DialogueLine("猎魔人", "她在撒谎。哭声不是引诱，是警告。有人还想继续献祭。")
            });
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("secondNight.forgeCorpse", new[]
            {
                new DialogueLine("围裙尸体", "尸体穿着铁匠的围裙，手指却细得像从未握过锤。脖颈后钉着一枚发黑银钉。"),
                new DialogueLine("猎魔人", "这不是铁匠。有人把死者装成他，想把我的剑引向错的人。")
            });
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("secondNight.blackenedHammer", new[]
            {
                new DialogueLine("黑血铁锤", "铁锤缝隙里有黑血，火星碰到血迹时炸出一圈蓝白色寒光。"),
                new DialogueLine("猎魔人", "黑血怕火，也怕银。今晚的东西不是肉身，是被钉住的怨念。")
            });
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
            DialogueLine[] lines = StoryDatabase.GetDialogue("secondNight.chapelWax", new[]
            {
                new DialogueLine("教堂黑蜡", "教堂门口的蜡泪混着铁屑，凝成倒置的祷文。祷文最后一行写着：死者替活人赎罪。"),
                new DialogueLine("猎魔人", "神父在用银钉操控尸体。找到蜡源，傀儡的护盾会裂开。")
            });
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(secondNightSecondAmbushPosition, TurnBasedEnemyVisualKind.BloodWraith, 1, "黑蜡哭影"));
        }

        private void InvestigateCryptGate()
        {
            if (foundCryptGate)
            {
                return;
            }

            foundCryptGate = true;
            CompleteNode(NightInvestigationNodeId.CryptGate);
            SetCurrentQuestObjective(ObjectiveThirdClearAmbush);
            DialogueLine[] lines = StoryDatabase.GetDialogue("thirdNight.cryptGate", new[]
            {
                new DialogueLine("地下门", "门缝里没有风，只有热。黑蜡沿着石阶往下流，像有人在下面点着一整排蜡烛。"),
                new DialogueLine(GameText.TrissName, "我能烧开门上的蜡，但它们会记住我的火。")
            });
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(thirdNightFirstAmbushPosition, TurnBasedEnemyVisualKind.BloodWraith, 2, "黑蜡守门影"));
        }

        private void InvestigateBlackWaxAltar()
        {
            if (foundBlackWaxAltar)
            {
                return;
            }

            foundBlackWaxAltar = true;
            CompleteNode(NightInvestigationNodeId.BlackWaxAltar);
            SetCurrentQuestObjective(ObjectiveThirdClearAmbush);
            DialogueLine[] lines = StoryDatabase.GetDialogue("thirdNight.blackWaxAltar", new[]
            {
                new DialogueLine("黑蜡祭坛", "祭坛上没有神像，只有一圈烧短的蜡。每根蜡烛里都封着一小段头发。"),
                new DialogueLine("猎魔人", "不是献祭。更像是把人留在这里，等有人替他们选择。")
            });
            dialogueManager.StartDialogue(lines, () => SpawnAmbush(thirdNightSecondAmbushPosition, TurnBasedEnemyVisualKind.CorruptedWolf, 2, "蜡下腐兽"));
        }

        private void InvestigateSealedReliquary()
        {
            if (foundSealedReliquary)
            {
                return;
            }

            foundSealedReliquary = true;
            CompleteNode(NightInvestigationNodeId.SealedReliquary);
            SetCurrentQuestObjective(ObjectiveThirdTruthChoice);
            DialogueLine[] lines = StoryDatabase.GetDialogue("thirdNight.sealedReliquary", new[]
            {
                new DialogueLine("封钉圣匣", "圣匣里躺着三枚银钉。每枚钉帽上都刻着一个孩子的名字，最小的那个还没干。"),
                new DialogueLine(GameText.TrissName, "烧掉它，门会开。留下它，村里今晚能睡。交出去……他们就得自己醒着。")
            });
            dialogueManager.StartDialogue(lines, ShowThirdTruthChoice);
        }

        private void CompleteNode(NightInvestigationNodeId id)
        {
            if (nodes.TryGetValue(id, out NightInvestigationNode node))
            {
                node.SetCompleted(true);
                WitcherSfxPlayer.Play(WitcherSfxCue.ClueFound, 0.72f);
            }
        }

        private void SetCurrentQuestObjective(string objectiveText, bool completed = false)
        {
            questManager = questManager == null ? QuestManager.CreateIfMissing() : questManager;
            questManager.SetCurrentObjectiveById(objectiveText, objectiveText, completed);
        }

        private void ShowTruthChoice()
        {
            if (truthChoiceResolved)
            {
                return;
            }

            EnsureChoiceUi();
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice(
                "firstNight.truthChoice",
                "井口哭声停下前，你决定：\nA 封井止哭   B 公开献祭者   C 先救活人",
                2,
                "A 封井",
                "B 公开",
                "C 救人");
            choiceSummaryText.text = choice.Summary;
            SetChoiceLabels(choice.Labels[0], choice.Labels[1], choice.Labels[2]);
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
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice(
                "secondNight.truthChoice",
                "第二具尸体站起来后，你把真相交给谁？\nA 交出铁匠   B 保住教堂   C 拔掉银钉",
                2,
                "A 铁匠",
                "B 教堂",
                "C 银钉");
            choiceSummaryText.text = choice.Summary;
            SetChoiceLabels(choice.Labels[0], choice.Labels[1], choice.Labels[2]);
            choicePanel.SetActive(true);
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            player?.SetControlEnabled(false);
        }

        private void ShowThirdTruthChoice()
        {
            if (thirdTruthChoiceResolved)
            {
                return;
            }

            EnsureChoiceUi();
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice(
                "thirdNight.truthChoice",
                "圣匣里的银钉还温着，你决定：\nA 让特莉丝烧尽黑蜡   B 封住地下门   C 把银钉交给孩子",
                2,
                "A 烧蜡",
                "B 封门",
                "C 交钉");
            choiceSummaryText.text = choice.Summary;
            SetChoiceLabels(choice.Labels[0], choice.Labels[1], choice.Labels[2]);
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
            firstNightChoiceIndex = choiceIndex;
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice("firstNight.truthChoice", string.Empty, 2, "A 封井", "B 公开", "C 救人");
            truthCorrect = choiceIndex == choice.CorrectIndex;
            choicePanel.SetActive(false);
            SetCurrentQuestObjective(ObjectiveDefeatBoss);

            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetFirstNightChoiceDialogueId(choiceIndex),
                GetFirstNightChoiceFallback(choiceIndex));

            dialogueManager.StartDialogue(lines, SpawnContractBoss);
        }

        private void ResolveSecondTruthChoice(int choiceIndex)
        {
            if (secondTruthChoiceResolved)
            {
                return;
            }

            secondTruthChoiceResolved = true;
            secondNightChoiceIndex = choiceIndex;
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice("secondNight.truthChoice", string.Empty, 2, "A 铁匠", "B 教堂", "C 银钉");
            secondTruthCorrect = choiceIndex == choice.CorrectIndex;
            choicePanel.SetActive(false);
            SetCurrentQuestObjective(ObjectiveSecondDefeatBoss);

            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetSecondNightChoiceDialogueId(choiceIndex),
                GetSecondNightChoiceFallback(choiceIndex));

            dialogueManager.StartDialogue(lines, SpawnSecondNightBoss);
        }

        private void ResolveThirdTruthChoice(int choiceIndex)
        {
            if (thirdTruthChoiceResolved)
            {
                return;
            }

            thirdTruthChoiceResolved = true;
            thirdNightChoiceIndex = choiceIndex;
            StoryDatabase.ChoiceData choice = StoryDatabase.GetChoice("thirdNight.truthChoice", string.Empty, 2, "A 烧蜡", "B 封门", "C 交钉");
            thirdTruthCorrect = choiceIndex == choice.CorrectIndex;
            choicePanel.SetActive(false);
            SetCurrentQuestObjective(ObjectiveThirdDefeatBoss);

            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetThirdNightChoiceDialogueId(choiceIndex),
                GetThirdNightChoiceFallback(choiceIndex));

            dialogueManager.StartDialogue(lines, SpawnThirdNightBoss);
        }

        private static string GetFirstNightChoiceDialogueId(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return "firstNight.choiceSealWell";
                case 1:
                    return "firstNight.choiceNameVillage";
                default:
                    return "firstNight.choiceSaveLiving";
            }
        }

        private static DialogueLine[] GetFirstNightChoiceFallback(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine("猎魔人", "井盖落下时，哭声像被塞进石头里。村口的人第一次敢靠近火堆。"),
                        new DialogueLine("寡妇", "她还在下面。你们只是听不见了。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("猎魔人", "献祭者的名字被说出口后，村民都低下头，像突然认得自己的鞋。"),
                        new DialogueLine("委托", "井底的哭声变轻了。寡妇家的窗却再也没有亮。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine("猎魔人", "你把还活着的人从井边拖开。哭声没有停，却开始给你让路。"),
                        new DialogueLine("委托", "井水翻起白雾，像有人在下面松了一口气。")
                    };
            }
        }

        private static string GetSecondNightChoiceDialogueId(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return "secondNight.choiceBlameSmith";
                case 1:
                    return "secondNight.choiceKeepChurch";
                default:
                    return "secondNight.choicePullNails";
            }
        }

        private static DialogueLine[] GetSecondNightChoiceFallback(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine("铁匠", "他们把铁匠铺的火灭了。没人问炉灰里为什么有教堂的蜡。"),
                        new DialogueLine("猎魔人", "尸体听见这个结果，反而站得更直。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("教堂", "钟声响了三下，村民跪下时很安静。黑蜡沿台阶往上爬。"),
                        new DialogueLine("猎魔人", "有些门被保住了。也有些东西被关在门后。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine("猎魔人", "第一枚银钉拔出来时，尸体倒回泥里，像终于记起自己已经死了。"),
                        new DialogueLine("委托", "铁匠铺的火重新亮起。教堂没有开门。")
                    };
            }
        }

        private static string GetThirdNightChoiceDialogueId(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return "thirdNight.choiceBurnWax";
                case 1:
                    return "thirdNight.choiceSealDoor";
                default:
                    return "thirdNight.choiceGiveNails";
            }
        }

        private static DialogueLine[] GetThirdNightChoiceFallback(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine(GameText.TrissName, "火从圣匣里翻出来时，黑蜡像雪一样塌下去。那些名字也跟着变轻了。"),
                        new DialogueLine("猎魔人", "门开了。里面的东西没有哭，只是在等我们。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("猎魔人", "你把门重新封上。村里钟声停了，地下却多了一次敲门声。"),
                        new DialogueLine(GameText.TrissName, "有些安静，不是结束。只是还没有轮到他们说话。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine(GameText.TrissName, "孩子接过银钉时没有哭。他只是问：如果我怕，能不能晚一点敲下去。"),
                        new DialogueLine("猎魔人", "你没有替他回答。圣匣自己裂开了一道缝。")
                    };
            }
        }

        private void SpawnAmbush(Vector2 position, TurnBasedEnemyVisualKind kind, int count, string title)
        {
            WitcherSfxPlayer.Play(WitcherSfxCue.EncounterSpawn, 0.72f);
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
            WitcherSfxPlayer.Play(WitcherSfxCue.BossSpawn, 0.82f);
            GameObject encounterObject = CreateEncounterObject("Well Crying Soul Boss", bossPosition, TurnBasedEnemyVisualKind.BlackMoonKnight);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureContractBoss(encounterObject.GetComponent<SpriteRenderer>().sprite, 0, "井底哭魂", "井底哭魂");

            int healthPenalty = foundBlackBlood ? 10 : 0;
            int attackPenalty = truthCorrect ? 3 : 1;
            int defensePenalty = foundFalseTestimony ? 2 : 0;
            int shieldAdjustment = foundClawMarks ? -1 : 0;
            string openingNote;
            switch (firstNightChoiceIndex)
            {
                case 0:
                    healthPenalty += 3;
                    attackPenalty = 0;
                    defensePenalty += 1;
                    shieldAdjustment += 1;
                    openingNote = "选择后果：井口被封，村民安静下来；井底哭魂在黑暗里撞得更急。";
                    break;
                case 1:
                    healthPenalty += 10;
                    attackPenalty = 2;
                    defensePenalty += foundFalseTestimony ? 3 : 1;
                    openingNote = "选择后果：献祭者的名字被说出口，伪装变薄；村里有人开始恨你。";
                    break;
                default:
                    healthPenalty += 16;
                    attackPenalty = 3;
                    shieldAdjustment -= 2;
                    openingNote = "选择后果：你先救活人，哭声仍在；井底哭魂露出最深的一道裂缝。";
                    break;
            }

            string[] weaknesses = foundBlackBlood
                ? new[] { "银", "火", "印", "剑", "？" }
                : new[] { "？", "火", "印", "剑", "？" };
            bool[] discovered = foundBlackBlood
                ? new[] { true, false, false, true, false }
                : new[] { false, false, false, true, false };
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
            WitcherSfxPlayer.Play(WitcherSfxCue.BossSpawn, 0.86f);
            GameObject encounterObject = CreateEncounterObject("Black Nail Puppet Boss", secondNightBossPosition, TurnBasedEnemyVisualKind.BlackNailPuppet);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureContractBoss(encounterObject.GetComponent<SpriteRenderer>().sprite, 1, "黑钉傀儡", "黑钉傀儡", TurnBasedEnemyVisualKind.BlackNailPuppet);

            int healthPenalty = foundIronCorpse ? 8 : 0;
            int attackPenalty = secondTruthCorrect ? 3 : 1;
            int defensePenalty = foundBlackenedHammer ? 2 : 0;
            int shieldAdjustment = foundChapelWax ? -1 : 0;
            string openingNote;
            switch (secondNightChoiceIndex)
            {
                case 0:
                    healthPenalty += 2;
                    attackPenalty = 0;
                    shieldAdjustment += 1;
                    openingNote = "选择后果：铁匠成了村口的答案；黑钉傀儡像收到命令一样站稳。";
                    break;
                case 1:
                    healthPenalty += 6;
                    attackPenalty = 1;
                    defensePenalty += 2;
                    openingNote = "选择后果：教堂保住了门面；黑蜡也保住了傀儡的骨架。";
                    break;
                default:
                    healthPenalty += 14;
                    attackPenalty = 3;
                    shieldAdjustment -= 2;
                    openingNote = "选择后果：银钉被拔出，尸体倒下；黑钉傀儡失去一半操控节奏。";
                    break;
            }

            string[] weaknesses = foundBlackenedHammer
                ? new[] { "银", "火", "剑", "印", "？" }
                : new[] { "银", "？", "剑", "印", "？" };
            bool[] discovered = foundBlackenedHammer
                ? new[] { true, true, true, false, false }
                : new[] { true, false, true, false, false };
            trigger.ApplyInvestigationModifiers(healthPenalty, attackPenalty, defensePenalty, shieldAdjustment, weaknesses, discovered, openingNote);
            trigger.ApplyMapScale(contractBossScale);
            NightContractEncounterWatcher watcher = encounterObject.AddComponent<NightContractEncounterWatcher>();
            watcher.Configure(this, NightContractEncounterRole.Boss);
            spawnedContractObjects.Add(encounterObject);
        }

        private void SpawnThirdNightBoss()
        {
            if (thirdBossSpawned)
            {
                return;
            }

            thirdBossSpawned = true;
            WitcherSfxPlayer.Play(WitcherSfxCue.BossSpawn, 0.9f);
            GameObject encounterObject = CreateEncounterObject("Black Wax Saint Boss", thirdNightBossPosition, TurnBasedEnemyVisualKind.BlackNailPuppet);
            BattleEncounterTrigger trigger = encounterObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureContractBoss(encounterObject.GetComponent<SpriteRenderer>().sprite, 2, "黑蜡圣徒", "黑蜡圣徒", TurnBasedEnemyVisualKind.BlackNailPuppet);

            int healthPenalty = foundCryptGate ? 8 : 0;
            int attackPenalty = thirdTruthCorrect ? 4 : 1;
            int defensePenalty = foundBlackWaxAltar ? 2 : 0;
            int shieldAdjustment = foundSealedReliquary ? -1 : 0;
            string openingNote;
            switch (thirdNightChoiceIndex)
            {
                case 0:
                    healthPenalty += 12;
                    attackPenalty = 2;
                    shieldAdjustment -= 1;
                    openingNote = "选择后果：特莉丝烧开黑蜡，圣徒的外壳变薄；那些被蜡封住的名字也一起安静了。";
                    break;
                case 1:
                    healthPenalty += 4;
                    attackPenalty = 0;
                    defensePenalty += 2;
                    shieldAdjustment += 2;
                    openingNote = "选择后果：地下门被封住，村里暂时睡去；黑蜡圣徒像守门人一样站得更稳。";
                    break;
                default:
                    healthPenalty += 18;
                    attackPenalty = 4;
                    shieldAdjustment -= 2;
                    openingNote = "选择后果：银钉交到孩子手里，圣匣自己裂开；黑蜡圣徒失去了最顺手的命令。";
                    break;
            }

            string[] weaknesses = foundBlackWaxAltar
                ? new[] { "火", "银", "印", "剑", "？" }
                : new[] { "火", "？", "印", "剑", "？" };
            bool[] discovered = foundBlackWaxAltar
                ? new[] { true, true, false, true, false }
                : new[] { true, false, false, true, false };
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
            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetFirstNightSettlementDialogueId(),
                GetFirstNightSettlementFallback());
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
            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetSecondNightSettlementDialogueId(),
                GetSecondNightSettlementFallback());
            dialogueManager.StartDialogue(lines, BeginThirdNightContract);
        }

        private IEnumerator ShowThirdNightSettlementWhenReady()
        {
            yield return new WaitForSeconds(0.2f);
            while (DialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            SetCurrentQuestObjective(ObjectiveThirdReturnVillage, true);
            DialogueLine[] lines = StoryDatabase.GetDialogue(
                GetThirdNightSettlementDialogueId(),
                GetThirdNightSettlementFallback());
            dialogueManager.StartDialogue(lines);
        }

        private string GetFirstNightSettlementDialogueId()
        {
            switch (firstNightChoiceIndex)
            {
                case 0:
                    return "firstNight.settlementSealWell";
                case 1:
                    return "firstNight.settlementNameVillage";
                default:
                    return "firstNight.settlementSaveLiving";
            }
        }

        private DialogueLine[] GetFirstNightSettlementFallback()
        {
            switch (firstNightChoiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine("委托结算", "村民把银币放在桌上。那一晚没人再听见哭声，连寡妇也没有。"),
                        new DialogueLine("第二晚钩子", "清晨，封井的铁链断了一节。村口又多了一具穿着铁匠围裙的尸体。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("委托结算", "银币被推到你面前时，几个人离席了。寡妇家的门被钉上一块木板。"),
                        new DialogueLine("第二晚钩子", "天亮前，铁匠铺的炉火自己灭了。村口多了一具穿着铁匠围裙的尸体。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine("委托结算", "孩子醒来后没有说话，只把一枚湿透的银币塞进你手心。"),
                        new DialogueLine("第二晚钩子", "清晨，井边多了一排小脚印。脚印尽头，是一具穿着铁匠围裙的尸体。")
                    };
            }
        }

        private string GetSecondNightSettlementDialogueId()
        {
            switch (secondNightChoiceIndex)
            {
                case 0:
                    return "secondNight.settlementBlameSmith";
                case 1:
                    return "secondNight.settlementKeepChurch";
                default:
                    return "secondNight.settlementPullNails";
            }
        }

        private DialogueLine[] GetSecondNightSettlementFallback()
        {
            switch (secondNightChoiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine("委托结算", "铁匠铺门口堆满石头。村民说这样睡得踏实些。"),
                        new DialogueLine("第三晚钩子", "夜里，石头缝里渗出黑蜡。教堂地下传来敲铁的声音。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("委托结算", "神父房间的灯亮了一整夜。村民路过时都放轻脚步。"),
                        new DialogueLine("第三晚钩子", "天快亮时，钟楼落下一根银钉，钉尖指向地下。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine("委托结算", "傀儡倒下后，铁匠坐在炉前，把每一枚银钉都敲弯。"),
                        new DialogueLine("第三晚钩子", "夜里，教堂门自己开了。门内没有神父，只有一排刚点燃的蜡。")
                    };
            }
        }

        private string GetThirdNightSettlementDialogueId()
        {
            switch (thirdNightChoiceIndex)
            {
                case 0:
                    return "thirdNight.settlementBurnWax";
                case 1:
                    return "thirdNight.settlementSealDoor";
                default:
                    return "thirdNight.settlementGiveNails";
            }
        }

        private DialogueLine[] GetThirdNightSettlementFallback()
        {
            switch (thirdNightChoiceIndex)
            {
                case 0:
                    return new[]
                    {
                        new DialogueLine("委托结算", "地下教堂亮了一整夜。天亮后，村民发现每扇窗台上都有一撮黑灰。"),
                        new DialogueLine(GameText.TrissName, "我不知道我们救下了谁，也不知道谁被我们烧没了。")
                    };
                case 1:
                    return new[]
                    {
                        new DialogueLine("委托结算", "村里终于睡了一个安稳觉。第三天清晨，教堂门口多了一只从里面伸出的手印。"),
                        new DialogueLine("猎魔人", "有些门能挡住怪物，也能挡住求救。")
                    };
                default:
                    return new[]
                    {
                        new DialogueLine("委托结算", "孩子把银钉埋在井边，没有告诉任何人。那晚之后，井水第一次映出了星星。"),
                        new DialogueLine(GameText.TrissName, "他还会害怕。可这次，害怕是他自己的。")
                    };
            }
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

            return WitcherSpriteLibrary.GetSolidSprite(kind == TurnBasedEnemyVisualKind.BlackMoonKnight || kind == TurnBasedEnemyVisualKind.BlackNailPuppet
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
