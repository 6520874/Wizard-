using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PixelRaid
{
    public class PixelRaidPlayerHud : MonoBehaviour
    {
        private const string HudName = "PixelRaid HUD";
        private const string PortraitPath = "PixelRaid/Art/UI/GeraltPortrait.png";

        private PixelRaidPlayerController player;
        private Image healthFill;
        private Image manaFill;
        private Text healthText;
        private Text manaText;
        private Text roomText;

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
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = gameObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = Vector2.zero;

            GameObject root = CreateUiObject("TopLeft Status", transform, new Vector2(364f, 112f), new Vector2(22f, -20f), new Vector2(0f, 1f));
            Image panel = root.AddComponent<Image>();
            panel.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(8, 11, 15, 215));
            panel.type = Image.Type.Sliced;
            panel.color = new Color32(8, 11, 15, 215);

            Image portraitFrame = CreateImage("Portrait Frame", root.transform, new Vector2(88f, 88f), new Vector2(12f, -12f), new Color32(21, 31, 39, 245));
            portraitFrame.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(21, 31, 39, 245));

            Image portrait = CreateImage("Geralt Portrait", root.transform, new Vector2(78f, 78f), new Vector2(17f, -17f), Color.white);
            portrait.sprite = LoadSprite(PortraitPath, 96f);
            portrait.preserveAspect = true;

            healthFill = CreateBar(root.transform, "Health", new Vector2(110f, -24f), new Color32(199, 41, 49, 255), out healthText);
            manaFill = CreateBar(root.transform, "Mana", new Vector2(110f, -56f), new Color32(53, 139, 229, 255), out manaText);

            roomText = CreateText("Room Label", root.transform, "霜林边境", 14, TextAnchor.MiddleLeft, new Vector2(110f, -88f), new Vector2(230f, 22f));
            roomText.color = new Color32(202, 216, 225, 255);
        }

        private Image CreateBar(Transform parent, string label, Vector2 position, Color32 fillColor, out Text valueText)
        {
            CreateText(label + " Label", parent, label == "Health" ? "HP" : "MP", 14, TextAnchor.MiddleLeft, position, new Vector2(30f, 22f));

            Image back = CreateImage(label + " Back", parent, new Vector2(190f, 18f), position + new Vector2(32f, -1f), new Color32(2, 4, 7, 230));
            back.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(2, 4, 7, 230));

            Image fill = CreateImage(label + " Fill", back.transform, new Vector2(184f, 12f), new Vector2(3f, -3f), fillColor);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fill.sprite = PixelRaidSpriteLibrary.GetSolidSprite(fillColor);

            valueText = CreateText(label + " Value", parent, "100/100", 13, TextAnchor.MiddleRight, position + new Vector2(132f, -1f), new Vector2(86f, 22f));
            valueText.color = new Color32(241, 244, 237, 255);
            return fill;
        }

        private void UpdateBars()
        {
            float health01 = player.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth);
            float mana01 = player.MaxMana <= 0 ? 0f : Mathf.Clamp01((float)player.CurrentMana / player.MaxMana);

            SetFill(healthFill, health01, 184f);
            SetFill(manaFill, mana01, 184f);
            healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
            manaText.text = $"{player.CurrentMana}/{player.MaxMana}";
        }

        private static void SetFill(Image image, float normalizedValue, float maxWidth)
        {
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
