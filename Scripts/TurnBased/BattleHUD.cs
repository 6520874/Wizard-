using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：给 HD-2D 战斗 HUD 使用的轻量单位视图数据，不参与战斗结算。
    public class BattleUnit
    {
        public string Id;
        public string Name;
        public bool IsPlayer;
        public int EnemyIndex = -1;
        public int Level = 1;
        public int CurrentHp;
        public int MaxHp;
        public int CurrentSp;
        public int MaxSp;
        public int Shield;
        public bool IsAlive = true;
        public bool IsCurrentActor;
        public Sprite Portrait;
        public Vector2 UiPosition;
        public TurnBasedEnemyVisualKind VisualKind;
        public string[] Weaknesses = System.Array.Empty<string>();
        public bool[] WeaknessDiscovered = System.Array.Empty<bool>();
    }

    // 中文说明：暗黑 HD-2D 回合制战斗 HUD 的总入口，负责协调行动轴、状态栏、弱点栏和技能名。
    public class BattleHUD : MonoBehaviour
    {
        private const string GeraltPortraitPath = "Art/UI/GeraltPortrait.png";
        private static Sprite cachedGeraltPortrait;
        private TurnOrderBar turnOrderBar;
        private PartyStatusPanel partyStatusPanel;
        private EnemyWeaknessPanel enemyWeaknessPanel;
        private SkillNameBanner skillNameBanner;
        private Image targetIndicator;
        private RectTransform targetIndicatorRect;
        private BattleUnit currentTarget;

        public static BattleHUD CreateIfMissing(Transform parent)
        {
            BattleHUD existing = parent.GetComponentInChildren<BattleHUD>(true);
            if (existing != null)
            {
                return existing;
            }

            GameObject hudObject = new GameObject("HD2D Battle HUD");
            hudObject.transform.SetParent(parent, false);
            BattleHUD hud = hudObject.AddComponent<BattleHUD>();
            hud.Build();
            return hud;
        }

        public void RefreshFromBattle(
            TurnBasedBattleManager manager,
            IReadOnlyList<TurnBasedEnemyState> enemies,
            GeraltController player,
            IReadOnlyList<Vector2> enemyPositions)
        {
            if (turnOrderBar == null)
            {
                Build();
            }

            List<BattleUnit> enemyUnits = BuildEnemyUnits(enemies, enemyPositions);
            List<BattleUnit> partyUnits = BuildPartyUnits(player);
            List<BattleUnit> timelineUnits = BuildTimelineUnits(manager, enemies, player, enemyPositions);

            RefreshTurnOrder(timelineUnits);
            RefreshPartyStatus(partyUnits);
            RefreshEnemyWeakness(enemyUnits);
            SetCurrentActor(timelineUnits.Count > 0 ? timelineUnits[0] : null);
            SetTarget(FindFirstAliveEnemy(enemyUnits));
        }

        public void RefreshTurnOrder(List<BattleUnit> units)
        {
            turnOrderBar?.RefreshTurnOrder(units);
        }

        public void RefreshPartyStatus(List<BattleUnit> party)
        {
            partyStatusPanel?.RefreshPartyStatus(party);
        }

        public void RefreshEnemyWeakness(List<BattleUnit> enemies)
        {
            enemyWeaknessPanel?.RefreshEnemyWeakness(enemies);
        }

        public void ShowSkillName(string skillName)
        {
            skillNameBanner?.ShowSkillName(skillName);
        }

        public void SetCurrentActor(BattleUnit unit)
        {
            turnOrderBar?.SetCurrentActor(unit);
            partyStatusPanel?.SetCurrentActor(unit);
        }

        public void SetTarget(BattleUnit target)
        {
            currentTarget = target;
            if (targetIndicator == null)
            {
                return;
            }

            bool show = target != null && target.IsAlive;
            targetIndicator.gameObject.SetActive(show);
            if (show)
            {
                targetIndicatorRect.anchoredPosition = target.UiPosition + new Vector2(0f, 112f);
            }
        }

        private void Update()
        {
            if (targetIndicator != null && targetIndicator.gameObject.activeSelf && currentTarget != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 8f) * 5f;
                targetIndicatorRect.anchoredPosition = currentTarget.UiPosition + new Vector2(0f, 112f + bob);
            }
        }

        private void Build()
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            turnOrderBar = CreateChild<TurnOrderBar>("TurnOrderBar");
            turnOrderBar.Build();

            partyStatusPanel = CreateChild<PartyStatusPanel>("PartyStatusPanel");
            partyStatusPanel.Build();

            enemyWeaknessPanel = CreateChild<EnemyWeaknessPanel>("EnemyWeaknessPanel");
            enemyWeaknessPanel.Build();

            skillNameBanner = CreateChild<SkillNameBanner>("SkillNameBanner");
            skillNameBanner.Build();

            // 目标当前已经由敌人血条和弱点框表达，先移除闪烁三角，避免干扰画面中心。
        }

        private T CreateChild<T>(string name) where T : MonoBehaviour
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        private static List<BattleUnit> BuildPartyUnits(GeraltController player)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            if (player == null)
            {
                return units;
            }

            units.Add(new BattleUnit
            {
                Id = "player",
                Name = "猎魔人",
                IsPlayer = true,
                Level = 1,
                CurrentHp = player.CurrentHealth,
                MaxHp = player.MaxHealth,
                CurrentSp = player.CurrentMana,
                MaxSp = player.MaxMana,
                IsAlive = player.IsAlive,
                Portrait = LoadGeraltPortrait(),
                UiPosition = new Vector2(304f, -34f),
                Weaknesses = System.Array.Empty<string>(),
                WeaknessDiscovered = System.Array.Empty<bool>()
            });
            return units;
        }

        private static Sprite LoadGeraltPortrait()
        {
            if (cachedGeraltPortrait != null)
            {
                return cachedGeraltPortrait;
            }

            string absolutePath = Path.Combine(Application.dataPath, GeraltPortraitPath);
            if (!File.Exists(absolutePath))
            {
                cachedGeraltPortrait = WitcherSpriteLibrary.GetGeraltFrame(GeraltAnimation.Idle, 0);
                return cachedGeraltPortrait;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                cachedGeraltPortrait = WitcherSpriteLibrary.GetGeraltFrame(GeraltAnimation.Idle, 0);
                return cachedGeraltPortrait;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            cachedGeraltPortrait = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                128f);
            cachedGeraltPortrait.name = "GeraltBattlePortrait";
            return cachedGeraltPortrait;
        }

        private static List<BattleUnit> BuildEnemyUnits(IReadOnlyList<TurnBasedEnemyState> enemies, IReadOnlyList<Vector2> enemyPositions)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            if (enemies == null)
            {
                return units;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                TurnBasedEnemyState enemy = enemies[i];
                units.Add(new BattleUnit
                {
                    Id = "enemy-" + i,
                    Name = enemy.Name,
                    IsPlayer = false,
                    EnemyIndex = i,
                    Level = Mathf.Clamp(enemy.Attack / 3, 1, 9),
                    CurrentHp = enemy.Health,
                    MaxHp = enemy.MaxHealth,
                    CurrentSp = 0,
                    MaxSp = 0,
                    Shield = GetShieldValue(enemy),
                    IsAlive = enemy.IsAlive,
                    Portrait = FirstFrame(enemy.IdleFrames, enemy.Sprite),
                    UiPosition = enemyPositions != null && i < enemyPositions.Count ? enemyPositions[i] : GetFallbackEnemyPosition(i),
                    VisualKind = enemy.VisualKind,
                    Weaknesses = enemy.WeaknessLabelsOverride != null && enemy.WeaknessLabelsOverride.Length > 0 ? enemy.WeaknessLabelsOverride : GetWeaknessLabels(enemy.VisualKind),
                    WeaknessDiscovered = enemy.WeaknessDiscoveryOverride != null && enemy.WeaknessDiscoveryOverride.Length > 0 ? enemy.WeaknessDiscoveryOverride : GetWeaknessDiscovery(enemy.VisualKind)
                });
            }

            return units;
        }

        private static List<BattleUnit> BuildTimelineUnits(
            TurnBasedBattleManager manager,
            IReadOnlyList<TurnBasedEnemyState> enemies,
            GeraltController player,
            IReadOnlyList<Vector2> enemyPositions)
        {
            List<BattleUnit> result = new List<BattleUnit>();
            if (manager == null)
            {
                return result;
            }

            List<TurnBattleTimelineEntry> preview = manager.GetTimelinePreview(8);
            for (int i = 0; i < preview.Count; i++)
            {
                TurnBattleTimelineEntry entry = preview[i];
                BattleUnit unit;
                if (entry.IsPlayer)
                {
                    unit = BuildPartyUnits(player).Count > 0 ? BuildPartyUnits(player)[0] : null;
                }
                else
                {
                    unit = BuildEnemyUnits(enemies, enemyPositions).Find(e => e.EnemyIndex == entry.EnemyIndex);
                }

                if (unit == null)
                {
                    continue;
                }

                unit.IsCurrentActor = i == 0;
                result.Add(unit);
            }

            return result;
        }

        private static BattleUnit FindFirstAliveEnemy(List<BattleUnit> enemies)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return enemies[i];
                }
            }

            return null;
        }

        private static int GetShieldValue(TurnBasedEnemyState enemy)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return 0;
            }

            int baseShield;
            switch (enemy.VisualKind)
            {
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                case TurnBasedEnemyVisualKind.BlackNailPuppet:
                    baseShield = Mathf.Clamp(Mathf.CeilToInt(enemy.Health / (float)enemy.MaxHealth * 5f), 1, 5);
                    break;
                case TurnBasedEnemyVisualKind.BloodWraith:
                    baseShield = Mathf.Clamp(Mathf.CeilToInt(enemy.Health / (float)enemy.MaxHealth * 4f), 1, 4);
                    break;
                default:
                    baseShield = Mathf.Clamp(Mathf.CeilToInt(enemy.Health / (float)enemy.MaxHealth * 3f), 1, 3);
                    break;
            }

            return Mathf.Clamp(baseShield + enemy.ShieldAdjustment, 0, 5);
        }

        private static string[] GetWeaknessLabels(TurnBasedEnemyVisualKind kind)
        {
            switch (kind)
            {
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    return new[] { "银", "火", "雷", "印", "剑" };
                case TurnBasedEnemyVisualKind.BlackNailPuppet:
                    return new[] { "银", "火", "剑", "印", "？" };
                case TurnBasedEnemyVisualKind.BloodWraith:
                    return new[] { "银", "火", "冰", "印" };
                default:
                    return new[] { "剑", "弩", "火", "银" };
            }
        }

        private static bool[] GetWeaknessDiscovery(TurnBasedEnemyVisualKind kind)
        {
            switch (kind)
            {
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    return new[] { true, true, false, false, true };
                case TurnBasedEnemyVisualKind.BlackNailPuppet:
                    return new[] { true, true, true, false, false };
                case TurnBasedEnemyVisualKind.BloodWraith:
                    return new[] { true, false, false, true };
                default:
                    return new[] { true, false, true, false };
            }
        }

        private static Vector2 GetFallbackEnemyPosition(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector2(-314f, 44f);
                case 1:
                    return new Vector2(-184f, -30f);
                case 2:
                    return new Vector2(-230f, 108f);
                default:
                    return new Vector2(-380f, -50f);
            }
        }

        private static Sprite FirstFrame(Sprite[] frames, Sprite fallback)
        {
            return frames != null && frames.Length > 0 && frames[0] != null ? frames[0] : fallback;
        }

        private static void AddOutline(Graphic graphic, Color32 color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }

    // 中文说明：顶部行动顺序条，负责显示单位头像、当前行动高亮和呼吸动效。
    public class TurnOrderBar : MonoBehaviour
    {
        private readonly List<TurnOrderIcon> icons = new List<TurnOrderIcon>();
        private RectTransform rect;

        public void Build()
        {
            rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(760f, 68f);
            rect.anchoredPosition = new Vector2(-96f, -28f);

            Image back = gameObject.AddComponent<Image>();
            back.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelColor);
            back.color = BattleHudStyle.PanelColor;
            back.raycastTarget = false;
            BattleHudStyle.AddOutline(back, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            Image rail = BattleHudStyle.CreateImage("Turn Rail", transform, new Vector2(704f, 2f), new Vector2(28f, -35f), BattleHudStyle.RuleColor, new Vector2(0f, 1f));
            rail.raycastTarget = false;

            for (int i = 0; i < 8; i++)
            {
                TurnOrderIcon icon = BattleHudStyle.CreateBehaviour<TurnOrderIcon>("Turn Icon " + i, transform);
                icon.Build();
                icon.Rect.anchorMin = new Vector2(0f, 1f);
                icon.Rect.anchorMax = new Vector2(0f, 1f);
                icon.Rect.anchoredPosition = new Vector2(28f + i * 58f, -10f);
                icons.Add(icon);
            }
        }

        public void RefreshTurnOrder(List<BattleUnit> units)
        {
            for (int i = 0; i < icons.Count; i++)
            {
                bool show = units != null && i < units.Count;
                icons[i].gameObject.SetActive(show);
                if (show)
                {
                    icons[i].Refresh(units[i], i == 0);
                }
            }
        }

        public void SetCurrentActor(BattleUnit unit)
        {
            for (int i = 0; i < icons.Count; i++)
            {
                icons[i].SetCurrent(unit != null && icons[i].UnitId == unit.Id);
            }
        }
    }

    // 中文说明：行动条上的单个菱形头像格，显示敌我颜色和当前行动呼吸效果。
    public class TurnOrderIcon : MonoBehaviour
    {
        private Image frame;
        private Image portrait;
        private Text label;
        private bool current;
        private Vector3 baseScale = Vector3.one;

        public RectTransform Rect { get; private set; }
        public string UnitId { get; private set; }

        public void Build()
        {
            Rect = gameObject.AddComponent<RectTransform>();
            Rect.sizeDelta = new Vector2(42f, 42f);
            frame = gameObject.AddComponent<Image>();
            frame.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelStrongColor);
            frame.color = BattleHudStyle.PanelStrongColor;
            frame.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            BattleHudStyle.AddOutline(frame, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            portrait = BattleHudStyle.CreateImage("Portrait", transform, new Vector2(38f, 38f), Vector2.zero, Color.white, new Vector2(0.5f, 0.5f));
            portrait.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            label = BattleHudStyle.CreateText("Label", transform, "?", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(42f, 42f), BattleHudStyle.TextColor);
            label.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            BattleHudStyle.AddOutline(label, Color.black, new Vector2(2f, -2f));
        }

        public void Refresh(BattleUnit unit, bool isCurrent)
        {
            UnitId = unit.Id;
            current = isCurrent;
            frame.color = isCurrent ? BattleHudStyle.SelectedColor : BattleHudStyle.PanelStrongColor;
            portrait.sprite = unit.Portrait;
            portrait.color = unit.Portrait == null ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, isCurrent ? (byte)255 : (byte)150);
            label.text = string.IsNullOrEmpty(unit.Name) ? "?" : unit.Name.Substring(0, 1);
            label.color = unit.Portrait == null
                ? (isCurrent ? BattleHudStyle.GoldColor : BattleHudStyle.MutedTextColor)
                : new Color32(255, 255, 255, 0);
            Rect.localScale = isCurrent ? Vector3.one * 1.15f : Vector3.one;
        }

        public void SetCurrent(bool value)
        {
            current = value;
        }

        private void Update()
        {
            if (!current)
            {
                return;
            }

            float pulse = 1.15f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.045f;
            Rect.localScale = baseScale * pulse;
        }
    }

    // 中文说明：右侧我方队伍状态栏容器，当前只有猎魔人，后续可扩展多人队伍。
    public class PartyStatusPanel : MonoBehaviour
    {
        private readonly List<PartyStatusItem> items = new List<PartyStatusItem>();
        private RectTransform rect;

        public void Build()
        {
            rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(254f, 410f);
            rect.anchoredPosition = new Vector2(-60f, 18f);

            Image back = gameObject.AddComponent<Image>();
            back.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 24, 15, 92));
            back.color = new Color32(8, 24, 15, 92);
            back.raycastTarget = false;

            for (int i = 0; i < 4; i++)
            {
                PartyStatusItem item = BattleHudStyle.CreateBehaviour<PartyStatusItem>("Party Status " + i, transform);
                item.Build();
                item.Rect.anchorMin = new Vector2(0f, 1f);
                item.Rect.anchorMax = new Vector2(0f, 1f);
                item.Rect.anchoredPosition = new Vector2(0f, -i * 102f);
                item.gameObject.SetActive(false);
                items.Add(item);
            }
        }

        public void RefreshPartyStatus(List<BattleUnit> party)
        {
            for (int i = 0; i < items.Count; i++)
            {
                bool show = party != null && i < party.Count;
                items[i].gameObject.SetActive(show);
                if (show)
                {
                    items[i].Refresh(party[i]);
                }
            }
        }

        public void SetCurrentActor(BattleUnit unit)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetHighlighted(unit != null && items[i].UnitId == unit.Id);
            }
        }
    }

    // 中文说明：单个我方角色状态块，负责平滑刷新 HP/SP 条。
    public class PartyStatusItem : MonoBehaviour
    {
        private Image frame;
        private Image portrait;
        private Text nameText;
        private Text hpText;
        private Text spText;
        private Image hpFill;
        private Image spFill;
        private float displayedHp = 1f;
        private float displayedSp = 1f;
        private float targetHp = 1f;
        private float targetSp = 1f;
        private bool highlighted;

        public RectTransform Rect { get; private set; }
        public string UnitId { get; private set; }

        public void Build()
        {
            Rect = gameObject.AddComponent<RectTransform>();
            Rect.sizeDelta = new Vector2(254f, 96f);
            frame = gameObject.AddComponent<Image>();
            frame.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelColor);
            frame.color = BattleHudStyle.PanelColor;
            BattleHudStyle.AddOutline(frame, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            portrait = BattleHudStyle.CreateImage("Portrait", transform, new Vector2(54f, 54f), new Vector2(12f, -18f), Color.white, new Vector2(0f, 1f));
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            BattleHudStyle.AddOutline(portrait, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            nameText = BattleHudStyle.CreateText("Name", transform, "猎魔人", 18, TextAnchor.MiddleLeft, new Vector2(76f, -10f), new Vector2(150f, 24f), BattleHudStyle.TextColor);
            hpText = BattleHudStyle.CreateText("HP", transform, "HP 0 / 0", 15, TextAnchor.MiddleLeft, new Vector2(76f, -38f), new Vector2(156f, 20f), BattleHudStyle.TextColor);
            spText = BattleHudStyle.CreateText("SP", transform, "SP 0 / 0", 14, TextAnchor.MiddleLeft, new Vector2(76f, -64f), new Vector2(156f, 20f), BattleHudStyle.MutedTextColor);
            BattleHudStyle.AddOutline(nameText, Color.black, new Vector2(2f, -2f));
            BattleHudStyle.AddOutline(hpText, Color.black, new Vector2(1f, -1f));
            BattleHudStyle.AddOutline(spText, Color.black, new Vector2(1f, -1f));

            Image hpBack = BattleHudStyle.CreateImage("HP Back", transform, new Vector2(168f, 7f), new Vector2(76f, -58f), new Color32(5, 7, 10, 230), new Vector2(0f, 1f));
            hpFill = BattleHudStyle.CreateImage("HP Fill", hpBack.transform, new Vector2(168f, 7f), Vector2.zero, new Color32(93, 168, 116, 240), new Vector2(0f, 1f));
            hpFill.rectTransform.pivot = new Vector2(0f, 1f);

            Image spBack = BattleHudStyle.CreateImage("SP Back", transform, new Vector2(168f, 5f), new Vector2(76f, -82f), new Color32(5, 7, 10, 230), new Vector2(0f, 1f));
            spFill = BattleHudStyle.CreateImage("SP Fill", spBack.transform, new Vector2(168f, 5f), Vector2.zero, new Color32(92, 150, 180, 230), new Vector2(0f, 1f));
            spFill.rectTransform.pivot = new Vector2(0f, 1f);
        }

        public void Refresh(BattleUnit unit)
        {
            UnitId = unit.Id;
            targetHp = unit.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)unit.CurrentHp / unit.MaxHp);
            targetSp = unit.MaxSp <= 0 ? 0f : Mathf.Clamp01((float)unit.CurrentSp / unit.MaxSp);
            portrait.sprite = unit.Portrait;
            nameText.text = $"Lv {unit.Level}  {unit.Name}";
            hpText.text = $"HP {unit.CurrentHp} / {unit.MaxHp}";
            spText.text = $"SP {unit.CurrentSp} / {unit.MaxSp}";
            Color aliveColor = unit.IsAlive ? Color.white : new Color32(120, 125, 128, 190);
            portrait.color = aliveColor;
            nameText.color = unit.IsAlive ? BattleHudStyle.TextColor : new Color32(128, 134, 138, 210);
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            frame.color = highlighted ? BattleHudStyle.SelectedColor : BattleHudStyle.PanelColor;
        }

        private void Update()
        {
            displayedHp = Mathf.Lerp(displayedHp, targetHp, Time.unscaledDeltaTime * 8f);
            displayedSp = Mathf.Lerp(displayedSp, targetSp, Time.unscaledDeltaTime * 8f);
            SetFill(hpFill, displayedHp, 168f);
            SetFill(spFill, displayedSp, 168f);
            if (highlighted)
            {
                float glow = 0.55f + Mathf.Sin(Time.unscaledTime * 5f) * 0.25f;
                frame.color = Color.Lerp(BattleHudStyle.PanelColor, BattleHudStyle.SelectedColor, glow);
            }
        }

        private static void SetFill(Image image, float normalized, float width)
        {
            Vector2 size = image.rectTransform.sizeDelta;
            size.x = width * Mathf.Clamp01(normalized);
            image.rectTransform.sizeDelta = size;
        }
    }

    // 中文说明：敌人弱点栏容器，使用独立固定列显示，避免遮挡舞台上的怪物。
    public class EnemyWeaknessPanel : MonoBehaviour
    {
        private readonly List<EnemyWeaknessItem> items = new List<EnemyWeaknessItem>();

        public void Build()
        {
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            for (int i = 0; i < 4; i++)
            {
                EnemyWeaknessItem item = BattleHudStyle.CreateBehaviour<EnemyWeaknessItem>("Enemy Weakness " + i, transform);
                item.Build();
                item.gameObject.SetActive(false);
                items.Add(item);
            }
        }

        public void RefreshEnemyWeakness(List<BattleUnit> enemies)
        {
            for (int i = 0; i < items.Count; i++)
            {
                bool show = enemies != null && i < enemies.Count && enemies[i].IsAlive;
                items[i].gameObject.SetActive(show);
                if (show)
                {
                    items[i].Refresh(enemies[i]);
                }
            }
        }
    }

    // 中文说明：单个敌人的护盾和弱点显示，破防归零时闪烁。
    public class EnemyWeaknessItem : MonoBehaviour
    {
        private static readonly Vector2 WeaknessColumnStart = new Vector2(90f, 118f);
        private const float WeaknessRowGap = 42f;

        private RectTransform rect;
        private Image frame;
        private Text shieldText;
        private Text weaknessText;
        private readonly List<Text> weaknessCells = new List<Text>();
        private int previousShield = -1;

        public void Build()
        {
            rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(196f, 34f);

            frame = gameObject.AddComponent<Image>();
            frame.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelColor);
            frame.color = BattleHudStyle.PanelColor;
            frame.raycastTarget = false;
            BattleHudStyle.AddOutline(frame, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            shieldText = BattleHudStyle.CreateText("Shield", transform, "5", 18, TextAnchor.MiddleCenter, new Vector2(8f, -4f), new Vector2(28f, 26f), BattleHudStyle.TextColor);
            weaknessText = BattleHudStyle.CreateText("Weakness", transform, "弱点", 14, TextAnchor.MiddleLeft, new Vector2(42f, -6f), new Vector2(44f, 24f), BattleHudStyle.TextColor);
            BattleHudStyle.AddOutline(shieldText, Color.black, new Vector2(1f, -1f));
            BattleHudStyle.AddOutline(weaknessText, Color.black, new Vector2(1f, -1f));

            for (int i = 0; i < 5; i++)
            {
                GameObject cellRoot = new GameObject("Weakness Cell " + i);
                cellRoot.transform.SetParent(transform, false);
                RectTransform cellRect = cellRoot.AddComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0f, 1f);
                cellRect.sizeDelta = new Vector2(18f, 20f);
                cellRect.anchoredPosition = new Vector2(84f + i * 21f, -7f);

                Image cellBack = cellRoot.AddComponent<Image>();
                cellBack.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelStrongColor);
                cellBack.color = BattleHudStyle.PanelStrongColor;
                cellBack.raycastTarget = false;
                BattleHudStyle.AddOutline(cellBack, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

                Text cell = BattleHudStyle.CreateText("Glyph", cellRoot.transform, "?", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(18f, 20f), BattleHudStyle.TextColor);
                BattleHudStyle.AddOutline(cell, Color.black, new Vector2(1f, -1f));
                weaknessCells.Add(cell);
            }
        }

        public static Vector2 CalculateAnchoredPosition(Vector2 enemyUiPosition)
        {
            return CalculateAnchoredPosition(enemyUiPosition, 0);
        }

        public static Vector2 CalculateAnchoredPosition(Vector2 enemyUiPosition, int enemyIndex)
        {
            int safeIndex = Mathf.Clamp(enemyIndex, 0, 3);
            return WeaknessColumnStart + new Vector2(0f, -safeIndex * WeaknessRowGap);
        }

        public void Refresh(BattleUnit unit)
        {
            rect.anchoredPosition = CalculateAnchoredPosition(unit.UiPosition, unit.EnemyIndex);
            shieldText.text = Mathf.Max(0, unit.Shield).ToString();
            if (previousShield > 0 && unit.Shield == 0)
            {
                StartCoroutine(FlashBreak());
            }

            previousShield = unit.Shield;
            for (int i = 0; i < weaknessCells.Count; i++)
            {
                bool hasWeakness = unit.Weaknesses != null && i < unit.Weaknesses.Length;
                bool discovered = unit.WeaknessDiscovered != null && i < unit.WeaknessDiscovered.Length && unit.WeaknessDiscovered[i];
                weaknessCells[i].text = hasWeakness && discovered ? unit.Weaknesses[i] : "?";
                weaknessCells[i].color = discovered ? BattleHudStyle.GoldColor : BattleHudStyle.MutedTextColor;
            }
        }

        private IEnumerator FlashBreak()
        {
            float timer = 0f;
            while (timer < 0.38f)
            {
                timer += Time.unscaledDeltaTime;
                float pulse = Mathf.Sin(timer * 36f) * 0.5f + 0.5f;
                frame.color = Color.Lerp(BattleHudStyle.PanelColor, BattleHudStyle.SelectedColor, pulse);
                yield return null;
            }

            frame.color = BattleHudStyle.PanelColor;
        }
    }

    // 中文说明：上方技能名横幅，释放技能时淡入、停留、淡出。
    public class SkillNameBanner : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform rect;
        private Text text;
        private Coroutine routine;

        public void Build()
        {
            rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(256f, 42f);
            rect.anchoredPosition = new Vector2(-42f, -112f);

            Image back = gameObject.AddComponent<Image>();
            back.sprite = WitcherSpriteLibrary.GetSolidSprite(BattleHudStyle.PanelColor);
            back.color = BattleHudStyle.PanelColor;
            back.raycastTarget = false;
            BattleHudStyle.AddOutline(back, BattleHudStyle.BorderColor, new Vector2(1f, -1f));

            text = BattleHudStyle.CreateText("Skill Name", transform, "", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(256f, 42f), BattleHudStyle.TextColor);
            BattleHudStyle.AddOutline(text, Color.black, new Vector2(2f, -2f));

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        public void ShowSkillName(string skillName)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            text.text = string.IsNullOrEmpty(skillName) ? "行动" : skillName;
            routine = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            rect.localScale = Vector3.one * 0.94f;
            float timer = 0f;
            while (timer < 0.18f)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / 0.18f);
                canvasGroup.alpha = t;
                rect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.04f, t);
                yield return null;
            }

            rect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.8f);

            timer = 0f;
            while (timer < 0.28f)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / 0.28f);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }
        }
    }

    // 中文说明：HD-2D HUD 使用的轻量 UI 工厂和运行时图形生成器。
    internal static class BattleHudStyle
    {
        public static readonly Color32 PanelColor = new Color32(8, 24, 15, 202);
        public static readonly Color32 PanelStrongColor = new Color32(5, 15, 10, 230);
        public static readonly Color32 BorderColor = new Color32(235, 244, 232, 238);
        public static readonly Color32 RuleColor = new Color32(230, 238, 222, 146);
        public static readonly Color32 TextColor = new Color32(246, 248, 239, 255);
        public static readonly Color32 MutedTextColor = new Color32(205, 216, 201, 255);
        public static readonly Color32 SelectedColor = new Color32(52, 75, 40, 246);
        public static readonly Color32 GoldColor = new Color32(255, 226, 136, 255);

        public static T CreateBehaviour<T>(string name, Transform parent) where T : MonoBehaviour
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        public static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color color, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = obj.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite((Color32)color);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Vector2 position, Vector2 rectSize, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;
            Text text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        public static Sprite CreateTriangleSprite(Color32 color)
        {
            Texture2D texture = new Texture2D(32, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    bool inside = y <= texture.height - 1 - Mathf.Abs(x - texture.width * 0.5f) * 1.2f;
                    texture.SetPixel(x, y, inside ? color : new Color32(0, 0, 0, 0));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
        }

        public static void AddOutline(Graphic graphic, Color32 color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
