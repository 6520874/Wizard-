using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PixelRaid
{
    public class PixelRaidPlayerHud : MonoBehaviour
    {
        private const string HudName = "PixelRaid HUD";
        private const string PortraitPath = "PixelRaid/Art/UI/GeraltPortrait.png";
        private const float PlayerBarWidth = 250f;

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

            GameObject root = CreateUiObject("TopLeft Status", transform, new Vector2(350f, 78f), new Vector2(6f, -6f), new Vector2(0f, 1f));
            Image panel = root.AddComponent<Image>();
            panel.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(8, 11, 15, 215));
            panel.color = new Color32(8, 11, 15, 215);

            Image portraitFrame = CreateImage("Portrait Frame", root.transform, new Vector2(58f, 58f), new Vector2(10f, -10f), new Color32(21, 31, 39, 245));
            portraitFrame.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(21, 31, 39, 245));

            Image portrait = CreateImage("Geralt Portrait", root.transform, new Vector2(50f, 50f), new Vector2(14f, -14f), Color.white);
            portrait.sprite = LoadSprite(PortraitPath, 96f);
            portrait.preserveAspect = true;

            healthFill = CreateBar(root.transform, "Health", "HP", new Vector2(78f, -16f), new Color32(220, 37, 48, 255), out healthText);
            manaFill = CreateBar(root.transform, "Mana", "MP", new Vector2(78f, -45f), new Color32(35, 132, 238, 255), out manaText);

            roomText = CreateText("Room Label", root.transform, "霜林边境", 11, TextAnchor.MiddleLeft, new Vector2(78f, -63f), new Vector2(240f, 14f));
            roomText.color = new Color32(202, 216, 225, 255);

            BuildBossBar();
        }

        private Image CreateBar(Transform parent, string name, string label, Vector2 position, Color32 fillColor, out Text valueText)
        {
            Text labelText = CreateText(name + " Label", parent, label, 13, TextAnchor.MiddleLeft, position, new Vector2(28f, 22f));
            labelText.color = new Color32(218, 227, 229, 255);

            Image back = CreateImage(name + " Back", parent, new Vector2(PlayerBarWidth, 20f), position + new Vector2(30f, 0f), new Color32(2, 4, 7, 245));
            back.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(2, 4, 7, 230));

            Image fill = CreateImage(name + " Fill", back.transform, new Vector2(PlayerBarWidth - 6f, 14f), new Vector2(3f, -3f), fillColor);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fill.sprite = PixelRaidSpriteLibrary.GetSolidSprite(fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            valueText = CreateText(name + " Value", back.transform, "100/100", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(PlayerBarWidth, 20f));
            valueText.color = new Color32(250, 249, 232, 255);
            return fill;
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
