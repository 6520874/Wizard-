using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WitcherGame
{
    /// <summary>
    /// Plays the black-screen opening narration, then starts the first village dialogue.
    /// Attach this script to an empty GameObject in the first scene.
    /// </summary>
    public class OpeningStoryManager : MonoBehaviour
    {
        [Header("Opening Narration")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("勾选后跳过黑屏旁白和村长对话，直接进入可操作状态。适合调试割草战斗。")]
        [SerializeField] private bool skipIntroNarrationAndDialogue;
        [Tooltip("跳过引导时是否仍然创建第一主线任务。")]
        [SerializeField] private bool startFirstQuestWhenIntroSkipped = true;
        [SerializeField] private float typewriterCharactersPerSecond = 28f;
        [SerializeField]
        [TextArea(2, 4)]
        private string[] narrationLines =
        {
            "黑月升起后的第七个冬天，北境的村庄开始一个接一个沉默。",
            "有人说，是瘟疫。",
            "有人说，是狼群。",
            "但猎魔人知道……真正会吃人的东西，往往披着人的皮。"
        };

        [Header("Village Scene")]
        [Tooltip("Optional. Leave empty to stay in the current scene and immediately play the village NPC dialogue.")]
        [SerializeField] private string villageSceneName = string.Empty;

        [Header("Optional UI References")]
        [SerializeField] private GameObject openingPanel;
        [SerializeField] private Text narrationText;
        [SerializeField] private Text continueHintText;

        [Header("Optional Manager References")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private QuestManager questManager;

        private int narrationIndex;
        private bool isTyping;
        private bool openingActive;
        private string currentFullText;
        private Coroutine typingRoutine;

        private void Awake()
        {
            EnsureOpeningUi();
            EnsureStoryManagers();
            openingPanel.SetActive(false);
        }

        private void Start()
        {
            if (skipIntroNarrationAndDialogue)
            {
                SkipIntro();
                return;
            }

            if (playOnStart)
            {
                BeginOpening();
            }
        }

        private void Update()
        {
            if (!openingActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                AdvanceNarration();
            }
        }

        public void BeginOpening()
        {
            if (skipIntroNarrationAndDialogue)
            {
                SkipIntro();
                return;
            }

            if (narrationLines == null || narrationLines.Length == 0)
            {
                EnterVillageScene();
                return;
            }

            narrationIndex = 0;
            openingActive = true;
            openingPanel.SetActive(true);
            ShowNarrationLine(narrationLines[narrationIndex]);
        }

        private void AdvanceNarration()
        {
            if (isTyping)
            {
                FinishTypingImmediately();
                return;
            }

            narrationIndex++;
            if (narrationIndex >= narrationLines.Length)
            {
                EndOpening();
                return;
            }

            ShowNarrationLine(narrationLines[narrationIndex]);
        }

        private void ShowNarrationLine(string line)
        {
            currentFullText = line;
            continueHintText.text = "点击或按空格继续";

            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            typingRoutine = StartCoroutine(TypeLine(line));
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            narrationText.text = string.Empty;
            float delay = typewriterCharactersPerSecond <= 0f ? 0f : 1f / typewriterCharactersPerSecond;

            for (int i = 0; i < line.Length; i++)
            {
                narrationText.text += line[i];
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

            narrationText.text = currentFullText;
            isTyping = false;
        }

        private void EndOpening()
        {
            openingActive = false;
            openingPanel.SetActive(false);
            EnterVillageScene();
        }

        private void EnterVillageScene()
        {
            if (!string.IsNullOrWhiteSpace(villageSceneName) && SceneManager.GetActiveScene().name != villageSceneName)
            {
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnVillageSceneLoaded;
                SceneManager.LoadScene(villageSceneName);
                return;
            }

            StartVillageIntroDialogue();
        }

        private void OnVillageSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnVillageSceneLoaded;
            EnsureStoryManagers();
            StartVillageIntroDialogue();
        }

        private void StartVillageIntroDialogue()
        {
            EnsureStoryManagers();
            if (dialogueManager != null)
            {
                dialogueManager.StartDefaultVillageDialogue();
            }
        }

        private void SkipIntro()
        {
            openingActive = false;
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            if (openingPanel != null)
            {
                openingPanel.SetActive(false);
            }

            EnsureStoryManagers();
            DialogueManager activeDialogue = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            if (activeDialogue != null)
            {
                activeDialogue.HideDialogue();
            }

            if (startFirstQuestWhenIntroSkipped && questManager != null)
            {
                questManager.StartFirstMainQuest();
            }
        }

        private void EnsureStoryManagers()
        {
            questManager = questManager == null ? FindObjectOfType<QuestManager>() : questManager;
            if (questManager == null)
            {
                questManager = new GameObject("Quest Manager").AddComponent<QuestManager>();
            }

            dialogueManager = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            if (dialogueManager == null)
            {
                dialogueManager = new GameObject("Dialogue Manager").AddComponent<DialogueManager>();
            }
        }

        private void EnsureOpeningUi()
        {
            if (openingPanel != null && narrationText != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Story UI Canvas", 120);
            openingPanel = CreateFullScreenPanel("OpeningPanel", canvas.transform, new Color32(0, 0, 0, 255));
            narrationText = CreateText("Opening Narration Text", openingPanel.transform, string.Empty, 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1080f, 220f), new Color32(220, 228, 231, 255));
            continueHintText = CreateText("Opening Continue Hint", openingPanel.transform, "点击或按空格继续", 16, TextAnchor.MiddleCenter, new Vector2(0f, -210f), new Vector2(420f, 36f), new Color32(121, 134, 141, 255));
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

        private static GameObject CreateFullScreenPanel(string name, Transform parent, Color32 color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

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
