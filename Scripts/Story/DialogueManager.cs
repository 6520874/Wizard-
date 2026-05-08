using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    /// <summary>
    /// Plays ordered dialogue lines with speaker names.
    /// Can be reused by later chapters by passing a new DialogueLine array to StartDialogue.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Serializable]
        public struct DialogueLine
        {
            public string speakerName;
            [TextArea(2, 4)] public string text;

            public DialogueLine(string speakerName, string text)
            {
                this.speakerName = speakerName;
                this.text = text;
            }
        }

        [Header("Dialogue")]
        [SerializeField] private float typewriterCharactersPerSecond = 38f;
        [SerializeField] private bool startFirstQuestWhenDefaultDialogueEnds = true;

        [Header("Optional UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Text continueHintText;

        [Header("Optional Manager References")]
        [SerializeField] private QuestManager questManager;

        private DialogueLine[] activeLines = Array.Empty<DialogueLine>();
        private int lineIndex;
        private bool isTyping;
        private bool dialogueActive;
        private string currentFullText;
        private Coroutine typingRoutine;
        private Action onDialogueFinished;

        private static readonly DialogueLine[] DefaultVillageDialogue =
        {
            new DialogueLine("老村长", "猎魔人……你终于来了。"),
            new DialogueLine("老村长", "三天前，矿洞里传来了女人的哭声。"),
            new DialogueLine("老村长", "进去的人，一个都没回来。"),
            new DialogueLine("老村长", "昨晚，我的小孙子也不见了。"),
            new DialogueLine("老村长", "他们说那是女妖，可我知道……那声音像极了我死去的女儿。"),
            new DialogueLine("猎魔人", "怪物不会无缘无故出现。"),
            new DialogueLine("猎魔人", "带我去最后一个失踪者出现的地方。"),
            new DialogueLine("猎魔人", "还有，准备好我的报酬。"),
            new DialogueLine("老村长", "只要你能救回孩子，村里最后的银币都给你。"),
            new DialogueLine("老村长", "但你要小心……这里死去的人，好像都还没真正离开。")
        };

        private void Awake()
        {
            EnsureDialogueUi();
            dialoguePanel.SetActive(false);
            questManager = questManager == null ? FindObjectOfType<QuestManager>() : questManager;
        }

        private void Update()
        {
            if (!dialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                AdvanceDialogue();
            }
        }

        public void StartDefaultVillageDialogue()
        {
            StartDialogue(DefaultVillageDialogue, () =>
            {
                if (startFirstQuestWhenDefaultDialogueEnds)
                {
                    questManager = questManager == null ? FindObjectOfType<QuestManager>() : questManager;
                    if (questManager != null)
                    {
                        questManager.StartFirstMainQuest();
                    }
                }
            });
        }

        public void HideDialogue()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            isTyping = false;
            dialogueActive = false;
            onDialogueFinished = null;
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        public void StartDialogue(DialogueLine[] lines, Action onFinished = null)
        {
            if (lines == null || lines.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            activeLines = lines;
            onDialogueFinished = onFinished;
            lineIndex = 0;
            dialogueActive = true;
            dialoguePanel.SetActive(true);
            ShowLine(activeLines[lineIndex]);
        }

        private void AdvanceDialogue()
        {
            if (isTyping)
            {
                FinishTypingImmediately();
                return;
            }

            lineIndex++;
            if (lineIndex >= activeLines.Length)
            {
                EndDialogue();
                return;
            }

            ShowLine(activeLines[lineIndex]);
        }

        private void ShowLine(DialogueLine line)
        {
            speakerNameText.text = line.speakerName;
            currentFullText = line.text;
            continueHintText.text = "点击或按空格继续";

            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            typingRoutine = StartCoroutine(TypeLine(currentFullText));
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            dialogueText.text = string.Empty;
            float delay = typewriterCharactersPerSecond <= 0f ? 0f : 1f / typewriterCharactersPerSecond;

            for (int i = 0; i < line.Length; i++)
            {
                dialogueText.text += line[i];
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
            }

            isTyping = false;
        }

        private void FinishTypingImmediately()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            dialogueText.text = currentFullText;
            isTyping = false;
        }

        private void EndDialogue()
        {
            dialogueActive = false;
            dialoguePanel.SetActive(false);
            Action finished = onDialogueFinished;
            onDialogueFinished = null;
            finished?.Invoke();
        }

        private void EnsureDialogueUi()
        {
            if (dialoguePanel != null && speakerNameText != null && dialogueText != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Story UI Canvas", 120);
            dialoguePanel = CreatePanel("DialoguePanel", canvas.transform, new Vector2(980f, 190f), new Vector2(0f, 36f), new Vector2(0.5f, 0f), new Color32(8, 10, 13, 225));
            speakerNameText = CreateText("Dialogue Speaker Name", dialoguePanel.transform, "角色名", 24, TextAnchor.MiddleLeft, new Vector2(30f, -20f), new Vector2(260f, 36f), new Color32(233, 222, 193, 255));
            dialogueText = CreateText("Dialogue Content", dialoguePanel.transform, "对白", 23, TextAnchor.UpperLeft, new Vector2(30f, -66f), new Vector2(920f, 84f), new Color32(225, 231, 232, 255));
            continueHintText = CreateText("Dialogue Continue Hint", dialoguePanel.transform, "点击或按空格继续", 14, TextAnchor.MiddleRight, new Vector2(710f, -152f), new Vector2(240f, 24f), new Color32(149, 163, 169, 255));
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
