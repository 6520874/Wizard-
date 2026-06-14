using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitcherGame
{
    // Places an invisible shop interaction trigger on the village map.
    // 中文说明：负责装备店门口的靠近检测、交互提示和打开商店流程。
    [RequireComponent(typeof(BoxCollider2D))]
    public class WitcherEquipmentShopTrigger : MonoBehaviour
    {
        private const string ShopRootName = "Village Equipment Shop";

        private readonly List<ShopItemData> shopItems = new List<ShopItemData>
        {
            new ShopItemData("生锈铁剑", 50, 3, 0),
            new ShopItemData("猎人长剑", 120, 7, 0),
            new ShopItemData("皮甲", 80, 0, 4),
            new ShopItemData("铁护腕", 60, 0, 2)
        };

        private GeraltController player;
        private PlayerInventory inventory;
        private WitcherShopUi shopUi;
        private GameObject promptRoot;
        private Text promptText;
        private bool playerInRange;

        public static bool ShouldBlockPlayerHealInput { get; private set; }

        public static WitcherEquipmentShopTrigger CreateIfMissing()
        {
            WitcherEquipmentShopTrigger existing = FindObjectOfType<WitcherEquipmentShopTrigger>();
            if (existing != null)
            {
                return existing;
            }

            GameObject shopRoot = new GameObject(ShopRootName);
            shopRoot.transform.position = new Vector3(6.86f, -0.64f, 0f);

            GameObject triggerObject = new GameObject("Equipment Shop Door Trigger");
            triggerObject.transform.SetParent(shopRoot.transform, false);
            triggerObject.transform.localPosition = Vector3.zero;
            BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(1.45f, 0.8f);
            return triggerObject.AddComponent<WitcherEquipmentShopTrigger>();
        }

        private void Awake()
        {
            BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            shopUi = WitcherShopUi.CreateIfMissing();
            EnsurePromptUi();
            SetPromptVisible(false);
        }

        private void OnDestroy()
        {
            ShouldBlockPlayerHealInput = false;
        }

        private void Update()
        {
            bool shopOpen = shopUi != null && shopUi.IsOpen;
            ShouldBlockPlayerHealInput = playerInRange || shopOpen;
            if (!playerInRange || shopOpen)
            {
                SetPromptVisible(false);
                return;
            }

            SetPromptVisible(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenDialogue();
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
            inventory = PlayerInventory.CreateIfMissing(player);
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

        private void OpenDialogue()
        {
            if (player == null)
            {
                return;
            }

            inventory = PlayerInventory.CreateIfMissing(player);
            SetPromptVisible(false);
            shopUi.BeginShopDialogue(player, inventory, shopItems, () =>
            {
                ShouldBlockPlayerHealInput = playerInRange;
            });
        }

        private void EnsurePromptUi()
        {
            if (promptRoot != null)
            {
                return;
            }

            Canvas canvas = EnsureCanvas("Shop Interaction Canvas", 130);
            promptRoot = new GameObject("Equipment Shop Prompt");
            promptRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = promptRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(250f, 44f);
            rootRect.anchoredPosition = new Vector2(0f, 116f);

            Image background = promptRoot.AddComponent<Image>();
            background.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 11, 218));
            background.color = new Color32(5, 8, 11, 218);
            Outline outline = promptRoot.AddComponent<Outline>();
            outline.effectColor = new Color32(148, 110, 64, 255);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject textObject = new GameObject("Equipment Shop Prompt Text");
            textObject.transform.SetParent(promptRoot.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            promptText = textObject.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.text = "按 E 交互：装备店";
            promptText.fontSize = 20;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = new Color32(255, 222, 141, 255);
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
            if (EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }
    }
}
