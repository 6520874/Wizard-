using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    public class WitcherSurvivorRunManager : MonoBehaviour
    {
        private const string ManagerName = "Witcher Survivor Run Manager";
        private const float WaveDuration = 30f;

        private static WitcherSurvivorRunManager instance;

        private GeraltController player;
        private WildHuntBossSpawnDirector spawner;
        private Transform uiRoot;
        private Text runStatusText;
        private Text experienceText;
        private Text waveText;
        private Image experienceFill;
        private GameObject levelUpOverlay;

        private int level = 1;
        private int experience;
        private int experienceToNext = 8;
        private int kills;
        private int wave = 1;
        private int pendingLevelUps;
        private float runTimer;
        private float nextWaveAt = WaveDuration;
        private bool choosingUpgrade;

        private enum UpgradeKind
        {
            FlameDamage,
            FlameReach,
            MaxHealth,
            MaxMana,
            ManaRegen,
            Mobility
        }

        public static WitcherSurvivorRunManager CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            WitcherSurvivorRunManager existing = FindObjectOfType<WitcherSurvivorRunManager>();
            if (existing != null)
            {
                instance = existing;
                instance.SetPlayer(target);
                return existing;
            }

            GameObject managerObject = new GameObject(ManagerName);
            WitcherSurvivorRunManager manager = managerObject.AddComponent<WitcherSurvivorRunManager>();
            manager.SetPlayer(target);
            return manager;
        }

        public static void NotifyKill(Vector3 position, int experienceAmount)
        {
            if (instance == null)
            {
                GeraltController player = FindObjectOfType<GeraltController>();
                if (player != null)
                {
                    CreateIfMissing(player);
                }
            }

            if (instance != null)
            {
                instance.RegisterKill(position, experienceAmount);
            }
        }

        private void Awake()
        {
            instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            spawner = FindObjectOfType<WildHuntBossSpawnDirector>();
            BuildHud();
            ApplyWaveTuning();
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
            }

            if (spawner == null)
            {
                spawner = FindObjectOfType<WildHuntBossSpawnDirector>();
            }

            if (player == null || !player.IsAlive || choosingUpgrade)
            {
                UpdateHud();
                return;
            }

            runTimer += Time.deltaTime;
            if (runTimer >= nextWaveAt)
            {
                AdvanceWave();
            }

            UpdateHud();
        }

        private void SetPlayer(GeraltController target)
        {
            player = target;
        }

        private void RegisterKill(Vector3 position, int experienceAmount)
        {
            kills++;
            int amount = Mathf.Max(1, experienceAmount);
            WitcherCombatText.Spawn($"+{amount} XP", position + Vector3.up * 0.75f, new Color32(255, 219, 91, 255));
            AddExperience(amount);
            UpdateHud();
        }

        private void AddExperience(int amount)
        {
            experience += Mathf.Max(1, amount);
            while (experience >= experienceToNext)
            {
                experience -= experienceToNext;
                level++;
                pendingLevelUps++;
                experienceToNext = Mathf.RoundToInt(experienceToNext * 1.32f + 4f);
            }

            if (pendingLevelUps > 0 && !choosingUpgrade)
            {
                ShowLevelUpChoices();
            }
        }

        private void AdvanceWave()
        {
            wave++;
            nextWaveAt += WaveDuration;
            ApplyWaveTuning();
            WitcherCombatText.Spawn($"第 {wave} 波", player.transform.position + Vector3.up * 1.55f, new Color32(255, 177, 84, 255));
        }

        private void ApplyWaveTuning()
        {
            spawner = spawner == null ? FindObjectOfType<WildHuntBossSpawnDirector>() : spawner;
            if (spawner != null)
            {
                spawner.ApplySurvivorWaveTuning(wave);
            }
        }

        private void BuildHud()
        {
            if (uiRoot != null)
            {
                Destroy(uiRoot.gameObject);
            }

            Canvas canvas = FindObjectOfType<WitcherHud>()?.GetComponent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Survivor Run Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 125;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject panelObject = CreateUiObject("Survivor Run Panel", canvas.transform, new Vector2(280f, 138f), new Vector2(-18f, -18f), new Vector2(1f, 1f));
            uiRoot = panelObject.transform;
            Image panel = panelObject.AddComponent<Image>();
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 206));
            panel.color = new Color32(5, 8, 12, 206);
            AddOutline(panel, new Color32(85, 74, 55, 255), new Vector2(2f, -2f));

            waveText = CreateText("Wave Text", uiRoot, "第 1 波", 21, TextAnchor.MiddleLeft, new Vector2(16f, -12f), new Vector2(120f, 28f));
            waveText.color = new Color32(255, 218, 143, 255);
            AddOutline(waveText, Color.black, new Vector2(1f, -1f));

            runStatusText = CreateText("Run Status Text", uiRoot, "00:00  击杀 0", 15, TextAnchor.MiddleRight, new Vector2(126f, -14f), new Vector2(138f, 24f));
            runStatusText.color = new Color32(220, 232, 236, 255);
            AddOutline(runStatusText, Color.black, new Vector2(1f, -1f));

            Text levelText = CreateText("Level Label", uiRoot, "猎魔等级", 14, TextAnchor.MiddleLeft, new Vector2(16f, -52f), new Vector2(86f, 22f));
            levelText.color = new Color32(178, 199, 207, 255);

            Image xpBack = CreateImage("Experience Back", uiRoot, new Vector2(248f, 16f), new Vector2(16f, -80f), new Color32(2, 4, 8, 236));
            xpBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(2, 4, 8, 236));

            experienceFill = CreateImage("Experience Fill", xpBack.transform, new Vector2(242f, 10f), new Vector2(3f, -3f), new Color32(255, 176, 46, 255));
            RectTransform fillRect = experienceFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            experienceFill.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 176, 46, 255));

            experienceText = CreateText("Experience Text", uiRoot, "Lv 1  0 / 8", 15, TextAnchor.MiddleRight, new Vector2(104f, -50f), new Vector2(160f, 24f));
            experienceText.color = new Color32(255, 240, 198, 255);
            AddOutline(experienceText, Color.black, new Vector2(1f, -1f));

            Text hint = CreateText("Survivor Hint", uiRoot, "J 火焰  K/左键 斩击  Shift 闪避", 13, TextAnchor.MiddleCenter, new Vector2(14f, -108f), new Vector2(252f, 20f));
            hint.color = new Color32(184, 205, 211, 255);
            UpdateHud();
        }

        private void UpdateHud()
        {
            if (runStatusText == null)
            {
                return;
            }

            int minutes = Mathf.FloorToInt(runTimer / 60f);
            int seconds = Mathf.FloorToInt(runTimer % 60f);
            runStatusText.text = $"{minutes:00}:{seconds:00}  击杀 {kills}";
            waveText.text = $"第 {wave} 波";
            experienceText.text = $"Lv {level}  {experience} / {experienceToNext}";
            SetFill(experienceFill, experienceToNext <= 0 ? 0f : Mathf.Clamp01((float)experience / experienceToNext), 242f);
        }

        private void ShowLevelUpChoices()
        {
            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
            choosingUpgrade = true;
            Time.timeScale = 0f;
            player?.SetControlEnabled(false);

            Canvas canvas = uiRoot == null ? FindObjectOfType<Canvas>() : uiRoot.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (levelUpOverlay != null)
            {
                Destroy(levelUpOverlay);
            }

            levelUpOverlay = CreateUiObject("Level Up Overlay", canvas.transform, new Vector2(760f, 360f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image dim = CreateCenteredImage("Level Up Dim", levelUpOverlay.transform, new Vector2(2400f, 1400f), Vector2.zero, new Color32(0, 0, 0, 132));
            CenterRect(dim.rectTransform, Vector2.zero, new Vector2(2400f, 1400f));
            dim.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 132));

            Image panel = CreateCenteredImage("Level Up Panel", levelUpOverlay.transform, new Vector2(760f, 360f), Vector2.zero, new Color32(13, 17, 21, 242));
            CenterRect(panel.rectTransform, Vector2.zero, new Vector2(760f, 360f));
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(13, 17, 21, 242));
            AddOutline(panel, new Color32(203, 137, 58, 255), new Vector2(3f, -3f));

            Text title = CreateText("Level Up Title", panel.transform, "猎魔人变强了", 34, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(760f, 48f));
            CenterRect(title.rectTransform, new Vector2(0f, 126f), new Vector2(760f, 48f));
            title.color = new Color32(255, 209, 107, 255);
            AddOutline(title, Color.black, new Vector2(2f, -2f));

            UpgradeKind[] choices = PickChoices();
            for (int i = 0; i < choices.Length; i++)
            {
                CreateUpgradeButton(panel.transform, choices[i], new Vector2(-240f + i * 240f, -32f));
            }
        }

        private UpgradeKind[] PickChoices()
        {
            List<UpgradeKind> pool = new List<UpgradeKind>
            {
                UpgradeKind.FlameDamage,
                UpgradeKind.FlameReach,
                UpgradeKind.MaxHealth,
                UpgradeKind.MaxMana,
                UpgradeKind.ManaRegen,
                UpgradeKind.Mobility
            };

            UpgradeKind[] choices = new UpgradeKind[3];
            for (int i = 0; i < choices.Length; i++)
            {
                int index = Random.Range(0, pool.Count);
                choices[i] = pool[index];
                pool.RemoveAt(index);
            }

            return choices;
        }

        private void CreateUpgradeButton(Transform parent, UpgradeKind kind, Vector2 position)
        {
            Image image = CreateCenteredImage("Upgrade " + kind, parent, new Vector2(205f, 190f), position, new Color32(25, 29, 33, 255));
            CenterRect(image.rectTransform, position, new Vector2(205f, 190f));
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(25, 29, 33, 255));
            AddOutline(image, new Color32(91, 78, 54, 255), new Vector2(2f, -2f));

            Button button = image.gameObject.AddComponent<Button>();
            UpgradeKind capturedKind = kind;
            button.onClick.AddListener(() => ApplyUpgrade(capturedKind));

            Text name = CreateText("Upgrade Name", image.transform, GetUpgradeName(kind), 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(185f, 42f));
            CenterRect(name.rectTransform, new Vector2(0f, 52f), new Vector2(185f, 42f));
            name.color = new Color32(255, 223, 143, 255);
            AddOutline(name, Color.black, new Vector2(2f, -2f));

            Text description = CreateText("Upgrade Description", image.transform, GetUpgradeDescription(kind), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(170f, 84f));
            CenterRect(description.rectTransform, new Vector2(0f, -18f), new Vector2(170f, 84f));
            description.color = new Color32(220, 228, 224, 255);
        }

        private void ApplyUpgrade(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.FlameDamage:
                    player?.ImproveFlameDamage(1);
                    break;
                case UpgradeKind.FlameReach:
                    player?.ImproveFlameReach(0.85f, 0.03f);
                    break;
                case UpgradeKind.MaxHealth:
                    player?.IncreaseMaxHealth(18);
                    break;
                case UpgradeKind.MaxMana:
                    player?.IncreaseMaxMana(18);
                    break;
                case UpgradeKind.ManaRegen:
                    player?.ImproveManaRegen(2.2f);
                    break;
                case UpgradeKind.Mobility:
                    player?.ImproveMobility(0.42f, 0.24f);
                    break;
            }

            if (player != null)
            {
                WitcherCombatText.Spawn(GetUpgradeName(kind), player.transform.position + Vector3.up * 1.4f, new Color32(255, 224, 125, 255));
            }

            CloseLevelUpChoices();
        }

        private void CloseLevelUpChoices()
        {
            choosingUpgrade = false;
            Time.timeScale = 1f;
            player?.SetControlEnabled(true);
            if (levelUpOverlay != null)
            {
                Destroy(levelUpOverlay);
                levelUpOverlay = null;
            }

            UpdateHud();
            if (pendingLevelUps > 0)
            {
                ShowLevelUpChoices();
            }
        }

        private static string GetUpgradeName(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.FlameDamage:
                    return "烈焰加深";
                case UpgradeKind.FlameReach:
                    return "火舌延展";
                case UpgradeKind.MaxHealth:
                    return "黑血体质";
                case UpgradeKind.MaxMana:
                    return "法印储能";
                case UpgradeKind.ManaRegen:
                    return "冥想回流";
                case UpgradeKind.Mobility:
                    return "猎手步伐";
                default:
                    return "未知强化";
            }
        }

        private static string GetUpgradeDescription(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.FlameDamage:
                    return "火焰直线伤害 +1";
                case UpgradeKind.FlameReach:
                    return "火焰距离变长\\n判定略微变宽";
                case UpgradeKind.MaxHealth:
                    return "最大生命 +18\\n并立即回复";
                case UpgradeKind.MaxMana:
                    return "最大魔法 +18\\n并立即回复";
                case UpgradeKind.ManaRegen:
                    return "魔法回复速度提升";
                case UpgradeKind.Mobility:
                    return "移动速度和纵向走位提升";
                default:
                    return string.Empty;
            }
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

        private static void CenterRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetFill(Image image, float normalizedValue, float maxWidth)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(maxWidth * normalizedValue, rect.sizeDelta.y);
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
