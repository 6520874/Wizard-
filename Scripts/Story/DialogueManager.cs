using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    /// <summary>
    /// Plays ordered dialogue lines with speaker names.
    /// Can be reused by later chapters by passing a new DialogueLine array to StartDialogue.
    /// </summary>
    // 中文说明：管理 NPC 对话队列、对话框显示和对白结束后的回调。
    public class DialogueManager : MonoBehaviour
    {
        [Serializable]
        public struct DialogueLine
        {
            public string speakerName;
            [TextArea(2, 4)] public string text;
            public string portraitPath;

            public DialogueLine(string speakerName, string text)
            {
                this.speakerName = speakerName;
                this.text = text;
                portraitPath = string.Empty;
            }

            public DialogueLine(string speakerName, string text, string portraitPath)
            {
                this.speakerName = speakerName;
                this.text = text;
                this.portraitPath = portraitPath;
            }
        }

        [Header("Dialogue")]
        [SerializeField] private float typewriterCharactersPerSecond = 38f;
        [SerializeField] private bool startFirstQuestWhenDefaultDialogueEnds = true;

        [Header("Optional UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject speakerPortraitFrame;
        [SerializeField] private Image speakerPortraitImage;
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
        private static readonly Dictionary<string, Sprite> CachedPortraits = new Dictionary<string, Sprite>();

        private static readonly DialogueLine[] DefaultVillageDialogue =
        {
            new DialogueLine("老村长", "猎魔人……你终于来了。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("老村长", "三天前，矿洞里传来了女人的哭声。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("老村长", "进去的人，一个都没回来。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("老村长", "昨晚，我的小孙子也不见了。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("老村长", "他们说那是女妖，可我知道……那声音像极了我死去的女儿。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("猎魔人", "怪物不会无缘无故出现。", "Art/UI/GeraltPortrait.png"),
            new DialogueLine("猎魔人", "带我去最后一个失踪者出现的地方。", "Art/UI/GeraltPortrait.png"),
            new DialogueLine("猎魔人", "还有，准备好我的报酬。", "Art/UI/GeraltPortrait.png"),
            new DialogueLine("老村长", "只要你能救回孩子，村里最后的银币都给你。", "Art/Story/Npcs/OldVillageChief.png"),
            new DialogueLine("老村长", "但你要小心……这里死去的人，好像都还没真正离开。", "Art/Story/Npcs/OldVillageChief.png")
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

        public bool IsDialogueActive => dialogueActive;

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
            SetSpeakerPortrait(line.portraitPath);

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
            speakerPortraitFrame = CreatePanel("Dialogue Speaker Portrait Frame", dialoguePanel.transform, new Vector2(116f, 116f), new Vector2(28f, -36f), new Vector2(0f, 1f), new Color32(13, 16, 18, 255));
            speakerPortraitImage = CreateImage("Dialogue Speaker Portrait", speakerPortraitFrame.transform, new Vector2(104f, 104f), new Vector2(6f, -6f), new Vector2(0f, 1f), Color.white);
            speakerPortraitImage.preserveAspect = true;
            speakerPortraitImage.raycastTarget = false;
            speakerNameText = CreateText("Dialogue Speaker Name", dialoguePanel.transform, "角色名", 24, TextAnchor.MiddleLeft, new Vector2(164f, -20f), new Vector2(260f, 36f), new Color32(233, 222, 193, 255));
            dialogueText = CreateText("Dialogue Content", dialoguePanel.transform, "对白", 23, TextAnchor.UpperLeft, new Vector2(164f, -66f), new Vector2(780f, 84f), new Color32(225, 231, 232, 255));
            continueHintText = CreateText("Dialogue Continue Hint", dialoguePanel.transform, "点击或按空格继续", 14, TextAnchor.MiddleRight, new Vector2(710f, -152f), new Vector2(240f, 24f), new Color32(149, 163, 169, 255));
        }

        private void SetSpeakerPortrait(string relativePath)
        {
            if (speakerPortraitFrame == null || speakerPortraitImage == null)
            {
                return;
            }

            Sprite portrait = LoadPortrait(relativePath);
            speakerPortraitFrame.SetActive(portrait != null);
            speakerPortraitImage.sprite = portrait;
        }

        private static Sprite LoadPortrait(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (CachedPortraits.TryGetValue(relativePath, out Sprite cached))
            {
                return cached;
            }

            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"缺少角色头像图片：{relativePath}，请补充对应素材。");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Debug.LogWarning($"角色头像图片读取失败：{relativePath}");
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Rect portraitRect = relativePath.Contains("Art/Story/Npcs/")
                ? CalculateUpperBodyRect(texture)
                : new Rect(0f, 0f, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, portraitRect, new Vector2(0.5f, 0.5f), 360f);
            sprite.name = Path.GetFileNameWithoutExtension(relativePath) + "_DialoguePortrait_Runtime";
            CachedPortraits[relativePath] = sprite;
            return sprite;
        }

        private static Rect CalculateUpperBodyRect(Texture2D texture)
        {
            int minX = texture.width;
            int minY = texture.height;
            int maxX = 0;
            int maxY = 0;
            Color32[] pixels = texture.GetPixels32();
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a <= 16)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX <= minX || maxY <= minY)
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }

            int bodyHeight = maxY - minY;
            int cropBottom = Mathf.Clamp(minY + Mathf.RoundToInt(bodyHeight * 0.48f), 0, texture.height - 1);
            int paddingX = Mathf.RoundToInt((maxX - minX) * 0.14f);
            int paddingY = Mathf.RoundToInt(bodyHeight * 0.08f);
            int xMin = Mathf.Clamp(minX - paddingX, 0, texture.width - 1);
            int xMax = Mathf.Clamp(maxX + paddingX, xMin + 1, texture.width);
            int yMin = Mathf.Clamp(cropBottom - paddingY, 0, texture.height - 1);
            int yMax = Mathf.Clamp(maxY + paddingY, yMin + 1, texture.height);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
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

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
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
