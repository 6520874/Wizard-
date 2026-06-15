using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：负责装备店对话、购买列表、金币显示、键鼠选择和购买反馈。
    public class WitcherShopUi : MonoBehaviour
    {
        private const string UiName = "Witcher Equipment Shop UI";
        private const string ShopkeeperPortraitPath = "Art/Shops/ShopkeeperPortrait.png";
        private static Sprite cachedShopkeeperPortrait;

        private readonly List<Button> itemButtons = new List<Button>();
        private readonly List<Image> itemButtonImages = new List<Image>();
        private readonly List<Text> itemButtonTexts = new List<Text>();
        private readonly List<Button> dialogueButtons = new List<Button>();
        private readonly List<Image> dialogueButtonImages = new List<Image>();

        private GameObject root;
        private GameObject dialoguePanel;
        private GameObject shopPanel;
        private Text goldText;
        private Text dialogueText;
        private Text shopFeedbackText;
        private Text ownedText;
        private GeraltController player;
        private PlayerInventory inventory;
        private IReadOnlyList<ShopItemData> items;
        private Action onClosed;
        private bool shopOpen;
        private bool showingShopList;
        private int selectedDialogueIndex;
        private int selectedItemIndex;

        public bool IsOpen => shopOpen;

        public static WitcherShopUi CreateIfMissing()
        {
            WitcherShopUi existing = FindObjectOfType<WitcherShopUi>();
            if (existing != null)
            {
                return existing;
            }

            GameObject uiObject = new GameObject(UiName);
            return uiObject.AddComponent<WitcherShopUi>();
        }

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        private void Update()
        {
            if (!shopOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                if (showingShopList)
                {
                    ShowDialogueOptions(GameText.Shop.AnythingElseLine);
                }
                else
                {
                    CloseShop();
                }

                return;
            }

            if (showingShopList)
            {
                HandleShopInput();
            }
            else
            {
                HandleDialogueInput();
            }
        }

        public void BeginShopDialogue(GeraltController targetPlayer, PlayerInventory targetInventory, IReadOnlyList<ShopItemData> shopItems, Action closedCallback)
        {
            player = targetPlayer;
            inventory = targetInventory;
            items = shopItems;
            onClosed = closedCallback;
            shopOpen = true;
            selectedDialogueIndex = 0;
            selectedItemIndex = 0;
            root.SetActive(true);
            player?.SetControlEnabled(false);
            ShowDialogueOptions(GameText.Shop.WelcomeLine);
        }

        private void HandleDialogueInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedDialogueIndex = selectedDialogueIndex == 0 ? 1 : 0;
                RefreshDialogueSelection();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ChooseDialogueOption(selectedDialogueIndex);
            }
        }

        private void HandleShopInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveItemSelection(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveItemSelection(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                TryBuySelectedItem();
            }
        }

        private void ShowDialogueOptions(string line)
        {
            showingShopList = false;
            dialoguePanel.SetActive(true);
            shopPanel.SetActive(false);
            dialogueText.text = line;
            selectedDialogueIndex = 0;
            RefreshDialogueSelection();
        }

        private void ChooseDialogueOption(int optionIndex)
        {
            if (optionIndex == 0)
            {
                ShowShopList();
                return;
            }

            CloseShop();
        }

        private void ShowShopList()
        {
            showingShopList = true;
            dialoguePanel.SetActive(false);
            shopPanel.SetActive(true);
            selectedItemIndex = 0;
            shopFeedbackText.text = GameText.Shop.ShopInputHint;
            RefreshShop();
        }

        private void MoveItemSelection(int delta)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            selectedItemIndex = (selectedItemIndex + delta + items.Count) % items.Count;
            RefreshItemSelection();
        }

        private void TryBuySelectedItem()
        {
            if (items == null || selectedItemIndex < 0 || selectedItemIndex >= items.Count || inventory == null)
            {
                return;
            }

            bool success = inventory.TryPurchase(items[selectedItemIndex], out string message);
            shopFeedbackText.text = message;
            if (success && player != null)
            {
                WitcherCombatText.Spawn(GameText.Shop.GoldSpent(items[selectedItemIndex].Price), player.transform.position + Vector3.up * 1.2f, new Color32(255, 214, 97, 255));
            }

            RefreshShop();
        }

        private void RefreshShop()
        {
            goldText.text = inventory == null ? GameText.Stats.GoldAmount(0) : GameText.Stats.GoldAmount(inventory.Gold);
            ownedText.text = BuildOwnedText();
            for (int i = 0; i < itemButtons.Count; i++)
            {
                bool hasItem = items != null && i < items.Count;
                itemButtons[i].gameObject.SetActive(hasItem);
                if (!hasItem)
                {
                    continue;
                }

                ShopItemData item = items[i];
                bool owned = inventory != null && inventory.HasEquipment(item.ItemName);
                itemButtonTexts[i].text = GameText.Shop.ItemRow(item.ItemName, item.Price, item.GetBonusText()) + (owned ? GameText.Shop.AlreadyOwnedSuffix : string.Empty);
                itemButtons[i].interactable = !owned;
            }

            RefreshItemSelection();
        }

        private string BuildOwnedText()
        {
            if (inventory == null)
            {
                return GameText.Shop.OwnedNone;
            }

            string equipmentText = inventory.OwnedEquipment.Count == 0
                ? GameText.Common.NotAvailable
                : string.Join("、", inventory.OwnedEquipment);
            return GameText.Shop.OwnedSummary(equipmentText, inventory.AttackBonus, inventory.DefenseBonus, inventory.Experience, inventory.OwnedLoot.Count);
        }

        private void RefreshItemSelection()
        {
            for (int i = 0; i < itemButtonImages.Count; i++)
            {
                bool selected = i == selectedItemIndex;
                itemButtonImages[i].color = selected ? new Color32(57, 92, 115, 238) : new Color32(13, 16, 20, 224);
            }
        }

        private void RefreshDialogueSelection()
        {
            for (int i = 0; i < dialogueButtonImages.Count; i++)
            {
                bool selected = i == selectedDialogueIndex;
                dialogueButtonImages[i].color = selected ? new Color32(76, 98, 122, 240) : new Color32(14, 16, 20, 228);
            }
        }

        private void CloseShop()
        {
            Hide();
            player?.SetControlEnabled(true);
            Action closed = onClosed;
            onClosed = null;
            closed?.Invoke();
        }

        private void Hide()
        {
            shopOpen = false;
            showingShopList = false;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 145;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            root = CreateUiObject("Equipment Shop Root", transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            StretchToParent(root.GetComponent<RectTransform>());
            Image dim = CreateImage("Equipment Shop Dim", root.transform, new Vector2(2000f, 1200f), Vector2.zero, new Color32(0, 0, 0, 112), new Vector2(0.5f, 0.5f));
            dim.raycastTarget = true;

            BuildDialoguePanel();
            BuildShopPanel();
        }

        private void BuildDialoguePanel()
        {
            Image panel = CreateImage("Equipment Shop Dialogue", root.transform, new Vector2(760f, 150f), new Vector2(0f, -158f), new Color32(7, 9, 12, 238), new Vector2(0.5f, 0.5f));
            dialoguePanel = panel.gameObject;
            AddOutline(panel, new Color32(108, 83, 50, 255), new Vector2(2f, -2f));
            CreateShopkeeperPortrait(dialoguePanel.transform, "Dialogue", new Vector2(26f, -18f), new Vector2(116f, 116f), new Vector2(102f, 102f));

            Text name = CreateText("Equipment Shop Keeper Name", dialoguePanel.transform, GameText.Shop.KeeperName, 24, TextAnchor.MiddleLeft, new Vector2(160f, -18f), new Vector2(160f, 28f), new Color32(255, 219, 132, 255));
            AddOutline(name, Color.black, new Vector2(1f, -1f));
            dialogueText = CreateText("Equipment Shop Dialogue Text", dialoguePanel.transform, string.Empty, 21, TextAnchor.UpperLeft, new Vector2(160f, -56f), new Vector2(572f, 42f), new Color32(232, 235, 228, 255));

            AddDialogueButton(GameText.Shop.BuyEquipment, 0, new Vector2(318f, -104f));
            AddDialogueButton(GameText.Common.Leave, 1, new Vector2(504f, -104f));
        }

        private void BuildShopPanel()
        {
            Image panel = CreateImage("Equipment Shop Panel", root.transform, new Vector2(760f, 390f), Vector2.zero, new Color32(6, 8, 12, 244), new Vector2(0.5f, 0.5f));
            shopPanel = panel.gameObject;
            AddOutline(panel, new Color32(122, 94, 55, 255), new Vector2(2f, -2f));
            Text title = CreateText("Equipment Shop Title", shopPanel.transform, GameText.Shop.ShopTitle, 28, TextAnchor.MiddleCenter, new Vector2(0f, -18f), new Vector2(760f, 36f), new Color32(255, 218, 132, 255));
            AddOutline(title, Color.black, new Vector2(2f, -2f));
            goldText = CreateText("Equipment Shop Gold", shopPanel.transform, GameText.Stats.GoldAmount(0), 20, TextAnchor.MiddleRight, new Vector2(552f, -62f), new Vector2(170f, 26f), new Color32(255, 220, 96, 255));
            CreateShopkeeperPortrait(shopPanel.transform, "Shop", new Vector2(38f, -78f), new Vector2(124f, 124f), new Vector2(110f, 110f));
            Text keeperName = CreateText("Equipment Shop Keeper Card Name", shopPanel.transform, GameText.Shop.KeeperName, 21, TextAnchor.MiddleCenter, new Vector2(38f, -210f), new Vector2(124f, 28f), new Color32(255, 219, 132, 255));
            AddOutline(keeperName, Color.black, new Vector2(1f, -1f));
            Text keeperHint = CreateText("Equipment Shop Keeper Card Hint", shopPanel.transform, GameText.Shop.KeeperRole, 14, TextAnchor.MiddleCenter, new Vector2(38f, -238f), new Vector2(124f, 24f), new Color32(180, 200, 213, 255));
            AddOutline(keeperHint, Color.black, new Vector2(1f, -1f));
            ownedText = CreateText("Equipment Shop Owned", shopPanel.transform, GameText.Shop.OwnedNone, 14, TextAnchor.UpperLeft, new Vector2(38f, -314f), new Vector2(384f, 66f), new Color32(180, 200, 213, 255));
            shopFeedbackText = CreateText("Equipment Shop Feedback", shopPanel.transform, GameText.Shop.ShopInputHint, 16, TextAnchor.MiddleRight, new Vector2(456f, -336f), new Vector2(270f, 24f), new Color32(207, 223, 232, 255));

            for (int i = 0; i < 5; i++)
            {
                AddItemButton(i, new Vector2(184f, -100f - i * 44f), new Vector2(538f, 36f));
            }

            Button closeButton = CreateButton("Equipment Shop Close Button", shopPanel.transform, GameText.Shop.ReturnToDialogue, new Vector2(110f, 30f), new Vector2(614f, -28f));
            closeButton.onClick.AddListener(() => ShowDialogueOptions(GameText.Shop.AnythingElseLine));
        }

        private void AddDialogueButton(string label, int optionIndex, Vector2 position)
        {
            Button button = CreateButton("Equipment Shop " + label, dialoguePanel.transform, label, new Vector2(150f, 34f), position);
            button.onClick.AddListener(() => ChooseDialogueOption(optionIndex));
            dialogueButtons.Add(button);
            dialogueButtonImages.Add(button.targetGraphic as Image);
        }

        private void AddItemButton(int index, Vector2 position, Vector2 size)
        {
            Button button = CreateButton($"Equipment Shop Item {index + 1}", shopPanel.transform, string.Empty, size, position);
            int capturedIndex = index;
            button.onClick.AddListener(() =>
            {
                selectedItemIndex = capturedIndex;
                TryBuySelectedItem();
            });
            itemButtons.Add(button);
            itemButtonImages.Add(button.targetGraphic as Image);
            itemButtonTexts.Add(button.GetComponentInChildren<Text>());
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 size, Vector2 position)
        {
            GameObject buttonObject = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(14, 16, 20, 228));
            image.color = new Color32(14, 16, 20, 228);
            AddOutline(image, new Color32(92, 103, 115, 230), new Vector2(1f, -1f));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(118, 150, 178, 255);
            colors.pressedColor = new Color32(255, 199, 105, 255);
            colors.disabledColor = new Color32(64, 64, 64, 160);
            button.colors = colors;

            Text label = CreateText(name + " Text", buttonObject.transform, text, 18, TextAnchor.MiddleCenter, Vector2.zero, size, new Color32(235, 238, 229, 255));
            label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            AddOutline(label, Color.black, new Vector2(1f, -1f));
            return button;
        }

        private static void CreateShopkeeperPortrait(Transform parent, string context, Vector2 position, Vector2 frameSize, Vector2 portraitSize)
        {
            Image portraitFrame = CreateImage($"Equipment Shop Keeper {context} Portrait Frame", parent, frameSize, position, new Color32(12, 14, 17, 255), new Vector2(0f, 1f));
            AddOutline(portraitFrame, new Color32(143, 100, 56, 255), new Vector2(2f, -2f));
            Image portrait = CreateImage($"Equipment Shop Keeper {context} Portrait", portraitFrame.transform, portraitSize, new Vector2((frameSize.x - portraitSize.x) * 0.5f, -(frameSize.y - portraitSize.y) * 0.5f), Color.white, new Vector2(0f, 1f));
            portrait.sprite = LoadShopkeeperPortrait();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }

        private static Sprite LoadShopkeeperPortrait()
        {
            if (cachedShopkeeperPortrait != null)
            {
                return cachedShopkeeperPortrait;
            }

            string absolutePath = Path.Combine(Application.dataPath, ShopkeeperPortraitPath);
            if (!File.Exists(absolutePath))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(42, 31, 24, 255));
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(42, 31, 24, 255));
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            cachedShopkeeperPortrait = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            cachedShopkeeperPortrait.name = "ShopkeeperPortrait_Runtime";
            return cachedShopkeeperPortrait;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color32 color)
        {
            GameObject textObject = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
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

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color, Vector2 anchor)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, anchor);
            Image image = obj.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
            image.color = color;
            return image;
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return obj;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
