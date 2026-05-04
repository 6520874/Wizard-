using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PixelRaid
{
    public class PixelRaidPlayerHud : MonoBehaviour
    {
        private const string HudName = "PixelRaid HUD";
        private const string PortraitPath = "PixelRaid/Art/UI/GeraltPortrait.png";
        private const float PlayerBarWidth = 430f;

        private PixelRaidPlayerController player;
        private Image healthFill;
        private Image manaFill;
        private Text healthText;
        private Text manaText;
        private Text roomText;
        private GameObject bossStatusRoot;
        private Image bossHealthFill;
        private Text bossHealthText;

        public static PixelRaidPlayerHud CreateIfMissing(PixelRaidPlayerController target)
        {
            PixelRaidPlayerHud existing = FindObjectOfType<PixelRaidPlayerHud>();
            if (existing != null)
            {
                existing.SetPlayer(target);
                return existing;
            }

            GameObject hudObject = new GameObject(HudName);
            PixelRaidPlayerHud hud = hudObject.AddComponent<PixelRaidPlayerHud>();
            hud.SetPlayer(target);
            return hud;
        }

        public void SetPlayer(PixelRaidPlayerController target)
        {
            player = target;
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
                player = FindObjectOfType<PixelRaidPlayerController>();
                if (player == null)
                {
                    return;
                }
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

            GameObject root = CreateUiObject("TopLeft Status", transform, new Vector2(650f, 205f), new Vector2(14f, -14f), new Vector2(0f, 1f));
            Image panel = root.AddComponent<Image>();
            panel.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 0));
            panel.color = new Color32(0, 0, 0, 0);

            BuildPortraitBadge(root.transform);

            Text nameText = CreateText("Player Title", root.transform, "猎魔人", 15, TextAnchor.MiddleLeft, new Vector2(176f, -18f), new Vector2(128f, 24f));
            nameText.color = new Color32(236, 221, 183, 255);
            AddOutline(nameText, new Color32(0, 0, 0, 230), new Vector2(1f, -1f));

            roomText = CreateText("Room Label", root.transform, "霜林边境", 12, TextAnchor.MiddleRight, new Vector2(450f, -18f), new Vector2(130f, 24f));
            roomText.color = new Color32(152, 178, 188, 255);

            healthFill = CreateBar(root.transform, "Health", "HP", new Vector2(176f, -48f), new Color32(219, 20, 31, 255), new Color32(111, 4, 8, 255), out healthText);
            manaFill = CreateBar(root.transform, "Mana", "MP", new Vector2(176f, -98f), new Color32(22, 112, 236, 255), new Color32(0, 42, 121, 255), out manaText);

            BuildSkillSlots(root.transform);

            BuildBossBar();
        }

        private void BuildPortraitBadge(Transform parent)
        {
            Image halo = CreateImage("Portrait Halo", parent, new Vector2(132f, 132f), new Vector2(18f, -10f), new Color32(8, 10, 12, 240));
            halo.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(8, 10, 12, 240));
            AddOutline(halo, new Color32(93, 87, 77, 255), new Vector2(4f, -4f));

            Image ring = CreateImage("Portrait Ring", parent, new Vector2(116f, 116f), new Vector2(26f, -18f), new Color32(19, 22, 24, 255));
            ring.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(19, 22, 24, 255));
            AddOutline(ring, new Color32(151, 126, 78, 255), new Vector2(2f, -2f));

            Image redCore = CreateImage("Portrait Red Core", parent, new Vector2(102f, 102f), new Vector2(33f, -25f), new Color32(63, 5, 9, 220));
            redCore.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(63, 5, 9, 220));

            Image portraitBack = CreateImage("Portrait Back", parent, new Vector2(88f, 88f), new Vector2(40f, -32f), new Color32(3, 5, 7, 255));
            portraitBack.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(3, 5, 7, 255));

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
            Text labelText = CreateText(name + " Label", parent, label, 25, TextAnchor.MiddleLeft, position + new Vector2(22f, -3f), new Vector2(58f, 34f));
            labelText.color = new Color32(229, 218, 185, 255);
            AddOutline(labelText, new Color32(0, 0, 0, 245), new Vector2(2f, -2f));

            Image railShadow = CreateImage(name + " Rail Shadow", parent, new Vector2(PlayerBarWidth + 54f, 44f), position + new Vector2(0f, 0f), new Color32(0, 0, 0, 175));
            railShadow.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 175));

            Image outer = CreateImage(name + " Outer Frame", parent, new Vector2(PlayerBarWidth + 42f, 34f), position + new Vector2(6f, -5f), new Color32(55, 50, 45, 255));
            outer.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(55, 50, 45, 255));
            AddOutline(outer, new Color32(7, 8, 9, 255), new Vector2(2f, -2f));

            Image leftCap = CreateImage(name + " Left Cap", parent, new Vector2(16f, 42f), position + new Vector2(-4f, -1f), new Color32(25, 23, 22, 255));
            leftCap.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(25, 23, 22, 255));
            AddOutline(leftCap, new Color32(117, 103, 74, 255), new Vector2(1f, -1f));

            Image rightArrow = CreateCenteredImage(name + " Arrow Head", parent, new Vector2(45f, 34f), position + new Vector2(PlayerBarWidth + 42f, -22f), new Color32(39, 36, 34, 255));
            rightArrow.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(39, 36, 34, 255));
            rightArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(rightArrow, new Color32(119, 104, 76, 255), new Vector2(1f, -1f));

            Image back = CreateImage(name + " Back", outer.transform, new Vector2(PlayerBarWidth, 24f), new Vector2(18f, -5f), new Color32(2, 4, 7, 255));
            back.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(2, 4, 7, 255));

            Image lowFill = CreateImage(name + " Low Fill", back.transform, new Vector2(PlayerBarWidth - 6f, 18f), new Vector2(3f, -3f), lowColor);
            lowFill.sprite = PixelRaidSpriteLibrary.GetSolidSprite(lowColor);

            Image fill = CreateImage(name + " Fill", back.transform, new Vector2(PlayerBarWidth - 6f, 18f), new Vector2(3f, -3f), fillColor);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fill.sprite = PixelRaidSpriteLibrary.GetSolidSprite(fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            Image highlight = CreateImage(name + " Highlight", back.transform, new Vector2(PlayerBarWidth - 6f, 5f), new Vector2(3f, -3f), new Color32(255, 255, 255, 70));
            highlight.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(255, 255, 255, 45));
            highlight.raycastTarget = false;

            CreateDiamond(name + " Gem", parent, position + new Vector2(PlayerBarWidth + 31f, -6f), 20f, fillColor, new Color32(7, 8, 10, 255));

            valueText = CreateText(name + " Value", outer.transform, "100/100", 25, TextAnchor.MiddleLeft, new Vector2(94f, -1f), new Vector2(PlayerBarWidth - 110f, 30f));
            valueText.color = new Color32(250, 249, 232, 255);
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
            frame.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(9, 10, 12, 235));
            AddOutline(frame, new Color32(105, 94, 73, 255), new Vector2(2f, -2f));

            Image inset = CreateImage("Skill Slot Inset " + index, frame.transform, new Vector2(56f, 46f), new Vector2(7f, -8f), new Color32(5, 8, 12, 255));
            inset.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 255));

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
            float health01 = player.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth);
            float mana01 = player.MaxMana <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentMana / player.MaxMana);

            SetFill(healthFill, health01, PlayerBarWidth - 6f);
            SetFill(manaFill, mana01, PlayerBarWidth - 6f);
            healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
            manaText.text = $"{player.CurrentMana}/{player.MaxMana}";
            UpdateBossBar();
        }

        private void BuildBossBar()
        {
            bossStatusRoot = CreateUiObject("Boss Status", transform, new Vector2(430f, 52f), new Vector2(0f, -22f), new Vector2(0.5f, 1f));
            Image panel = bossStatusRoot.AddComponent<Image>();
            panel.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(5, 9, 14, 205));
            panel.color = new Color32(5, 9, 14, 205);

            Text title = CreateText("Boss Name", bossStatusRoot.transform, "狂猎统领", 15, TextAnchor.MiddleLeft, new Vector2(14f, -6f), new Vector2(160f, 20f));
            title.color = new Color32(207, 231, 245, 255);

            Image back = CreateImage("Boss Health Back", bossStatusRoot.transform, new Vector2(392f, 16f), new Vector2(18f, -28f), new Color32(2, 4, 8, 240));
            back.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(2, 4, 8, 240));

            bossHealthFill = CreateImage("Boss Health Fill", back.transform, new Vector2(386f, 10f), new Vector2(3f, -3f), new Color32(83, 184, 232, 255));
            RectTransform fillRect = bossHealthFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            bossHealthFill.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(83, 184, 232, 255));

            bossHealthText = CreateText("Boss Health Value", bossStatusRoot.transform, "8/8", 13, TextAnchor.MiddleRight, new Vector2(316f, -5f), new Vector2(92f, 20f));
            bossHealthText.color = new Color32(236, 246, 251, 255);
            bossStatusRoot.SetActive(false);
        }

        private void UpdateBossBar()
        {
            PixelRaidWildHuntBossController boss = FindObjectOfType<PixelRaidWildHuntBossController>();
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

        private static Image CreateCenteredImage(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateDiamond(string name, Transform parent, Vector2 position, float size, Color32 fillColor, Color32 outlineColor)
        {
            Image outline = CreateCenteredImage(name + " Outline", parent, new Vector2(size + 8f, size + 8f), position, outlineColor);
            outline.sprite = PixelRaidSpriteLibrary.GetSolidSprite(outlineColor);
            outline.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image diamond = CreateCenteredImage(name, parent, new Vector2(size, size), position, fillColor);
            diamond.sprite = PixelRaidSpriteLibrary.GetSolidSprite(fillColor);
            diamond.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
            return diamond;
        }

        private static void CreateSpike(string name, Transform parent, Vector2 position, Vector2 size, float rotation)
        {
            Image spike = CreateCenteredImage(name, parent, size, position, new Color32(38, 36, 33, 255));
            spike.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(38, 36, 33, 255));
            spike.rectTransform.rotation = Quaternion.Euler(0f, 0f, rotation);
            AddOutline(spike, new Color32(110, 97, 72, 255), new Vector2(1f, -1f));
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Text textComponent = obj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            string absolutePath = Path.Combine(Application.dataPath, assetRelativePath);
            if (!File.Exists(absolutePath))
            {
                return PixelRaidSpriteLibrary.GetSolidSprite(new Color32(23, 28, 34, 255));
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return PixelRaidSpriteLibrary.GetSolidSprite(new Color32(23, 28, 34, 255));
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
