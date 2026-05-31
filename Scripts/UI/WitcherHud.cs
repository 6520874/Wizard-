using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：管理地图界面的头像、生命魔法条、首领血条和失败面板。
    public class WitcherHud : MonoBehaviour
    {
        private const string HudName = "Witcher HUD";
        private const string PortraitPath = "Art/UI/GeraltPortrait.png";
        private const string HudFramePath = "Art/UI/DarkHudReferenceFull.png";
        private const float TopLeftHudScale = 0.74f;
        private const float PlayerBarWidth = 384f;
        private const float ManaBarWidth = 384f;
        private static Sprite cachedHealthFillSprite;
        private static Sprite cachedManaFillSprite;

        private GeraltController player;
        private Image healthFill;
        private Image manaFill;
        private Text healthText;
        private Text manaText;
        private Text roomText;
        private Text goldValueText;
        private Text experienceValueText;
        private Text attackValueText;
        private Text defenseValueText;
        private GeraltController subscribedPlayer;
        private PlayerInventory playerInventory;
        private PlayerInventory subscribedInventory;
        private GameObject bossStatusRoot;
        private Image bossHealthFill;
        private Text bossHealthText;
        private GameObject gameOverRoot;

        public static WitcherHud CreateIfMissing(GeraltController target)
        {
            WitcherHud existing = FindObjectOfType<WitcherHud>();
            if (existing != null)
            {
                existing.SetPlayer(target);
                existing.BuildHud();
                return existing;
            }

            GameObject hudObject = new GameObject(HudName);
            WitcherHud hud = hudObject.AddComponent<WitcherHud>();
            hud.SetPlayer(target);
            return hud;
        }

        public void SetPlayer(GeraltController target)
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.StatsChanged -= UpdateBars;
            }

            if (subscribedInventory != null)
            {
                subscribedInventory.InventoryChanged -= UpdateInventoryText;
            }

            player = target;
            subscribedPlayer = target;
            playerInventory = null;
            subscribedInventory = null;
            if (subscribedPlayer != null)
            {
                subscribedPlayer.StatsChanged += UpdateBars;
            }

            BindInventory();
        }

        public void SetRoomName(string roomName)
        {
            if (roomText != null)
            {
                roomText.text = roomName;
            }
        }

        private void Awake()
        {
            BuildHud();
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
                if (player == null)
                {
                    return;
                }
            }

            if (gameOverRoot != null && gameOverRoot.activeSelf && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)))
            {
                RestartCurrentScene();
                return;
            }

            UpdateBars();
        }

        private void BuildHud()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            EnsureEventSystem();
            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRect = gameObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = Vector2.zero;

            GameObject root = CreateUiObject("TopLeft Status", transform, new Vector2(650f, 304f), new Vector2(8f, -8f), new Vector2(0f, 1f));
            root.transform.localScale = Vector3.one * TopLeftHudScale;
            Image panel = root.AddComponent<Image>();
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 0));
            panel.color = new Color32(0, 0, 0, 0);

            Image frame = CreateImage("Dark HUD Reference Frame", root.transform, new Vector2(650f, 304f), Vector2.zero, Color.white);
            frame.sprite = LoadSprite(HudFramePath, 100f, new Rect(0f, 511f, 920f, 430f));
            frame.preserveAspect = true;
            frame.raycastTarget = false;

            Image portrait = CreateImage("Geralt Portrait", root.transform, new Vector2(110f, 110f), new Vector2(38f, -52f), Color.white);
            portrait.sprite = LoadSprite(PortraitPath, 96f);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            healthFill = CreateReferenceFill("Health Runtime Fill", root.transform, new Vector2(PlayerBarWidth, 20f), new Vector2(198f, -76f), new Color32(139, 16, 24, 238), GetHealthFillSprite());
            manaFill = CreateReferenceFill("Mana Runtime Fill", root.transform, new Vector2(ManaBarWidth, 20f), new Vector2(198f, -146f), new Color32(37, 70, 132, 236), GetManaFillSprite());

            healthText = CreateText("Health Value", root.transform, "HP 100 / 100", 21, TextAnchor.MiddleLeft, new Vector2(220f, -76f), new Vector2(240f, 28f));
            healthText.color = new Color32(255, 250, 232, 255);
            AddOutline(healthText, new Color32(0, 0, 0, 255), new Vector2(2f, -2f));

            manaText = CreateText("Mana Value", root.transform, "MP 100 / 100", 21, TextAnchor.MiddleLeft, new Vector2(220f, -146f), new Vector2(230f, 28f));
            manaText.color = new Color32(255, 250, 232, 255);
            AddOutline(manaText, new Color32(0, 0, 0, 255), new Vector2(2f, -2f));

            roomText = CreateText("Room Label", root.transform, "霜林边境", 12, TextAnchor.MiddleRight, new Vector2(480f, -18f), new Vector2(150f, 20f));
            roomText.color = new Color32(152, 178, 188, 255);
            AddOutline(roomText, new Color32(0, 0, 0, 220), new Vector2(1f, -1f));

            goldValueText = CreateStatusValueText("Gold Value", root.transform, new Vector2(119f, -260f));
            experienceValueText = CreateStatusValueText("Experience Value", root.transform, new Vector2(250f, -260f));
            attackValueText = CreateStatusValueText("Attack Bonus Value", root.transform, new Vector2(381f, -260f));
            defenseValueText = CreateStatusValueText("Defense Bonus Value", root.transform, new Vector2(512f, -260f));

            BuildBossBar();
            BuildGameOverPanel();
            UpdateBars();
        }

        public void ShowDefeatScreen()
        {
            if (gameOverRoot == null)
            {
                BuildGameOverPanel();
            }

            gameOverRoot.SetActive(true);
        }

        private void UpdateBars()
        {
            if (player == null || healthText == null || manaText == null)
            {
                return;
            }

            float health01 = player.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth);
            float mana01 = player.MaxMana <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentMana / player.MaxMana);

            SetFill(healthFill, health01, PlayerBarWidth);
            SetFill(manaFill, mana01, ManaBarWidth);
            healthText.text = $"HP {player.CurrentHealth} / {player.MaxHealth}";
            manaText.text = $"MP {player.CurrentMana} / {player.MaxMana}";
            UpdateInventoryText();
            UpdateBossBar();
        }

        private void OnDestroy()
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.StatsChanged -= UpdateBars;
            }

            if (subscribedInventory != null)
            {
                subscribedInventory.InventoryChanged -= UpdateInventoryText;
            }
        }

        private void BindInventory()
        {
            if (player == null)
            {
                return;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || inventory == subscribedInventory)
            {
                playerInventory = inventory;
                return;
            }

            if (subscribedInventory != null)
            {
                subscribedInventory.InventoryChanged -= UpdateInventoryText;
            }

            playerInventory = inventory;
            subscribedInventory = inventory;
            subscribedInventory.InventoryChanged += UpdateInventoryText;
        }

        private void UpdateInventoryText()
        {
            if (goldValueText == null || experienceValueText == null || attackValueText == null || defenseValueText == null)
            {
                return;
            }

            BindInventory();
            if (playerInventory == null)
            {
                goldValueText.text = "0";
                experienceValueText.text = "0";
                attackValueText.text = "+0";
                defenseValueText.text = "+0";
                return;
            }

            goldValueText.text = playerInventory.Gold.ToString();
            experienceValueText.text = playerInventory.Experience.ToString();
            attackValueText.text = $"+{playerInventory.AttackBonus}";
            defenseValueText.text = $"+{playerInventory.DefenseBonus}";
        }

        private void BuildBossBar()
        {
            bossStatusRoot = CreateUiObject("Boss Status", transform, new Vector2(430f, 52f), new Vector2(0f, -22f), new Vector2(0.5f, 1f));
            Image panel = bossStatusRoot.AddComponent<Image>();
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 9, 14, 205));
            panel.color = new Color32(5, 9, 14, 205);

            Text title = CreateText("Boss Name", bossStatusRoot.transform, "狂猎统领", 15, TextAnchor.MiddleLeft, new Vector2(14f, -6f), new Vector2(160f, 20f));
            title.color = new Color32(207, 231, 245, 255);

            Image back = CreateImage("Boss Health Back", bossStatusRoot.transform, new Vector2(392f, 16f), new Vector2(18f, -28f), new Color32(2, 4, 8, 240));
            back.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(2, 4, 8, 240));

            bossHealthFill = CreateImage("Boss Health Fill", back.transform, new Vector2(386f, 10f), new Vector2(3f, -3f), new Color32(83, 184, 232, 255));
            RectTransform fillRect = bossHealthFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            bossHealthFill.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(83, 184, 232, 255));

            bossHealthText = CreateText("Boss Health Value", bossStatusRoot.transform, "8/8", 13, TextAnchor.MiddleRight, new Vector2(316f, -5f), new Vector2(92f, 20f));
            bossHealthText.color = new Color32(236, 246, 251, 255);
            bossStatusRoot.SetActive(false);
        }

        private void BuildGameOverPanel()
        {
            if (gameOverRoot != null)
            {
                Destroy(gameOverRoot);
            }

            gameOverRoot = CreateUiObject("Defeat Overlay", transform, new Vector2(520f, 260f), Vector2.zero, new Vector2(0.5f, 0.5f));

            Image dim = CreateImage("Defeat Screen Dim", gameOverRoot.transform, new Vector2(2400f, 1400f), Vector2.zero, new Color32(0, 0, 0, 138));
            CenterRect(dim.rectTransform, Vector2.zero, new Vector2(2400f, 1400f));
            dim.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 138));

            Image panel = CreateCenteredImage("Defeat Panel", gameOverRoot.transform, new Vector2(520f, 260f), Vector2.zero, new Color32(15, 18, 22, 236));
            CenterRect(panel.rectTransform, Vector2.zero, new Vector2(520f, 260f));
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(15, 18, 22, 236));
            AddOutline(panel, new Color32(130, 31, 33, 255), new Vector2(4f, -4f));

            Text title = CreateText("Defeat Title", panel.transform, "你失败了", 48, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 70f));
            CenterRect(title.rectTransform, new Vector2(0f, 68f), new Vector2(520f, 70f));
            title.color = new Color32(255, 70, 64, 255);
            AddOutline(title, new Color32(0, 0, 0, 255), new Vector2(3f, -3f));

            Text subtitle = CreateText("Defeat Subtitle", panel.transform, "猎魔人的道路还没有结束", 19, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 34f));
            CenterRect(subtitle.rectTransform, new Vector2(0f, 12f), new Vector2(520f, 34f));
            subtitle.color = new Color32(226, 219, 199, 255);
            AddOutline(subtitle, new Color32(0, 0, 0, 240), new Vector2(2f, -2f));

            Button retryButton = CreateButton("Retry Button", panel.transform, "再来一次", new Vector2(0f, -72f), new Vector2(190f, 50f));
            retryButton.onClick.AddListener(RestartCurrentScene);

            gameOverRoot.SetActive(false);
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            Image image = CreateCenteredImage(name, parent, size, position, new Color32(96, 18, 22, 255));
            CenterRect(image.rectTransform, position, size);
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(96, 18, 22, 255));
            AddOutline(image, new Color32(225, 172, 90, 255), new Vector2(2f, -2f));

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(126, 22, 27, 255);
            colors.highlightedColor = new Color32(178, 39, 42, 255);
            colors.pressedColor = new Color32(70, 10, 16, 255);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text text = CreateText(name + " Text", image.transform, label, 22, TextAnchor.MiddleCenter, Vector2.zero, size);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.color = new Color32(255, 241, 210, 255);
            AddOutline(text, new Color32(0, 0, 0, 255), new Vector2(2f, -2f));
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void CenterRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        private void UpdateBossBar()
        {
            if (bossStatusRoot == null)
            {
                return;
            }

            bossStatusRoot.SetActive(false);
        }

        private static void SetFill(Image image, float normalizedValue, float maxWidth)
        {
            if (image == null)
            {
                return;
            }

            if (image.type == Image.Type.Filled)
            {
                image.fillAmount = normalizedValue;
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(maxWidth * normalizedValue, rect.sizeDelta.y);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateReferenceFill(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            return CreateReferenceFill(name, parent, size, position, color, WitcherSpriteLibrary.GetSolidSprite(color));
        }

        private static Image CreateReferenceFill(string name, Transform parent, Vector2 size, Vector2 position, Color32 color, Sprite sprite)
        {
            Image image = CreateImage(name, parent, size, position, color);
            image.sprite = sprite != null ? sprite : WitcherSpriteLibrary.GetSolidSprite(color);
            image.color = sprite != null ? Color.white : (Color)color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            return image;
        }

        private static Image CreateCenteredImage(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateStatusValueText(string name, Transform parent, Vector2 position)
        {
            Text text = CreateText(name, parent, "0", 16, TextAnchor.MiddleCenter, position, new Vector2(96f, 24f));
            text.color = new Color32(226, 198, 132, 255);
            AddOutline(text, new Color32(0, 0, 0, 245), new Vector2(1f, -1f));
            return text;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Text textComponent = obj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = anchor;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Sprite GetHealthFillSprite()
        {
            if (cachedHealthFillSprite == null)
            {
                cachedHealthFillSprite = CreateBarFillSprite(
                    new Color32(70, 6, 11, 255),
                    new Color32(146, 16, 24, 255),
                    new Color32(196, 46, 38, 255),
                    new Color32(255, 150, 112, 58));
            }

            return cachedHealthFillSprite;
        }

        private static Sprite GetManaFillSprite()
        {
            if (cachedManaFillSprite == null)
            {
                cachedManaFillSprite = CreateBarFillSprite(
                    new Color32(7, 17, 40, 255),
                    new Color32(28, 58, 118, 255),
                    new Color32(54, 83, 150, 255),
                    new Color32(127, 174, 214, 52));
            }

            return cachedManaFillSprite;
        }

        private static Sprite CreateBarFillSprite(Color32 dark, Color32 middle, Color32 bright, Color32 highlight)
        {
            const int width = 256;
            const int height = 24;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float vertical = height <= 1 ? 0f : (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    float horizontal = width <= 1 ? 0f : (float)x / (width - 1);
                    Color baseColor = Color.Lerp(dark, middle, Mathf.Clamp01(horizontal * 1.25f));
                    baseColor = Color.Lerp(baseColor, bright, Mathf.Clamp01((1f - Mathf.Abs(vertical - 0.62f) * 2.1f) * 0.34f));
                    int noise = ((x * 13 + y * 31 + (x / 7) * 17) & 15) - 7;
                    float scratch = ((x + y * 5) % 29 == 0 || (x * 3 + y) % 47 == 0) ? -0.16f : 0f;
                    float edgeShade = vertical < 0.14f || vertical > 0.88f ? -0.22f : 0f;
                    baseColor *= Mathf.Clamp01(1f + noise / 72f + scratch + edgeShade);
                    if (y == height - 5 || y == height - 6)
                    {
                        baseColor = Color.Lerp(baseColor, highlight, highlight.a / 255f);
                    }

                    texture.SetPixel(x, y, baseColor);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return obj;
        }

        private static Sprite LoadSprite(string assetRelativePath, float pixelsPerUnit)
        {
            return LoadSprite(assetRelativePath, pixelsPerUnit, Rect.zero);
        }

        private static Sprite LoadSprite(string assetRelativePath, float pixelsPerUnit, Rect cropRect)
        {
            string absolutePath = Path.Combine(Application.dataPath, assetRelativePath);
            if (!File.Exists(absolutePath))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(23, 28, 34, 255));
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(23, 28, 34, 255));
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Rect spriteRect = cropRect.width > 0f && cropRect.height > 0f
                ? cropRect
                : new Rect(0f, 0f, texture.width, texture.height);
            return Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
