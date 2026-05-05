using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WitcherGame
{
    public class WitcherHud : MonoBehaviour
    {
        private const string HudName = "Witcher HUD";
        private const string PortraitPath = "Art/UI/GeraltPortrait.png";
        private const string HudFramePath = "Art/UI/DarkHudReferenceFull.png";
        private const float PlayerBarWidth = 382f;
        private const float ManaBarWidth = 352f;

        private GeraltController player;
        private Image healthFill;
        private Image manaFill;
        private Image healthMissing;
        private Image manaMissing;
        private Text healthText;
        private Text manaText;
        private Text roomText;
        private GeraltController subscribedPlayer;
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

            player = target;
            subscribedPlayer = target;
            if (subscribedPlayer != null)
            {
                subscribedPlayer.StatsChanged += UpdateBars;
            }
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

            GameObject root = CreateUiObject("TopLeft Status", transform, new Vector2(620f, 303f), new Vector2(8f, -8f), new Vector2(0f, 1f));
            Image panel = root.AddComponent<Image>();
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 0));
            panel.color = new Color32(0, 0, 0, 0);

            Image frame = CreateImage("Dark HUD Reference Frame", root.transform, new Vector2(620f, 303f), Vector2.zero, new Color32(255, 255, 255, 188));
            frame.sprite = LoadSprite(HudFramePath, 100f, new Rect(0f, 511f, 880f, 430f));
            frame.preserveAspect = true;

            Image portraitCover = CreateImage("Portrait Cover", root.transform, new Vector2(118f, 118f), new Vector2(34f, -47f), new Color32(2, 4, 6, 226));
            portraitCover.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(2, 4, 6, 226));

            Image portrait = CreateImage("Geralt Portrait", root.transform, new Vector2(108f, 108f), new Vector2(39f, -52f), Color.white);
            portrait.sprite = LoadSprite(PortraitPath, 96f);
            portrait.preserveAspect = true;

            Image healthTrack = CreateImage("Health Dynamic Track", root.transform, new Vector2(PlayerBarWidth, 34f), new Vector2(158f, -72f), new Color32(48, 6, 10, 245));
            healthTrack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(48, 6, 10, 245));

            Image manaTrack = CreateImage("Mana Dynamic Track", root.transform, new Vector2(ManaBarWidth, 29f), new Vector2(158f, -141f), new Color32(3, 19, 57, 245));
            manaTrack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(3, 19, 57, 245));

            healthFill = CreateReferenceFill("Health Runtime Fill", root.transform, new Vector2(PlayerBarWidth, 34f), new Vector2(158f, -72f), new Color32(239, 20, 33, 222));
            manaFill = CreateReferenceFill("Mana Runtime Fill", root.transform, new Vector2(ManaBarWidth, 29f), new Vector2(158f, -141f), new Color32(28, 132, 255, 222));
            healthMissing = CreateRightAnchoredImage("Health Missing Mask", root.transform, new Vector2(PlayerBarWidth, 38f), new Vector2(540f, -70f), new Color32(18, 18, 18, 235));
            manaMissing = CreateRightAnchoredImage("Mana Missing Mask", root.transform, new Vector2(ManaBarWidth, 32f), new Vector2(510f, -139f), new Color32(12, 17, 24, 235));

            Image healthTextCover = CreateImage("Health Original Text Cover", root.transform, new Vector2(230f, 31f), new Vector2(176f, -77f), new Color32(67, 4, 8, 225));
            healthTextCover.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(67, 4, 8, 225));

            Image manaTextCover = CreateImage("Mana Original Text Cover", root.transform, new Vector2(218f, 28f), new Vector2(176f, -147f), new Color32(3, 25, 75, 225));
            manaTextCover.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(3, 25, 75, 225));

            healthText = CreateText("Health Value", root.transform, "HP 100 / 100", 23, TextAnchor.MiddleLeft, new Vector2(187f, -76f), new Vector2(240f, 30f));
            healthText.color = new Color32(255, 250, 232, 255);
            AddOutline(healthText, new Color32(0, 0, 0, 255), new Vector2(2f, -2f));

            manaText = CreateText("Mana Value", root.transform, "MP 100 / 100", 23, TextAnchor.MiddleLeft, new Vector2(187f, -146f), new Vector2(230f, 30f));
            manaText.color = new Color32(255, 250, 232, 255);
            AddOutline(manaText, new Color32(0, 0, 0, 255), new Vector2(2f, -2f));

            roomText = CreateText("Room Label", root.transform, "霜林边境", 12, TextAnchor.MiddleRight, new Vector2(420f, -24f), new Vector2(130f, 20f));
            roomText.color = new Color32(152, 178, 188, 255);
            AddOutline(roomText, new Color32(0, 0, 0, 220), new Vector2(1f, -1f));

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

        private void BuildPortraitBadge(Transform parent)
        {
            Image halo = CreateImage("Portrait Halo", parent, new Vector2(132f, 132f), new Vector2(18f, -10f), new Color32(8, 10, 12, 240));
            halo.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 10, 12, 240));
            AddOutline(halo, new Color32(93, 87, 77, 255), new Vector2(4f, -4f));

            Image ring = CreateImage("Portrait Ring", parent, new Vector2(116f, 116f), new Vector2(26f, -18f), new Color32(19, 22, 24, 255));
            ring.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(19, 22, 24, 255));
            AddOutline(ring, new Color32(151, 126, 78, 255), new Vector2(2f, -2f));

            Image redCore = CreateImage("Portrait Red Core", parent, new Vector2(102f, 102f), new Vector2(33f, -25f), new Color32(63, 5, 9, 220));
            redCore.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(63, 5, 9, 220));

            Image portraitBack = CreateImage("Portrait Back", parent, new Vector2(88f, 88f), new Vector2(40f, -32f), new Color32(3, 5, 7, 255));
            portraitBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(3, 5, 7, 255));

            Image portrait = CreateImage("Geralt Portrait", parent, new Vector2(82f, 82f), new Vector2(43f, -35f), Color.white);
            portrait.sprite = LoadSprite(PortraitPath, 96f);
            portrait.preserveAspect = true;

            CreateDiamond("Top Ruby", parent, new Vector2(74f, -1f), 28f, new Color32(229, 24, 35, 255), new Color32(30, 3, 5, 255));
            CreateDiamond("Bottom Spike", parent, new Vector2(74f, -132f), 22f, new Color32(48, 44, 39, 255), new Color32(12, 12, 12, 255));
            CreateSpike("Left Wing", parent, new Vector2(6f, -60f), new Vector2(34f, 12f), 18f);
            CreateSpike("Right Wing", parent, new Vector2(140f, -60f), new Vector2(34f, 12f), -18f);
        }

        private Image CreateBar(Transform parent, string name, string label, Vector2 position, Color32 fillColor, Color32 lowColor, out Text valueText)
        {
            Text labelText = CreateText(name + " Label", parent, label, 22, TextAnchor.MiddleLeft, position + new Vector2(22f, -2f), new Vector2(58f, 30f));
            labelText.color = new Color32(229, 218, 185, 255);
            AddOutline(labelText, new Color32(0, 0, 0, 245), new Vector2(2f, -2f));

            Image railShadow = CreateImage(name + " Rail Shadow", parent, new Vector2(PlayerBarWidth + 54f, 42f), position + new Vector2(0f, 0f), new Color32(8, 12, 17, 118));
            railShadow.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 12, 17, 118));

            Image outer = CreateImage(name + " Outer Frame", parent, new Vector2(PlayerBarWidth + 42f, 32f), position + new Vector2(6f, -5f), new Color32(52, 48, 43, 245));
            outer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(55, 50, 45, 255));
            AddOutline(outer, new Color32(7, 8, 9, 255), new Vector2(2f, -2f));

            Image leftCap = CreateImage(name + " Left Cap", parent, new Vector2(16f, 42f), position + new Vector2(-4f, -1f), new Color32(25, 23, 22, 255));
            leftCap.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(25, 23, 22, 255));
            AddOutline(leftCap, new Color32(117, 103, 74, 255), new Vector2(1f, -1f));

            Image rightArrow = CreateCenteredImage(name + " Arrow Head", parent, new Vector2(45f, 34f), position + new Vector2(PlayerBarWidth + 42f, -22f), new Color32(39, 36, 34, 255));
            rightArrow.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(39, 36, 34, 255));
            rightArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(rightArrow, new Color32(119, 104, 76, 255), new Vector2(1f, -1f));

            Image back = CreateImage(name + " Back", outer.transform, new Vector2(PlayerBarWidth, 22f), new Vector2(18f, -5f), new Color32(2, 4, 7, 255));
            back.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(2, 4, 7, 255));

            Image lowFill = CreateImage(name + " Low Fill", back.transform, new Vector2(PlayerBarWidth - 6f, 16f), new Vector2(3f, -3f), lowColor);
            lowFill.sprite = WitcherSpriteLibrary.GetSolidSprite(lowColor);

            Image fill = CreateImage(name + " Fill", back.transform, new Vector2(PlayerBarWidth - 6f, 16f), new Vector2(3f, -3f), fillColor);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fill.sprite = WitcherSpriteLibrary.GetSolidSprite(fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            Image highlight = CreateImage(name + " Highlight", back.transform, new Vector2(PlayerBarWidth - 6f, 5f), new Vector2(3f, -3f), new Color32(255, 255, 255, 70));
            highlight.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 255, 255, 45));
            highlight.raycastTarget = false;

            CreateDiamond(name + " Gem", parent, position + new Vector2(PlayerBarWidth + 31f, -6f), 20f, fillColor, new Color32(7, 8, 10, 255));

            valueText = CreateText(name + " Value", outer.transform, "100/100", 22, TextAnchor.MiddleLeft, new Vector2(92f, 0f), new Vector2(PlayerBarWidth - 110f, 28f));
            valueText.color = new Color32(255, 248, 228, 255);
            AddOutline(valueText, new Color32(0, 0, 0, 250), new Vector2(2f, -2f));
            return fill;
        }

        private void BuildSkillSlots(Transform parent)
        {
            CreateSkillSlot(parent, 0, "盾", new Color32(42, 151, 250, 255), "15 s");
            CreateSkillSlot(parent, 1, "火", new Color32(255, 93, 25, 255), "22 s");
            CreateSkillSlot(parent, 2, "冰", new Color32(89, 210, 255, 255), "30 s");
            CreateSkillSlot(parent, 3, "印", new Color32(155, 93, 255, 255), "25%");
        }

        private void CreateSkillSlot(Transform parent, int index, string iconText, Color32 iconColor, string cooldownText)
        {
            Vector2 position = new Vector2(192f + index * 86f, -148f);
            Image frame = CreateImage("Skill Slot " + index, parent, new Vector2(70f, 70f), position, new Color32(9, 10, 12, 235));
            frame.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(9, 10, 12, 235));
            AddOutline(frame, new Color32(105, 94, 73, 255), new Vector2(2f, -2f));

            Image inset = CreateImage("Skill Slot Inset " + index, frame.transform, new Vector2(56f, 46f), new Vector2(7f, -8f), new Color32(5, 8, 12, 255));
            inset.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 255));

            Text icon = CreateText("Skill Icon " + index, frame.transform, iconText, 27, TextAnchor.MiddleCenter, new Vector2(7f, -7f), new Vector2(56f, 44f));
            icon.color = iconColor;
            AddOutline(icon, new Color32(0, 0, 0, 245), new Vector2(2f, -2f));

            Text cooldown = CreateText("Skill Cooldown " + index, frame.transform, cooldownText, 15, TextAnchor.MiddleCenter, new Vector2(5f, -48f), new Vector2(60f, 18f));
            cooldown.color = new Color32(214, 255, 204, 255);
            AddOutline(cooldown, new Color32(0, 0, 0, 245), new Vector2(1f, -1f));

            CreateDiamond("Skill Top Gem " + index, frame.transform, new Vector2(35f, 4f), 10f, iconColor, new Color32(7, 8, 10, 255));
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
            SetMissing(healthMissing, health01, PlayerBarWidth);
            SetMissing(manaMissing, mana01, ManaBarWidth);
            healthText.text = $"HP {player.CurrentHealth} / {player.MaxHealth}";
            manaText.text = $"MP {player.CurrentMana} / {player.MaxMana}";
            UpdateBossBar();
        }

        private void OnDestroy()
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.StatsChanged -= UpdateBars;
            }
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
            WildHuntBossController boss = FindObjectOfType<WildHuntBossController>();
            bool shouldShow = boss != null && boss.HasSpawned && boss.CurrentHealth > 0;
            bossStatusRoot.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            float health01 = boss.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth);
            SetFill(bossHealthFill, health01, 386f);
            bossHealthText.text = $"{boss.CurrentHealth}/{boss.MaxHealth}";
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

        private static void SetMissing(Image image, float normalizedValue, float maxWidth)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(maxWidth * (1f - normalizedValue), rect.sizeDelta.y);
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
            Image image = CreateImage(name, parent, size, position, color);
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
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

        private static Image CreateRightAnchoredImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(1f, 1f);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
            return image;
        }

        private static Image CreateDiamond(string name, Transform parent, Vector2 position, float size, Color32 fillColor, Color32 outlineColor)
        {
            Image outline = CreateCenteredImage(name + " Outline", parent, new Vector2(size + 8f, size + 8f), position, outlineColor);
            outline.sprite = WitcherSpriteLibrary.GetSolidSprite(outlineColor);
            outline.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image diamond = CreateCenteredImage(name, parent, new Vector2(size, size), position, fillColor);
            diamond.sprite = WitcherSpriteLibrary.GetSolidSprite(fillColor);
            diamond.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
            return diamond;
        }

        private static void CreateSpike(string name, Transform parent, Vector2 position, Vector2 size, float rotation)
        {
            Image spike = CreateCenteredImage(name, parent, size, position, new Color32(38, 36, 33, 255));
            spike.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(38, 36, 33, 255));
            spike.rectTransform.rotation = Quaternion.Euler(0f, 0f, rotation);
            AddOutline(spike, new Color32(110, 97, 72, 255), new Vector2(1f, -1f));
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
