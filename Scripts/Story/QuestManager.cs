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
    public class QuestManager : MonoBehaviour
    {
        [Serializable]
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

        private QuestData activeQuest;

        public QuestData ActiveQuest => activeQuest;

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
                description = "调查灰鸦村矿洞中的哭声，找到失踪的孩子，并查明怪物出现的真正原因。",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective("与老村长对话"),
                    new QuestObjective("前往村庄西侧矿洞"),
                    new QuestObjective("调查矿洞入口的血迹"),
                    new QuestObjective("击败第一只低级食尸鬼"),
                    new QuestObjective("进入矿洞深处")
                }
            };

            StartQuest(quest);
            SetObjectiveCompleted(0, true);
        }

        public void StartQuest(QuestData quest)
        {
            activeQuest = quest;
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
                for (int i = 0; i < activeQuest.objectives.Count; i++)
                {
                    QuestObjective objective = activeQuest.objectives[i];
                    builder.Append(objective.completed ? "✓ " : "□ ");
                    builder.Append(objective.text);
                    if (i < activeQuest.objectives.Count - 1)
                    {
                        builder.AppendLine();
                    }
                }

                questObjectivesText.text = builder.ToString();
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
            questPanel = CreatePanel("QuestPanel", canvas.transform, new Vector2(360f, 230f), new Vector2(-26f, -110f), new Vector2(1f, 1f), new Color32(8, 11, 15, 210));

            questTitleText = CreateText("Quest Title", questPanel.transform, "任务标题", 23, TextAnchor.UpperLeft, new Vector2(18f, -16f), new Vector2(324f, 32f), new Color32(236, 230, 211, 255));
            questDescriptionText = CreateText("Quest Description", questPanel.transform, "任务描述", 15, TextAnchor.UpperLeft, new Vector2(18f, -54f), new Vector2(324f, 58f), new Color32(186, 199, 205, 255));
            questObjectivesText = CreateText("Quest Objectives", questPanel.transform, "任务目标", 16, TextAnchor.UpperLeft, new Vector2(18f, -122f), new Vector2(324f, 92f), new Color32(224, 221, 204, 255));
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
            return textComponent;
        }
    }
}
