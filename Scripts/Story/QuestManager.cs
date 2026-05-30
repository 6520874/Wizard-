using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [Serializable]
        // 中文说明：保存单个任务目标的文字说明和完成状态。
        public class QuestObjective
        {
            public string text;
            public bool completed;

            public QuestObjective(string text)
            {
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
        [SerializeField] private Text questNavigationHintText;
        [SerializeField] private GameObject questCompletePanel;
        [SerializeField] private Text questCompleteText;

        private QuestData activeQuest;
        private bool questCompletionShown;
        private static readonly Dictionary<int, Vector2> ObjectiveNavigationTargets = new Dictionary<int, Vector2>();

        public QuestData ActiveQuest => activeQuest;
        public int CurrentObjectiveIndex => GetCurrentObjectiveIndex();

        public static void RegisterObjectiveNavigationTarget(int objectiveIndex, Vector2 worldPosition)
        {
            if (objectiveIndex < 0)
            {
                return;
            }

            ObjectiveNavigationTargets[objectiveIndex] = worldPosition;
        }

        public bool IsObjectiveCurrent(int objectiveIndex)
        {
            return objectiveIndex >= 0 && CurrentObjectiveIndex == objectiveIndex;
        }

        private void Awake()
        {
            EnsureQuestUi();
            SetQuestPanelVisible(false);
        }

        public void StartFirstMainQuest()
        {
            QuestData quest = new QuestData
            {
                title = "灰鸦村的哭声",
                description = "调查灰鸦村矿洞中的哭声，找到失踪的孩子，并查明真正披着人皮的怪物。",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective("与老村长对话"),
                    new QuestObjective("询问铁匠关于银钉的旧事"),
                    new QuestObjective("质问神父关于驱魔仪式"),
                    new QuestObjective("追问贵族使者为何封锁村庄"),
                    new QuestObjective("寻找失踪孩子留下的线索"),
                    new QuestObjective("前往村庄西侧矿洞"),
                    new QuestObjective("调查矿洞入口的血迹"),
                    new QuestObjective("击败第一只低级食尸鬼"),
                    new QuestObjective("进入矿洞深处"),
                    new QuestObjective("击败灰母祭坛前的月夜骑士"),
                    new QuestObjective("调查灰母祭坛并公开真相")
                }
            };

            StartQuest(quest);
            SetObjectiveCompleted(0, true);
        }

        public void StartQuest(QuestData quest)
        {
            activeQuest = quest;
            questCompletionShown = false;
            if (questCompletePanel != null)
            {
                questCompletePanel.SetActive(false);
            }

            SetQuestPanelVisible(activeQuest != null);
            RefreshQuestUi();
        }

        public void SetObjectiveCompleted(int objectiveIndex, bool completed)
        {
            if (activeQuest == null || objectiveIndex < 0 || objectiveIndex >= activeQuest.objectives.Count)
            {
                return;
            }

            activeQuest.objectives[objectiveIndex].completed = completed;
            RefreshQuestUi();
            if (completed && !questCompletionShown && AreAllObjectivesCompleted())
            {
                ShowQuestCompletePanel();
            }
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
                StringBuilder builder = new StringBuilder();
                int currentObjectiveIndex = GetCurrentObjectiveIndex();
                if (currentObjectiveIndex >= 0)
                {
                    QuestObjective objective = activeQuest.objectives[currentObjectiveIndex];
                    builder.AppendLine($"当前目标 {currentObjectiveIndex + 1}/{activeQuest.objectives.Count}");
                    builder.Append("□ ");
                    builder.Append(objective.text);
                }
                else
                {
                    builder.AppendLine("主线目标完成");
                    builder.Append("✓ 灰鸦村的第一批线索已经串起来了");
                }

                questObjectivesText.text = builder.ToString();
            }

            if (questNavigationHintText != null)
            {
                int currentObjectiveIndex = GetCurrentObjectiveIndex();
                questNavigationHintText.text = currentObjectiveIndex >= 0 && ObjectiveNavigationTargets.ContainsKey(currentObjectiveIndex)
                    ? "点击任务卡：自动前往目标"
                    : "继续探索灰鸦村";
            }
        }

        private int GetCurrentObjectiveIndex()
        {
            if (activeQuest == null)
            {
                return -1;
            }

            for (int i = 0; i < activeQuest.objectives.Count; i++)
            {
                if (!activeQuest.objectives[i].completed)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool AreAllObjectivesCompleted()
        {
            if (activeQuest == null || activeQuest.objectives.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < activeQuest.objectives.Count; i++)
            {
                if (!activeQuest.objectives[i].completed)
                {
                    return false;
                }
            }

            return true;
        }

        private void ShowQuestCompletePanel()
        {
            questCompletionShown = true;
            if (questCompletePanel == null || questCompleteText == null)
            {
                EnsureQuestUi();
            }

            if (questCompleteText != null)
            {
                questCompleteText.text = "第一章完成\n灰鸦村的哭声\n\n真相不会拯救所有人，但至少能阻止下一次献祭。";
            }

            if (questCompletePanel != null)
            {
                questCompletePanel.SetActive(true);
            }
        }

        private void NavigateToCurrentObjective()
        {
            int currentObjectiveIndex = GetCurrentObjectiveIndex();
            if (currentObjectiveIndex < 0 || !ObjectiveNavigationTargets.TryGetValue(currentObjectiveIndex, out Vector2 target))
            {
                return;
            }

            GeraltController player = FindObjectOfType<GeraltController>();
            if (player != null && player.MoveToWorldDestination(target))
            {
                WitcherCombatText.Spawn("前往目标", player.transform.position + Vector3.up * 1.15f, new Color32(255, 220, 120, 255));
            }
        }

        private void SetQuestPanelVisible(bool visible)
        {
            if (questPanel != null)
            {
                questPanel.SetActive(visible);
            }
        }

        private void EnsureQuestUi()
        {
            if (questPanel != null && questTitleText != null && questDescriptionText != null && questObjectivesText != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Story UI Canvas", 120);
            questPanel = CreatePanel("QuestPanel", canvas.transform, new Vector2(360f, 176f), new Vector2(-26f, -110f), new Vector2(1f, 1f), new Color32(8, 11, 15, 218));
            Button questButton = questPanel.AddComponent<Button>();
            questButton.targetGraphic = questPanel.GetComponent<Image>();
            questButton.onClick.AddListener(NavigateToCurrentObjective);
            ColorBlock colors = questButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(42, 53, 61, 255);
            colors.pressedColor = new Color32(92, 78, 54, 255);
            questButton.colors = colors;

            questTitleText = CreateText("Quest Title", questPanel.transform, "任务标题", 23, TextAnchor.UpperLeft, new Vector2(18f, -16f), new Vector2(324f, 32f), new Color32(236, 230, 211, 255));
            questDescriptionText = CreateText("Quest Description", questPanel.transform, "任务描述", 14, TextAnchor.UpperLeft, new Vector2(18f, -54f), new Vector2(324f, 46f), new Color32(186, 199, 205, 255));
            questObjectivesText = CreateText("Quest Objectives", questPanel.transform, "任务目标", 18, TextAnchor.UpperLeft, new Vector2(18f, -104f), new Vector2(324f, 48f), new Color32(255, 225, 147, 255));
            questNavigationHintText = CreateText("Quest Navigation Hint", questPanel.transform, "点击任务卡：自动前往目标", 13, TextAnchor.MiddleRight, new Vector2(88f, -150f), new Vector2(254f, 18f), new Color32(151, 178, 190, 255));

            questCompletePanel = CreatePanel("Quest Complete Panel", canvas.transform, new Vector2(500f, 190f), new Vector2(0f, 44f), new Vector2(0.5f, 0.5f), new Color32(6, 8, 11, 232));
            questCompleteText = CreateText("Quest Complete Text", questCompletePanel.transform, string.Empty, 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460f, 150f), new Color32(255, 225, 148, 255));
            RectTransform completeTextRect = questCompleteText.GetComponent<RectTransform>();
            completeTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            completeTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            completeTextRect.pivot = new Vector2(0.5f, 0.5f);
            questCompletePanel.SetActive(false);
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
            if (EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

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
            return textComponent;
        }
    }
}
