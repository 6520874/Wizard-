using System;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：负责剧情 NPC 的靠近提示、按 E 对话以及主线任务目标推进。
    [RequireComponent(typeof(BoxCollider2D))]
    public class WitcherStoryNpcTrigger : MonoBehaviour
    {
        [SerializeField] private string npcDisplayName = "村民";
        [SerializeField] private int questObjectiveIndex = -1;
        [SerializeField] private DialogueManager.DialogueLine[] dialogueLines = Array.Empty<DialogueManager.DialogueLine>();

        private GeraltController player;
        private DialogueManager dialogueManager;
        private QuestManager questManager;
        private GameObject promptRoot;
        private Text promptText;
        private bool playerInRange;

        public void Configure(string displayName, int objectiveIndex, DialogueManager.DialogueLine[] lines)
        {
            npcDisplayName = displayName;
            questObjectiveIndex = objectiveIndex;
            dialogueLines = lines ?? Array.Empty<DialogueManager.DialogueLine>();
            if (promptText != null)
            {
                promptText.text = $"按 E 对话：{npcDisplayName}";
            }
        }

        private void Awake()
        {
            BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            if (triggerCollider.size == Vector2.zero)
            {
                triggerCollider.size = new Vector2(1.15f, 1.25f);
            }

            dialogueManager = FindObjectOfType<DialogueManager>();
            questManager = FindObjectOfType<QuestManager>();
            EnsurePromptUi();
            SetPromptVisible(false);
        }

        private void Update()
        {
            dialogueManager = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            bool dialogueOpen = dialogueManager != null && dialogueManager.IsDialogueActive;
            if (!playerInRange || dialogueOpen)
            {
                SetPromptVisible(false);
                return;
            }

            SetPromptVisible(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartNpcDialogue();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GeraltController candidate = other.GetComponent<GeraltController>();
            if (candidate == null)
            {
                return;
            }

            player = candidate;
            playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (player == null || other.gameObject != player.gameObject)
            {
                return;
            }

            playerInRange = false;
            SetPromptVisible(false);
        }

        private void StartNpcDialogue()
        {
            dialogueManager = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            questManager = questManager == null ? FindObjectOfType<QuestManager>() : questManager;
            if (dialogueManager == null || dialogueLines.Length == 0)
            {
                Debug.LogWarning($"剧情 NPC {npcDisplayName} 没有可播放的对话。");
                return;
            }

            SetPromptVisible(false);
            player?.SetControlEnabled(false);
            dialogueManager.StartDialogue(dialogueLines, () =>
            {
                if (questManager != null)
                {
                    if (questManager.ActiveQuest == null)
                    {
                        questManager.StartFirstMainQuest();
                    }

                    questManager.SetObjectiveCompleted(questObjectiveIndex, true);
                }

                player?.SetControlEnabled(true);
            });
        }

        private void EnsurePromptUi()
        {
            if (promptRoot != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Story Interaction Canvas", 132);
            promptRoot = new GameObject("Story NPC Prompt");
            promptRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = promptRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(280f, 44f);
            rootRect.anchoredPosition = new Vector2(0f, 168f);

            Image background = promptRoot.AddComponent<Image>();
            background.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 11, 220));
            background.color = new Color32(5, 8, 11, 220);
            Outline outline = promptRoot.AddComponent<Outline>();
            outline.effectColor = new Color32(146, 112, 68, 255);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject textObject = new GameObject("Story NPC Prompt Text");
            textObject.transform.SetParent(promptRoot.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            promptText = textObject.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.text = $"按 E 对话：{npcDisplayName}";
            promptText.fontSize = 20;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = new Color32(255, 226, 150, 255);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(visible);
            }
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
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
