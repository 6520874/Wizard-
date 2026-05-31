using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：管理经典 JRPG 对话框、逐字显示、角色控制暂停和对白结束回调。
    public class DialogueManager : MonoBehaviour
    {
        private const string ManagerName = "Dialogue Manager";

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

        private static DialogueManager instance;
        private DialogueLine[] activeLines = Array.Empty<DialogueLine>();
        private int lineIndex;
        private bool isTyping;
        private bool dialogueActive;
        private bool resumePlayerWhenFinished = true;
        private string currentFullText;
        private Coroutine typingRoutine;
        private Action onDialogueFinished;

        public static bool IsDialogueActive => instance != null && instance.dialogueActive;

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

        public static DialogueManager CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            DialogueManager existing = FindObjectOfType<DialogueManager>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            return new GameObject(ManagerName).AddComponent<DialogueManager>();
        }

        private void Awake()
        {
            instance = this;
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

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
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
                    questManager?.StartFirstMainQuest();
                }
            });
        }

        public void HideDialogue()
        {
            StopTyping();
            isTyping = false;
            dialogueActive = false;
            onDialogueFinished = null;
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            PlayerInputController.RefreshPlayerControl();
        }

        public void StartDialogue(DialogueLine[] lines, Action onFinished = null)
        {
            StartDialogue(lines, onFinished, true);
        }

        public void StartDialogue(DialogueLine[] lines, Action onFinished, bool resumePlayerWhenDialogueEnds)
        {
            if (lines == null || lines.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            StopTyping();
            activeLines = lines;
            onDialogueFinished = onFinished;
            resumePlayerWhenFinished = resumePlayerWhenDialogueEnds;
            lineIndex = 0;
            dialogueActive = true;
            dialoguePanel.SetActive(true);
            PlayerInputController.RefreshPlayerControl();
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
            speakerNameText.text = line.SpeakerName;
            currentFullText = line.Text;
            continueHintText.text = isTyping ? string.Empty : "Enter 继续";
            StopTyping();
            typingRoutine = StartCoroutine(TypeLine(currentFullText));
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            continueHintText.text = string.Empty;
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
            continueHintText.text = "Enter 继续";
        }

        private void FinishTypingImmediately()
        {
            StopTyping();
            dialogueText.text = currentFullText;
            isTyping = false;
            continueHintText.text = "Enter 继续";
        }

        private void EndDialogue()
        {
            StopTyping();
            dialogueActive = false;
            dialoguePanel.SetActive(false);
            Action finished = onDialogueFinished;
            onDialogueFinished = null;
            finished?.Invoke();
            if (resumePlayerWhenFinished)
            {
                PlayerInputController.RefreshPlayerControl();
            }
        }

        private void StopTyping()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }
        }

        private void EnsureDialogueUi()
        {
            if (dialoguePanel != null && speakerNameText != null && dialogueText != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Story UI Canvas", 160);
            dialoguePanel = CreatePanel("DialoguePanel", canvas.transform, new Vector2(980f, 196f), new Vector2(0f, 34f), new Vector2(0.5f, 0f), new Color32(5, 7, 10, 232));
            AddOutline(dialoguePanel, new Color32(117, 92, 52, 255), new Vector2(2f, -2f));
            CreatePanel("Dialogue Inner Bloodline", dialoguePanel.transform, new Vector2(920f, 2f), new Vector2(30f, -52f), new Vector2(0f, 1f), new Color32(113, 16, 24, 200));
            speakerNameText = CreateText("Dialogue Speaker Name", dialoguePanel.transform, "角色名", 24, TextAnchor.MiddleLeft, new Vector2(30f, -18f), new Vector2(260f, 36f), new Color32(238, 211, 150, 255));
            dialogueText = CreateText("Dialogue Content", dialoguePanel.transform, "对白", 23, TextAnchor.UpperLeft, new Vector2(30f, -68f), new Vector2(920f, 86f), new Color32(225, 224, 209, 255));
            continueHintText = CreateText("Dialogue Continue Hint", dialoguePanel.transform, "Enter 继续", 14, TextAnchor.MiddleRight, new Vector2(710f, -154f), new Vector2(240f, 24f), new Color32(155, 166, 166, 255));
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

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
