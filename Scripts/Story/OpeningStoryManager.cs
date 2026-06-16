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
    // 中文说明：播放开场旁白剧情，并在结束后进入后续村庄流程。
    public class OpeningStoryManager : MonoBehaviour
    {
        private const string DefaultOpeningMusicPath = "Music/DarkPlace_Opening";

        [Header("Opening Narration")]
        [InspectorName("开场自动播放")]
        [SerializeField] private bool playOnStart = true;
        [Header("调试开局：0 = 正常开场；1 = 直接第一夜；2 = 直接第二夜；3 = 直接第三夜")]
        [Tooltip("0 = 正常开场；1 = 直接第一夜；2 = 直接第二夜；3 = 直接第三夜。")]
        [InspectorName("开局夜晚")]
        [SerializeField, Range(0, 3)] private int startNightOverride;
        [Tooltip("勾选后跳过黑屏旁白和村长对话，直接进入可操作状态。适合调试地图和回合制战斗。")]
        [InspectorName("跳过开场旁白和村长对话")]
        [SerializeField] private bool skipIntroNarrationAndDialogue;
        [Tooltip("跳过引导时是否仍然创建第一主线任务。")]
        [InspectorName("跳过时创建第一夜任务")]
        [SerializeField] private bool startFirstQuestWhenIntroSkipped = true;
        [InspectorName("打字速度（每秒字符数）")]
        [SerializeField] private float typewriterCharactersPerSecond = 28f;
        [InspectorName("开场旁白文本")]
        [SerializeField]
        [TextArea(2, 4)]
        private string[] narrationLines =
        {
            "黑月升起后的第七个冬天，北境的村庄开始一个接一个沉默。",
            "有人说，是瘟疫。",
            "有人说，是狼群。",
            "但猎魔人知道……真正会吃人的东西，往往披着人的皮。"
        };

        [Header("Opening Music")]
        [Tooltip("Resources 目录下的开场音乐路径，不需要扩展名。")]
        [SerializeField] private string openingMusicResourcePath = DefaultOpeningMusicPath;
        [SerializeField] private float openingMusicVolume = 0.42f;
        [SerializeField] private float musicFadeInSeconds = 1.8f;
        [SerializeField] private float musicFadeOutSeconds = 0.9f;

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
        private Coroutine musicRoutine;
        private AudioSource openingMusicSource;

        private void Awake()
        {
            EnsureOpeningUi();
            EnsureStoryManagers();
            openingPanel.SetActive(false);
        }

        private void Start()
        {
            if (startNightOverride > 0)
            {
                StartFromNightOverride();
                return;
            }

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

            string[] activeNarrationLines = StoryDatabase.GetOpeningNarration(narrationLines);
            if (activeNarrationLines == null || activeNarrationLines.Length == 0)
            {
                EnterVillageScene();
                return;
            }

            narrationLines = activeNarrationLines;
            narrationIndex = 0;
            openingActive = true;
            openingPanel.SetActive(true);
            PlayOpeningMusic();
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
            continueHintText.text = GameText.Opening.ContinueHint;

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
            FadeOutOpeningMusic();
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

            StopOpeningMusicImmediately();

            EnsureStoryManagers();
            DialogueManager activeDialogue = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            if (activeDialogue != null)
            {
                activeDialogue.HideDialogue();
            }

            if (startFirstQuestWhenIntroSkipped && questManager != null)
            {
                GeraltController player = FindObjectOfType<GeraltController>();
                NightContractManager.CreateIfMissing(player).BeginFirstNightContract();
            }
        }

        private void StartFromNightOverride()
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

            StopOpeningMusicImmediately();

            EnsureStoryManagers();
            DialogueManager activeDialogue = dialogueManager == null ? FindObjectOfType<DialogueManager>() : dialogueManager;
            if (activeDialogue != null)
            {
                activeDialogue.HideDialogue();
            }

            GeraltController player = FindObjectOfType<GeraltController>();
            NightContractManager nightContract = NightContractManager.CreateIfMissing(player);
            if (startNightOverride == 3)
            {
                nightContract.BeginThirdNightContract();
                return;
            }

            if (startNightOverride == 2)
            {
                nightContract.BeginSecondNightContract();
                return;
            }

            nightContract.BeginFirstNightContract();
        }

        private void PlayOpeningMusic()
        {
            AudioClip clip = Resources.Load<AudioClip>(openingMusicResourcePath);
            if (clip == null)
            {
                Debug.LogWarning(GameText.Opening.MissingMusic(openingMusicResourcePath));
                return;
            }

            EnsureOpeningMusicSource();
            if (openingMusicSource == null)
            {
                return;
            }

            if (musicRoutine != null)
            {
                StopCoroutine(musicRoutine);
            }

            openingMusicSource.clip = clip;
            openingMusicSource.loop = true;
            openingMusicSource.volume = 0f;
            openingMusicSource.Play();
            musicRoutine = StartCoroutine(FadeOpeningMusic(0f, openingMusicVolume, musicFadeInSeconds, false));
        }

        private void FadeOutOpeningMusic()
        {
            if (openingMusicSource == null)
            {
                return;
            }

            if (musicRoutine != null)
            {
                StopCoroutine(musicRoutine);
            }

            musicRoutine = StartCoroutine(FadeOpeningMusic(openingMusicSource.volume, 0f, musicFadeOutSeconds, true));
        }

        private void StopOpeningMusicImmediately()
        {
            if (musicRoutine != null)
            {
                StopCoroutine(musicRoutine);
                musicRoutine = null;
            }

            if (openingMusicSource != null)
            {
                openingMusicSource.Stop();
                openingMusicSource.clip = null;
                openingMusicSource.volume = 0f;
            }
        }

        private void EnsureOpeningMusicSource()
        {
            if (openingMusicSource != null)
            {
                return;
            }

            openingMusicSource = GetComponent<AudioSource>();
            if (openingMusicSource == null)
            {
                openingMusicSource = gameObject.AddComponent<AudioSource>();
            }

            openingMusicSource.playOnAwake = false;
            openingMusicSource.loop = true;
            openingMusicSource.spatialBlend = 0f;
            openingMusicSource.priority = 24;
        }

        private IEnumerator FadeOpeningMusic(float from, float to, float duration, bool stopWhenDone)
        {
            if (openingMusicSource == null)
            {
                yield break;
            }

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration && openingMusicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                openingMusicSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            if (openingMusicSource == null)
            {
                yield break;
            }

            openingMusicSource.volume = to;
            if (stopWhenDone)
            {
                openingMusicSource.Stop();
                openingMusicSource.clip = null;
            }

            musicRoutine = null;
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
            continueHintText = CreateText("Opening Continue Hint", openingPanel.transform, GameText.Opening.ContinueHint, 16, TextAnchor.MiddleCenter, new Vector2(0f, -210f), new Vector2(420f, 36f), new Color32(121, 134, 141, 255));
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
