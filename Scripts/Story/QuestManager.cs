using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    /// <summary>
    /// Tracks the active main quest and renders a compact quest panel.
    /// Attach this to a scene GameObject, or let OpeningStoryManager create it at runtime.
    /// </summary>
    // 中文说明：管理当前任务、任务目标进度以及任务面板的显示刷新。
    public class QuestManager : MonoBehaviour
    {
        private const string ManagerName = "Quest Manager";
        private static QuestManager instance;

        [Serializable]
        // 中文说明：保存单个任务目标的文字说明和完成状态。
        public class QuestObjective
        {
            public string id;
            public string text;
            public bool completed;

            public QuestObjective(string text)
                : this(text, text)
            {
            }

            public QuestObjective(string id, string text)
            {
                this.id = id;
                this.text = text;
            }
        }

        [Serializable]
        // 中文说明：保存一条任务的标题、描述和目标列表数据。
        public class QuestData
        {
            public string title;
            [TextArea(2, 5)] public string description;
            public List<QuestObjective> objectives = new List<QuestObjective>();
        }

        [Header("Optional UI References")]
        [SerializeField] private GameObject questPanel;
        [SerializeField] private Text questTitleText;
        [SerializeField] private Text questDescriptionText;
        [SerializeField] private Text questObjectivesText;
        [SerializeField] private Text questTrackHintText;
        [SerializeField] private Button questPanelButton;

        private QuestData activeQuest;
        private GeraltController player;
        private bool questPanelSuppressed;

        public QuestData ActiveQuest => activeQuest;
        public QuestObjective CurrentObjective => activeQuest != null && activeQuest.objectives.Count > 0 ? activeQuest.objectives[0] : null;

        public static QuestManager CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            QuestManager existing = FindObjectOfType<QuestManager>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            return new GameObject(ManagerName).AddComponent<QuestManager>();
        }

        private void Awake()
        {
            instance = this;
            EnsureQuestUi();
            SetQuestPanelVisible(false);
        }

        public void StartFirstMainQuest()
        {
            QuestData quest = StoryDatabase.GetQuest(
                "main.greyRavenCry",
                "灰鸦村的哭声",
                "调查灰鸦村矿洞中的哭声，找到失踪的孩子，并查明怪物出现的真正原因。",
                "main.talkVillageElder",
                "main.goToMine",
                "main.inspectMineBlood",
                "main.defeatGhoul",
                "main.enterMineDepths");

            StartQuest(quest);
            SetObjectiveCompleted(0, true);
        }

        public void StartQuest(QuestData quest)
        {
            activeQuest = quest;
            SetQuestPanelVisible(activeQuest != null);
            RefreshQuestUi();
            DialogueManager.RefreshQuestHintIfVisible();
        }

        public void SetQuestPanelSuppressed(bool suppressed)
        {
            questPanelSuppressed = suppressed;
            SetQuestPanelVisible(activeQuest != null);
        }

        public void SetObjectiveCompleted(int objectiveIndex, bool completed)
        {
            if (activeQuest == null || objectiveIndex < 0 || objectiveIndex >= activeQuest.objectives.Count)
            {
                return;
            }

            activeQuest.objectives[objectiveIndex].completed = completed;
            RefreshQuestUi();
            DialogueManager.RefreshQuestHintIfVisible();
        }

        public void SetObjectiveCompleted(string objectiveText, bool completed)
        {
            if (activeQuest == null)
            {
                return;
            }

            for (int i = 0; i < activeQuest.objectives.Count; i++)
            {
                if (activeQuest.objectives[i].text == objectiveText)
                {
                    SetObjectiveCompleted(i, completed);
                    return;
                }
            }
        }

        public void SetCurrentObjective(string objectiveText, bool completed = false)
        {
            if (activeQuest == null)
            {
                return;
            }

            SetSingleObjective(new QuestObjective(objectiveText) { completed = completed });
            RefreshQuestUi();
            DialogueManager.RefreshQuestHintIfVisible();
        }

        public void SetCurrentObjectiveById(string objectiveId, string fallbackText, bool completed = false)
        {
            if (activeQuest == null)
            {
                return;
            }

            SetSingleObjective(new QuestObjective(objectiveId, StoryDatabase.GetObjectiveText(objectiveId, fallbackText)) { completed = completed });
            RefreshQuestUi();
            DialogueManager.RefreshQuestHintIfVisible();
        }

        private void SetSingleObjective(QuestObjective objective)
        {
            activeQuest.objectives.Clear();
            activeQuest.objectives.Add(objective);
        }

        private void RefreshQuestUi()
        {
            if (activeQuest == null)
            {
                return;
            }

            if (questTitleText != null)
            {
                questTitleText.text = activeQuest.title;
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.text = activeQuest.description;
            }

            if (questObjectivesText != null)
            {
                questObjectivesText.text = BuildObjectivesText(activeQuest.objectives);
            }
        }

        private static string BuildObjectivesText(IReadOnlyList<QuestObjective> objectives)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < objectives.Count; i++)
            {
                QuestObjective objective = objectives[i];
                builder.Append(objective.completed ? GameText.Quest.CompletedPrefix : GameText.Quest.PendingPrefix);
                builder.Append(objective.text);
                if (i < objectives.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void SetQuestPanelVisible(bool visible)
        {
            if (questPanel != null)
            {
                questPanel.SetActive(visible && !questPanelSuppressed);
            }
        }

        private void EnsureQuestUi()
        {
            if (questPanel != null && questTitleText != null && questDescriptionText != null && questObjectivesText != null)
            {
                EnsureQuestPanelButton();
                EnsureTrackHintText();
                return;
            }

            Canvas canvas = EnsureCanvas("Story UI Canvas", 120);
            questPanel = CreatePanel("QuestPanel", canvas.transform, new Vector2(360f, 230f), new Vector2(-26f, -110f), new Vector2(1f, 1f), new Color32(8, 11, 15, 210));

            questTitleText = CreateText("Quest Title", questPanel.transform, GameText.Quest.TitlePlaceholder, 23, TextAnchor.UpperLeft, new Vector2(18f, -16f), new Vector2(324f, 32f), new Color32(236, 230, 211, 255));
            questDescriptionText = CreateText("Quest Description", questPanel.transform, GameText.Quest.DescriptionPlaceholder, 15, TextAnchor.UpperLeft, new Vector2(18f, -54f), new Vector2(324f, 58f), new Color32(186, 199, 205, 255));
            questObjectivesText = CreateText("Quest Objectives", questPanel.transform, GameText.Quest.ObjectivesPlaceholder, 16, TextAnchor.UpperLeft, new Vector2(18f, -122f), new Vector2(324f, 92f), new Color32(224, 221, 204, 255));
            questTrackHintText = CreateText("Quest Track Hint", questPanel.transform, GameText.Quest.TrackHint, 13, TextAnchor.LowerRight, new Vector2(18f, -202f), new Vector2(324f, 22f), new Color32(146, 176, 184, 230));
            EnsureQuestPanelButton();
        }

        private void EnsureQuestPanelButton()
        {
            if (questPanel == null)
            {
                return;
            }

            questPanelButton = questPanel.GetComponent<Button>();
            if (questPanelButton == null)
            {
                questPanelButton = questPanel.AddComponent<Button>();
            }

            Image image = questPanel.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                questPanelButton.targetGraphic = image;
            }

            questPanelButton.onClick.RemoveListener(NavigateToCurrentObjective);
            questPanelButton.onClick.AddListener(NavigateToCurrentObjective);
            questPanelButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = questPanelButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(84, 103, 112, 255);
            colors.pressedColor = new Color32(124, 96, 52, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(70, 70, 70, 180);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            questPanelButton.colors = colors;
        }

        private void EnsureTrackHintText()
        {
            if (questPanel == null || questTrackHintText != null)
            {
                return;
            }

            questTrackHintText = CreateText("Quest Track Hint", questPanel.transform, GameText.Quest.TrackHint, 13, TextAnchor.LowerRight, new Vector2(18f, -202f), new Vector2(324f, 22f), new Color32(146, 176, 184, 230));
        }

        private void NavigateToCurrentObjective()
        {
            QuestObjective objective = CurrentObjective;
            if (objective == null || objective.completed || string.IsNullOrWhiteSpace(objective.text))
            {
                return;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null)
            {
                return;
            }

            if (!TryResolveObjectiveTarget(string.IsNullOrWhiteSpace(objective.id) ? objective.text : objective.id, out Vector2 targetPosition))
            {
                ShowTrackHint(GameText.Quest.NoTrackTarget);
                return;
            }

            bool moving = player.MoveToWorldPosition(targetPosition);
            ShowTrackHint(moving ? GameText.Quest.MovingToTarget : GameText.Quest.ArrivedNearTarget);
        }

        private bool TryResolveObjectiveTarget(string objectiveText, out Vector2 targetPosition)
        {
            NightContractManager nightContract = FindObjectOfType<NightContractManager>();
            if (nightContract != null && nightContract.TryGetNavigationTarget(objectiveText, out targetPosition))
            {
                return true;
            }

            switch (objectiveText)
            {
                case "main.talkVillageElder":
                case "与老村长对话":
                    targetPosition = Vector2.zero;
                    return true;
                case "main.goToMine":
                case "main.inspectMineBlood":
                case "main.defeatGhoul":
                case "main.enterMineDepths":
                case "前往村庄西侧矿洞":
                case "调查矿洞入口的血迹":
                case "击败第一只低级食尸鬼":
                case "进入矿洞深处":
                    targetPosition = new Vector2(-5.75f, -2.02f);
                    return true;
                default:
                    targetPosition = default;
                    return false;
            }
        }

        private void ShowTrackHint(string hint)
        {
            if (questTrackHintText == null)
            {
                return;
            }

            questTrackHintText.text = hint;
        }

        private static Canvas EnsureCanvas(string name, int sortingOrder)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            {
                return existingCanvas;
            }

            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasObject.AddComponent<CanvasScaler>();
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
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
            return panel;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color32 color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = anchor;
            textComponent.color = color;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;
            return textComponent;
        }
    }
}
