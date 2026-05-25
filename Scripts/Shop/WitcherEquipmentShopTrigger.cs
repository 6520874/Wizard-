using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：在大地图生成装备店建筑、碰撞和交互触发区。
    [RequireComponent(typeof(BoxCollider2D))]
    public class WitcherEquipmentShopTrigger : MonoBehaviour
    {
        private const string ShopRootName = "Raven Anvil Equipment Shop";

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
            shopRoot.transform.position = new Vector3(7.6f, 2.18f, 0f);
            BuildShopBuilding(shopRoot.transform);
            BuildShopCollision(shopRoot);

            GameObject triggerObject = new GameObject("Equipment Shop Door Trigger");
            triggerObject.transform.SetParent(shopRoot.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, -1.45f, 0f);
            BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(3.2f, 1.2f);
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

        private static void BuildShopCollision(GameObject shopRoot)
        {
            BoxCollider2D bodyCollider = shopRoot.AddComponent<BoxCollider2D>();
            bodyCollider.isTrigger = false;
            bodyCollider.size = new Vector2(3.05f, 1.85f);
            bodyCollider.offset = new Vector2(0f, 0.12f);
        }

        private static void BuildShopBuilding(Transform parent)
        {
            CreateBlock(parent, "Shop Wall", new Vector2(2.8f, 1.45f), new Vector3(0f, -0.1f, 0f), new Color32(47, 39, 34, 255), 35);
            CreateBlock(parent, "Shop Roof", new Vector2(3.2f, 0.62f), new Vector3(0f, 0.78f, 0f), new Color32(26, 24, 27, 255), 37);
            CreateBlock(parent, "Shop Roof Edge", new Vector2(3.42f, 0.18f), new Vector3(0f, 0.45f, 0f), new Color32(83, 63, 42, 255), 38);
            CreateBlock(parent, "Shop Door", new Vector2(0.58f, 0.82f), new Vector3(-0.48f, -0.55f, 0f), new Color32(26, 17, 13, 255), 39);
            CreateBlock(parent, "Shop Window", new Vector2(0.54f, 0.34f), new Vector3(0.58f, -0.15f, 0f), new Color32(218, 133, 54, 245), 40);
            CreateBlock(parent, "Shop Sign Back", new Vector2(0.72f, 0.48f), new Vector3(1.12f, 0.42f, 0f), new Color32(18, 16, 18, 255), 41);
            CreateTextSign(parent);
            CreateBlock(parent, "Shop Anvil Left", new Vector2(0.56f, 0.18f), new Vector3(1.05f, -0.78f, 0f), new Color32(92, 96, 98, 255), 42);
            CreateBlock(parent, "Shop Anvil Base", new Vector2(0.28f, 0.28f), new Vector3(1.06f, -0.98f, 0f), new Color32(45, 48, 50, 255), 42);
        }

        private static void CreateBlock(Transform parent, string name, Vector2 size, Vector3 position, Color32 color, int sortingOrder)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(color);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static void CreateTextSign(Transform parent)
        {
            GameObject textObject = new GameObject("Shop Sign Text");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = new Vector3(1.12f, 0.34f, -0.02f);
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = "武";
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.045f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color32(255, 219, 120, 255);
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 43;
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
