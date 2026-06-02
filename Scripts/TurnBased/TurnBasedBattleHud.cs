using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：创建并刷新回合制战斗界面，同时播放战斗 UI 动画和特效。
    public class TurnBasedBattleHud : MonoBehaviour
    {
        private const string HudName = "Turn Based Battle HUD";

        private readonly List<Text> enemyRows = new List<Text>();
        private readonly List<EnemyVisualSlot> enemySlots = new List<EnemyVisualSlot>();
        private readonly List<Button> commandButtons = new List<Button>();
        private readonly List<Image> commandButtonImages = new List<Image>();
        private readonly List<Button> skillButtons = new List<Button>();
        private readonly List<Image> timelineGems = new List<Image>();
        private readonly List<Text> timelineGemLabels = new List<Text>();
        private readonly List<PartyVisualSlot> partyVisualSlots = new List<PartyVisualSlot>();
        private static Sprite cachedBattleBackdrop;
        private static Sprite cachedFloorMist;
        private static Sprite cachedGroundShadow;
        private static Sprite cachedGroundGlow;
        private static Sprite[] cachedCommandButtonSprites;
        private static readonly Dictionary<BattleSkillId, Sprite[]> cachedSkillEffectFrames = new Dictionary<BattleSkillId, Sprite[]>();
        private static Sprite[] cachedGeraltIdleFrames;
        private static Sprite[] cachedGeraltSlashFrames;
        private static Sprite[] cachedGeraltHurtFrames;
        private static readonly Dictionary<GeraltAnimation, Sprite[]> cachedGeraltFrames = new Dictionary<GeraltAnimation, Sprite[]>();

        private TurnBasedBattleManager manager;
        private GeraltAnimator playerAnimator;
        private GameObject root;
        private GameObject skillPanel;
        private Text messageText;
        private Text playerText;
        private Text potionText;
        private Text currentTurnText;
        private Text nextTurnText;
        private GameObject victoryRewardPanel;
        private Text victoryRewardText;
        private Image currentActorPortrait;
        private Image playerFigure;
        private Vector2 playerFigureHomePosition;
        private bool playerFigureBusy;
        private Text playerDamageText;
        private Image playerHealthFill;
        private Image playerManaFill;
        private Image flameEffect;
        private Image flameImpactEffect;
        private BattleHUD hdBattleHud;
        private Color32 skillImpactColor = new Color32(255, 118, 32, 210);
        private IReadOnlyList<TurnBasedEnemyState> visibleEnemies;
        private int playerIdleIndex;
        private float playerIdleTimer;

        public bool SkillMenuOpen => skillPanel != null && skillPanel.activeSelf;

        // 中文说明：保存一个敌人在战斗界面中的图片、血条和动画运行状态。
        private class EnemyVisualSlot
        {
            public Image Image;
            public RectTransform Rect;
            public Vector2 HomePosition;
            public Text DamageText;
            public Image HealthBack;
            public Image HealthFill;
            public Image GroundShadow;
            public Image GroundGlow;
            public Image NameplateBack;
            public Image TargetReticle;
            public bool Busy;
            public int IdleIndex;
            public float IdleTimer;
        }

        private class PartyVisualSlot
        {
            public Image Image;
            public PartyMember Member;
            public int IdleIndex;
            public float IdleTimer;
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || visibleEnemies == null)
            {
                return;
            }

            UpdatePlayerIdleFigure();
            UpdatePartyIdleFigures();

            for (int i = 0; i < enemySlots.Count && i < visibleEnemies.Count; i++)
            {
                EnemyVisualSlot slot = enemySlots[i];
                TurnBasedEnemyState enemy = visibleEnemies[i];
                if (slot.Busy || !enemy.IsAlive || enemy.IdleFrames == null || enemy.IdleFrames.Length <= 1)
                {
                    continue;
                }

                slot.IdleTimer += Time.deltaTime;
                if (slot.IdleTimer < 0.15f)
                {
                    continue;
                }

                slot.IdleTimer = 0f;
                slot.IdleIndex = (slot.IdleIndex + 1) % enemy.IdleFrames.Length;
                slot.Image.sprite = enemy.IdleFrames[slot.IdleIndex];
            }
        }

        public static TurnBasedBattleHud CreateIfMissing(TurnBasedBattleManager target)
        {
            TurnBasedBattleHud existing = FindObjectOfType<TurnBasedBattleHud>();
            if (existing != null)
            {
                existing.manager = target;
                return existing;
            }

            GameObject hudObject = new GameObject(HudName);
            TurnBasedBattleHud hud = hudObject.AddComponent<TurnBasedBattleHud>();
            hud.manager = target;
            return hud;
        }

        public void Show(IReadOnlyList<TurnBasedEnemyState> enemies, GeraltController player, int potionCount)
        {
            playerAnimator = player == null ? null : player.GetComponent<GeraltAnimator>();
            if (root == null)
            {
                BuildHud();
            }

            root.SetActive(true);
            EnsureHdBattleHud();
            if (victoryRewardPanel != null)
            {
                victoryRewardPanel.SetActive(false);
            }

            Refresh(enemies, player, potionCount);
            SetCommandsEnabled(true);
            SetSelectedCommand(0);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            if (victoryRewardPanel != null)
            {
                victoryRewardPanel.SetActive(false);
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void ShowSkillName(string skillName)
        {
            EnsureHdBattleHud();
            hdBattleHud?.ShowSkillName(skillName);
        }

        public void ShowVictoryRewards(int experience, int gold, IReadOnlyList<string> loot)
        {
            if (victoryRewardPanel == null)
            {
                return;
            }

            if (victoryRewardText != null)
            {
                victoryRewardText.text =
                    "战斗胜利\n" +
                    $"经验 +{Mathf.Max(0, experience)}    金币 +{Mathf.Max(0, gold)}\n" +
                    BuildLootLine(loot);
            }

            victoryRewardPanel.SetActive(true);
        }

        public void SetCommandsEnabled(bool enabled)
        {
            for (int i = 0; i < commandButtons.Count; i++)
            {
                commandButtons[i].interactable = enabled;
            }

            SetSkillButtonsEnabled(enabled);
        }

        public void SetSelectedCommand(int selectedIndex)
        {
            for (int i = 0; i < commandButtonImages.Count; i++)
            {
                bool selected = i == selectedIndex;
                commandButtonImages[i].color = selected
                    ? new Color32(34, 77, 116, 246)
                    : new Color32(12, 14, 18, 214);
                commandButtonImages[i].rectTransform.localScale = selected ? new Vector3(1.045f, 1.045f, 1f) : Vector3.one;
            }
        }

        public void ShowSkillMenu()
        {
            if (skillPanel == null)
            {
                return;
            }

            skillPanel.SetActive(true);
            SetSkillButtonsEnabled(true);
            SetMessage("选择猎魔技能。  1连击  2火焰  3闪电  4专注");
        }

        public void HideSkillMenu()
        {
            if (skillPanel != null)
            {
                skillPanel.SetActive(false);
            }
        }

        private void SetSkillButtonsEnabled(bool enabled)
        {
            for (int i = 0; i < skillButtons.Count; i++)
            {
                skillButtons[i].interactable = enabled;
            }
        }

        private void EnsureHdBattleHud()
        {
            if (root == null)
            {
                return;
            }

            if (hdBattleHud == null)
            {
                hdBattleHud = BattleHUD.CreateIfMissing(root.transform);
                hdBattleHud.transform.SetAsLastSibling();
            }
        }

        private List<Vector2> GetEnemyHudPositions()
        {
            List<Vector2> positions = new List<Vector2>();
            for (int i = 0; i < enemySlots.Count; i++)
            {
                positions.Add(enemySlots[i].HomePosition);
            }

            return positions;
        }

        public void Refresh(IReadOnlyList<TurnBasedEnemyState> enemies, GeraltController player, int potionCount)
        {
            visibleEnemies = enemies;
            RefreshTurnTimeline();
            RefreshPartyVisuals();
            EnsureHdBattleHud();
            hdBattleHud?.RefreshFromBattle(manager, enemies, player, GetEnemyHudPositions());
            if (playerText != null && player != null)
            {
                playerText.text = $"HP {player.CurrentHealth}/{player.MaxHealth}    MP {player.CurrentMana}/{player.MaxMana}";
                SetFillWidth(playerHealthFill, player.MaxHealth <= 0 ? 0f : (float)player.CurrentHealth / player.MaxHealth, 190f);
                SetFillWidth(playerManaFill, player.MaxMana <= 0 ? 0f : (float)player.CurrentMana / player.MaxMana, 190f);
            }

            if (potionText != null)
            {
                potionText.text = $"药剂 x{potionCount}";
            }

            for (int i = 0; i < enemyRows.Count; i++)
            {
                if (i >= enemies.Count)
                {
                    enemyRows[i].text = string.Empty;
                    if (i < enemySlots.Count)
                    {
                        enemySlots[i].Image.gameObject.SetActive(false);
                        enemySlots[i].DamageText.gameObject.SetActive(false);
                        enemySlots[i].HealthBack.gameObject.SetActive(false);
                        enemySlots[i].GroundShadow.gameObject.SetActive(false);
                        enemySlots[i].GroundGlow.gameObject.SetActive(false);
                        enemySlots[i].NameplateBack.gameObject.SetActive(false);
                        enemySlots[i].TargetReticle?.gameObject.SetActive(false);
                    }
                    continue;
                }

                TurnBasedEnemyState enemy = enemies[i];
                string state = enemy.IsAlive ? $"HP {enemy.Health}/{enemy.MaxHealth}" : "已击败";
                enemyRows[i].text = $"{enemy.Name}\n{state}";
                enemyRows[i].color = enemy.IsAlive ? new Color32(233, 238, 229, 255) : new Color32(128, 126, 119, 255);

                if (i < enemySlots.Count)
                {
                    EnemyVisualSlot slot = enemySlots[i];
                    bool showEnemy = enemy.Sprite != null && enemy.IsAlive;
                    slot.Image.gameObject.SetActive(showEnemy);
                    slot.HealthBack.gameObject.SetActive(showEnemy);
                    slot.GroundShadow.gameObject.SetActive(showEnemy);
                    slot.GroundGlow.gameObject.SetActive(showEnemy);
                    slot.NameplateBack.gameObject.SetActive(enemy.Sprite != null);
                    slot.TargetReticle?.gameObject.SetActive(false);
                    float normalizedHealth = enemy.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)enemy.Health / enemy.MaxHealth);
                    SetFillWidth(slot.HealthFill, normalizedHealth, 118f);
                    slot.Rect.sizeDelta = enemy.VisualKind == TurnBasedEnemyVisualKind.BlackMoonKnight && i == 0
                        ? new Vector2(248f, 248f)
                        : new Vector2(184f, 184f);
                    if (!slot.Busy)
                    {
                        slot.Image.sprite = FirstFrame(enemy.IdleFrames, enemy.Sprite);
                        slot.Image.color = enemy.IsAlive ? Color.white : new Color32(120, 120, 120, 150);
                        slot.Rect.anchoredPosition = slot.HomePosition;
                        SetEnemyFacingScale(slot, 1f);
                    }
                }
            }
        }

        public IEnumerator PlayEnemyAttack(int enemyIndex)
        {
            if (!TryGetSlot(enemyIndex, out EnemyVisualSlot slot) || visibleEnemies == null || enemyIndex >= visibleEnemies.Count)
            {
                yield break;
            }

            TurnBasedEnemyState enemy = visibleEnemies[enemyIndex];
            yield return PlayEnemyFrames(slot, enemy.AttackFrames, enemy.Sprite, 0.085f, true, false);
        }

        public IEnumerator PlayEnemyHurt(int enemyIndex, float startDelay = 0f, int damage = 0)
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            if (!TryGetSlot(enemyIndex, out EnemyVisualSlot slot) || visibleEnemies == null || enemyIndex >= visibleEnemies.Count)
            {
                yield break;
            }

            TurnBasedEnemyState enemy = visibleEnemies[enemyIndex];
            if (damage > 0)
            {
                StartCoroutine(FloatDamageText(slot.DamageText, damage, false));
            }

            yield return PlayEnemyFrames(slot, enemy.HurtFrames, enemy.Sprite, 0.075f, false, true);
        }

        public IEnumerator PlayFlameSignEffect()
        {
            yield return PlaySkillEffect(BattleSkillId.FlameSign, -1);
        }

        public IEnumerator PlaySkillEffect(BattleSkillId skillId, int targetEnemyIndex)
        {
            if (flameEffect == null)
            {
                yield break;
            }

            Sprite[] frames = LoadSkillEffectFrames(skillId);
            Rect targetRect = GetSkillTargetRect(skillId, targetEnemyIndex);
            Vector2 effectPosition = GetSkillEffectPosition(skillId, targetRect);
            Vector2 effectSize = GetSkillEffectSize(skillId, targetRect);
            Vector3 effectScale = GetSkillEffectScale(skillId);
            float frameDuration = GetSkillEffectFrameDuration(skillId);
            skillImpactColor = GetSkillImpactColor(skillId);

            flameEffect.gameObject.SetActive(true);
            flameEffect.color = Color.white;
            flameEffect.rectTransform.anchoredPosition = effectPosition;
            flameEffect.rectTransform.sizeDelta = effectSize;
            flameEffect.rectTransform.localScale = effectScale;
            PrepareFlameImpact(effectPosition, targetRect);

            if (frames.Length == 0)
            {
                flameEffect.sprite = GetSkillFallbackSprite(skillId);
                UpdateFlameImpact(1f, true);
                yield return new WaitForSeconds(Mathf.Max(0.34f, frameDuration * 4f));
                flameEffect.gameObject.SetActive(false);
                HideFlameImpact();
                yield break;
            }

            for (int i = 0; i < frames.Length; i++)
            {
                flameEffect.sprite = frames[i];
                float t = frames.Length <= 1 ? 1f : (float)i / (frames.Length - 1);
                float pulseScale = 1f + Mathf.Sin(t * Mathf.PI) * 0.13f;
                flameEffect.rectTransform.localScale = new Vector3(effectScale.x, effectScale.y * pulseScale, effectScale.z);
                Color color = Color.white;
                color.a = t > 0.72f ? Mathf.Lerp(1f, 0.18f, (t - 0.72f) / 0.28f) : 1f;
                flameEffect.color = color;
                UpdateFlameImpact(t, t >= 0.35f);
                yield return new WaitForSeconds(frameDuration);
            }

            flameEffect.gameObject.SetActive(false);
            HideFlameImpact();
        }

        public IEnumerator PlayPlayerAttack()
        {
            yield return PlayPlayerFrames(GetPlayerFrames(GeraltAnimation.Slash), 0.085f, new Vector2(54f, -6f), false);
        }

        public IEnumerator PlayPlayerAttack(int enemyIndex)
        {
            yield return PlayPlayerFrames(GetPlayerFrames(GeraltAnimation.Slash), 0.085f, GetPlayerAttackMotion(enemyIndex), false);
        }

        public IEnumerator PlayPlayerComboSlash(int enemyIndex, int hitCount)
        {
            Sprite[] slashFrames = GetPlayerFrames(GeraltAnimation.Slash);
            Vector2 baseMotion = GetPlayerAttackMotion(enemyIndex);
            int safeHitCount = Mathf.Max(1, hitCount);
            for (int i = 0; i < safeHitCount; i++)
            {
                float motionScale = i == safeHitCount - 1 ? 1f : 0.82f + i * 0.08f;
                yield return PlayPlayerFrames(slashFrames, 0.04f, baseMotion * motionScale, false);
                if (i < safeHitCount - 1)
                {
                    yield return new WaitForSeconds(0.035f);
                }
            }
        }

        public IEnumerator PlayPlayerCast()
        {
            yield return PlayPlayerSkill(BattleSkillId.ArcaneBurst, BattleSkillAnimationKind.Cast);
        }

        public IEnumerator PlayPlayerSkill(BattleSkillId skillId, BattleSkillAnimationKind animationKind)
        {
            GeraltAnimation animation = GetGeraltAnimationForSkill(skillId, animationKind);
            yield return PlayPlayerFrames(
                GetPlayerFrames(animation),
                GetPlayerSkillFrameDuration(animation),
                GetPlayerSkillMotion(animation),
                animation == GeraltAnimation.ShieldSign);
        }

        public IEnumerator PlayPlayerHurt(int damage = 0)
        {
            if (damage > 0 && playerDamageText != null)
            {
                StartCoroutine(FloatDamageText(playerDamageText, damage, true));
            }

            yield return PlayPlayerFrames(GetPlayerFrames(GeraltAnimation.Hurt), 0.09f, new Vector2(22f, 0f), true);
        }

        private void BuildHud()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();

            root = CreateUiObject("Turn Battle Root", transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            StretchToParent(root.GetComponent<RectTransform>());

            Image dim = CreateCenteredImage("Turn Battle Dim", root.transform, new Vector2(2400f, 1400f), Vector2.zero, new Color32(3, 5, 8, 255));
            dim.sprite = GetBattleBackdropSprite();
            dim.color = Color.white;

            Image topCinemaBar = CreateCenteredImage("Battle Top Cinema Bar", root.transform, new Vector2(1120f, 52f), new Vector2(0f, 258f), new Color32(0, 0, 0, 186));
            topCinemaBar.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 220));
            topCinemaBar.raycastTarget = false;

            Image bottomCinemaBar = CreateCenteredImage("Battle Bottom Cinema Bar", root.transform, new Vector2(1120f, 46f), new Vector2(0f, -262f), new Color32(0, 0, 0, 160));
            bottomCinemaBar.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 220));
            bottomCinemaBar.raycastTarget = false;

            Image horizonGlow = CreateCenteredImage("Battle Horizon Glow", root.transform, new Vector2(1060f, 190f), new Vector2(0f, 78f), new Color32(42, 79, 101, 82));
            horizonGlow.sprite = GetFloorMistSprite();
            horizonGlow.raycastTarget = false;

            Image stageWash = CreateCenteredImage("Stage Wash", root.transform, new Vector2(820f, 250f), new Vector2(0f, 38f), new Color32(183, 48, 24, 46));
            stageWash.sprite = GetFloorMistSprite();
            stageWash.raycastTarget = false;

            Image playerAura = CreateCenteredImage("Battle Player Side Aura", root.transform, new Vector2(390f, 270f), new Vector2(-292f, -30f), new Color32(32, 133, 255, 48));
            playerAura.sprite = GetFloorMistSprite();
            playerAura.raycastTarget = false;

            Image enemyAura = CreateCenteredImage("Battle Enemy Side Aura", root.transform, new Vector2(450f, 286f), new Vector2(296f, 36f), new Color32(226, 55, 36, 42));
            enemyAura.sprite = GetFloorMistSprite();
            enemyAura.raycastTarget = false;

            Image floorPlate = CreateCenteredImage("Battle Floor Plate", root.transform, new Vector2(820f, 124f), new Vector2(0f, -20f), new Color32(10, 15, 18, 192));
            floorPlate.sprite = GetFloorMistSprite();
            floorPlate.raycastTarget = false;

            Image duelLine = CreateCenteredImage("Battle Duel Center Line", root.transform, new Vector2(680f, 2f), new Vector2(0f, -158f), new Color32(174, 190, 203, 70));
            duelLine.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(174, 190, 203, 96));
            duelLine.raycastTarget = false;

            Image frontFog = CreateCenteredImage("Battle Front Fog", root.transform, new Vector2(1260f, 122f), new Vector2(0f, -84f), new Color32(92, 119, 127, 54));
            frontFog.sprite = GetFloorMistSprite();
            frontFog.raycastTarget = false;

            // HD-2D HUD owns the visible turn order; the legacy timeline stays disabled to avoid duplicated top UI.

            Image playerGlow = CreateCenteredImage("Battle Player Ground Glow", root.transform, new Vector2(178f, 42f), new Vector2(304f, -108f), new Color32(42, 143, 255, 78));
            playerGlow.sprite = GetGroundGlowSprite();
            playerGlow.raycastTarget = false;

            Image playerShadow = CreateCenteredImage("Battle Player Ground Shadow", root.transform, new Vector2(150f, 32f), new Vector2(304f, -116f), new Color32(0, 0, 0, 178));
            playerShadow.sprite = GetGroundShadowSprite();
            playerShadow.raycastTarget = false;

            BuildPartySupportSlots();

            playerFigure = CreateCenteredImage("Battle Player Figure", root.transform, new Vector2(132f, 156f), new Vector2(304f, -34f), Color.white);
            playerFigure.sprite = GetPlayerIdleFrame();
            playerFigureHomePosition = playerFigure.rectTransform.anchoredPosition;
            playerFigure.preserveAspect = true;
            playerFigure.raycastTarget = false;
            SetPlayerFacingScale(-1f);
            playerDamageText = CreateText("Player Damage Text", playerFigure.transform, string.Empty, 32, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(160f, 52f));
            playerDamageText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            playerDamageText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            playerDamageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            playerDamageText.color = new Color32(255, 82, 74, 255);
            playerDamageText.gameObject.SetActive(false);
            AddOutline(playerDamageText, new Color32(0, 0, 0, 255), new Vector2(3f, -3f));
            SetPlayerFacingScale(-1f);

            Image playerPanel = CreateImage("Player Battle Plate", root.transform, new Vector2(246f, 118f), new Vector2(22f, -78f), new Color32(7, 11, 17, 218));
            playerPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(7, 11, 17, 238));
            AddOutline(playerPanel, new Color32(93, 145, 207, 255), new Vector2(2f, -2f));

            Text playerName = CreateText("Player Battle Name", playerPanel.transform, "猎魔人", 24, TextAnchor.MiddleLeft, new Vector2(18f, -12f), new Vector2(176f, 28f));
            playerName.color = new Color32(255, 218, 138, 255);
            AddOutline(playerName, Color.black, new Vector2(1f, -1f));

            Text bpText = CreateText("Player Battle BP", playerPanel.transform, "回合", 15, TextAnchor.MiddleRight, new Vector2(132f, -16f), new Vector2(88f, 24f));
            bpText.color = new Color32(225, 238, 255, 255);
            AddOutline(bpText, Color.black, new Vector2(1f, -1f));

            Image playerHealthBack = CreateImage("Player Battle HP Back", playerPanel.transform, new Vector2(198f, 13f), new Vector2(20f, -50f), new Color32(42, 5, 8, 245));
            playerHealthBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(42, 5, 8, 245));
            playerHealthFill = CreateImage("Player Battle HP Fill", playerHealthBack.transform, new Vector2(190f, 7f), new Vector2(4f, -3f), new Color32(232, 34, 45, 255));
            playerHealthFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            Image playerManaBack = CreateImage("Player Battle MP Back", playerPanel.transform, new Vector2(198f, 13f), new Vector2(20f, -74f), new Color32(4, 18, 52, 245));
            playerManaBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(4, 18, 52, 245));
            playerManaFill = CreateImage("Player Battle MP Fill", playerManaBack.transform, new Vector2(190f, 7f), new Vector2(4f, -3f), new Color32(46, 145, 255, 255));
            playerManaFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            playerText = CreateText("Battle Player Stats", playerPanel.transform, "HP 100/100    MP 100/100", 15, TextAnchor.MiddleLeft, new Vector2(18f, -94f), new Vector2(220f, 20f));
            playerText.color = new Color32(226, 241, 238, 255);
            AddOutline(playerText, Color.black, new Vector2(1f, -1f));
            playerPanel.gameObject.SetActive(false);

            enemySlots.Clear();
            enemyRows.Clear();
            for (int i = 0; i < 4; i++)
            {
                Vector2 slotPosition = GetEnemyStagePosition(i);
                Image groundGlow = CreateCenteredImage($"Battle Enemy Ground Glow {i + 1}", root.transform, new Vector2(204f, 48f), slotPosition + new Vector2(0f, -72f), new Color32(226, 72, 34, 76));
                groundGlow.sprite = GetGroundGlowSprite();
                groundGlow.raycastTarget = false;
                groundGlow.gameObject.SetActive(false);

                Image groundShadow = CreateCenteredImage($"Battle Enemy Ground Shadow {i + 1}", root.transform, new Vector2(164f, 36f), slotPosition + new Vector2(0f, -80f), new Color32(0, 0, 0, 174));
                groundShadow.sprite = GetGroundShadowSprite();
                groundShadow.raycastTarget = false;
                groundShadow.gameObject.SetActive(false);

                Image targetReticle = CreateCenteredImage($"Battle Target Reticle {i + 1}", root.transform, new Vector2(190f, 58f), slotPosition + new Vector2(0f, -70f), new Color32(255, 160, 62, 0));
                targetReticle.sprite = GetGroundGlowSprite();
                targetReticle.raycastTarget = false;
                targetReticle.gameObject.SetActive(false);

                Image enemyImage = CreateCenteredImage($"Battle Enemy Sprite {i + 1}", root.transform, new Vector2(198f, 198f), slotPosition, Color.white);
                enemyImage.preserveAspect = true;
                enemyImage.raycastTarget = false;
                enemyImage.gameObject.SetActive(false);
                Text damageText = CreateText($"Enemy Damage Text {i + 1}", enemyImage.transform, string.Empty, 32, TextAnchor.MiddleCenter, new Vector2(0f, 64f), new Vector2(160f, 54f));
                damageText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                damageText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                damageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                damageText.color = new Color32(255, 80, 54, 255);
                damageText.gameObject.SetActive(false);
                AddOutline(damageText, new Color32(0, 0, 0, 255), new Vector2(3f, -3f));

                Image healthBack = CreateCenteredImage($"Battle Enemy HP Back {i + 1}", enemyImage.transform, new Vector2(128f, 12f), new Vector2(0f, -72f), new Color32(12, 6, 7, 230));
                healthBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(12, 6, 7, 230));
                healthBack.raycastTarget = false;
                AddOutline(healthBack, new Color32(0, 0, 0, 220), new Vector2(1f, -1f));

                GameObject healthFillObject = CreateUiObject($"Battle Enemy HP Fill {i + 1}", healthBack.transform, new Vector2(118f, 5f), new Vector2(-59f, 0f), new Vector2(0.5f, 0.5f));
                RectTransform healthFillRect = healthFillObject.GetComponent<RectTransform>();
                healthFillRect.pivot = new Vector2(0f, 0.5f);
                Image healthFill = healthFillObject.AddComponent<Image>();
                healthFill.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(226, 34, 43, 255));
                healthFill.color = new Color32(226, 34, 43, 255);
                healthFill.raycastTarget = false;
                Image nameplateBack = CreateCenteredImage($"Battle Enemy Nameplate {i + 1}", root.transform, new Vector2(164f, 64f), slotPosition + new Vector2(0f, 126f), new Color32(8, 10, 13, 208));
                nameplateBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 10, 13, 226));
                nameplateBack.raycastTarget = false;
                nameplateBack.gameObject.SetActive(false);
                AddOutline(nameplateBack, new Color32(74, 110, 155, 255), new Vector2(2f, -2f));

                Text row = CreateText($"Enemy Stage Label {i + 1}", nameplateBack.transform, string.Empty, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(154f, 58f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                row.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                row.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                row.color = new Color32(233, 238, 229, 255);
                AddOutline(row, Color.black, new Vector2(1f, -1f));
                enemyRows.Add(row);

                enemySlots.Add(new EnemyVisualSlot
                {
                    Image = enemyImage,
                    Rect = enemyImage.rectTransform,
                    HomePosition = enemyImage.rectTransform.anchoredPosition,
                    DamageText = damageText,
                    HealthBack = healthBack,
                    HealthFill = healthFill,
                    GroundShadow = groundShadow,
                    GroundGlow = groundGlow,
                    NameplateBack = nameplateBack,
                    TargetReticle = targetReticle
                });
            }

            flameEffect = CreateCenteredImage("Flame Sign Battle Effect", root.transform, new Vector2(640f, 142f), new Vector2(-74f, 72f), Color.white);
            flameEffect.preserveAspect = true;
            flameEffect.raycastTarget = false;
            flameEffect.gameObject.SetActive(false);

            flameImpactEffect = CreateCenteredImage("Flame Sign Impact", root.transform, new Vector2(128f, 128f), Vector2.zero, new Color32(255, 118, 32, 0));
            flameImpactEffect.sprite = GetGroundGlowSprite();
            flameImpactEffect.raycastTarget = false;
            flameImpactEffect.gameObject.SetActive(false);

            Image commandPanel = CreateImage("Command Panel", root.transform, new Vector2(432f, 122f), new Vector2(224f, -394f), new Color32(4, 9, 15, 172));
            commandPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(4, 9, 15, 172));
            AddOutline(commandPanel, new Color32(91, 137, 160, 160), new Vector2(1f, -1f));

            Image commandInnerGlow = CreateImage("Command Panel Inner Glow", commandPanel.transform, new Vector2(418f, 108f), new Vector2(7f, -7f), new Color32(17, 28, 38, 38));
            commandInnerGlow.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(17, 28, 38, 62));
            commandInnerGlow.raycastTarget = false;

            Image commandTopRule = CreateImage("Command Panel Top Rule", commandPanel.transform, new Vector2(408f, 1f), new Vector2(12f, -44f), new Color32(118, 151, 186, 104));
            commandTopRule.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(118, 151, 186, 160));
            commandTopRule.raycastTarget = false;

            Image commandGoldRule = CreateImage("Command Panel Gold Rule", commandPanel.transform, new Vector2(82f, 1f), new Vector2(22f, -42f), new Color32(255, 195, 92, 134));
            commandGoldRule.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 195, 92, 190));
            commandGoldRule.raycastTarget = false;

            Text commandTitle = CreateText("Battle Command Title", commandPanel.transform, "行动", 20, TextAnchor.MiddleLeft, new Vector2(22f, -12f), new Vector2(86f, 28f));
            commandTitle.color = new Color32(255, 214, 132, 255);
            AddOutline(commandTitle, Color.black, new Vector2(1f, -1f));

            potionText = CreateText("Battle Potion Count", commandPanel.transform, "药剂 x3", 14, TextAnchor.MiddleRight, new Vector2(322f, -14f), new Vector2(86f, 24f));
            potionText.color = new Color32(183, 219, 255, 255);
            AddOutline(potionText, Color.black, new Vector2(1f, -1f));

            messageText = CreateText("Battle Message", commandPanel.transform, "选择行动。", 14, TextAnchor.UpperLeft, new Vector2(104f, -18f), new Vector2(210f, 32f));
            messageText.color = new Color32(255, 246, 214, 255);
            AddOutline(messageText, Color.black, new Vector2(1f, -1f));

            commandButtons.Clear();
            commandButtonImages.Clear();
            AddCommandButton(commandPanel.transform, "1 攻击", TurnBattleAction.Attack, new Vector2(18f, -58f));
            AddCommandButton(commandPanel.transform, "2 技能", TurnBattleAction.FlameSign, new Vector2(222f, -58f));
            AddCommandButton(commandPanel.transform, "3 道具", TurnBattleAction.Item, new Vector2(18f, -90f));
            AddCommandButton(commandPanel.transform, "4 防御", TurnBattleAction.Defend, new Vector2(222f, -90f));
            BuildSkillPanel(commandPanel.transform);
            BuildVictoryRewardPanel(root.transform);
            EnsureHdBattleHud();

            root.SetActive(false);
        }

        private void BuildVictoryRewardPanel(Transform parent)
        {
            Image panel = CreateCenteredImage("Battle Victory Reward Panel", parent, new Vector2(356f, 128f), new Vector2(0f, 36f), new Color32(5, 7, 10, 226));
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 7, 10, 226));
            panel.raycastTarget = false;
            AddOutline(panel, new Color32(198, 154, 76, 255), new Vector2(3f, -3f));
            victoryRewardPanel = panel.gameObject;

            Image inner = CreateImage("Battle Victory Reward Inner", panel.transform, new Vector2(336f, 106f), new Vector2(10f, -10f), new Color32(24, 18, 13, 156));
            inner.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(24, 18, 13, 156));
            inner.raycastTarget = false;

            Image topRule = CreateImage("Battle Victory Reward Gold Rule", panel.transform, new Vector2(292f, 2f), new Vector2(32f, -42f), new Color32(255, 204, 101, 180));
            topRule.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 204, 101, 210));
            topRule.raycastTarget = false;

            victoryRewardText = CreateText("Battle Victory Reward Text", panel.transform, "战斗胜利", 18, TextAnchor.MiddleCenter, new Vector2(12f, -12f), new Vector2(332f, 100f));
            victoryRewardText.color = new Color32(255, 231, 170, 255);
            AddOutline(victoryRewardText, Color.black, new Vector2(2f, -2f));
            victoryRewardPanel.SetActive(false);
        }

        private static string BuildLootLine(IReadOnlyList<string> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "战利品：无";
            }

            string line = "战利品：";
            for (int i = 0; i < loot.Count; i++)
            {
                if (string.IsNullOrEmpty(loot[i]))
                {
                    continue;
                }

                if (line.Length > 4)
                {
                    line += "、";
                }

                line += loot[i];
            }

            return line.Length > 4 ? line : "战利品：无";
        }

        private void BuildTurnTimeline(Transform parent)
        {
            timelineGems.Clear();
            timelineGemLabels.Clear();

            Image currentPlate = CreateImage("Battle Current Turn Plate", parent, new Vector2(132f, 40f), new Vector2(22f, -24f), new Color32(5, 8, 12, 204));
            currentPlate.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 228));
            AddOutline(currentPlate, new Color32(124, 149, 178, 255), new Vector2(2f, -2f));

            currentTurnText = CreateText("Battle Current Turn Text", currentPlate.transform, "第1手", 18, TextAnchor.MiddleRight, new Vector2(48f, -8f), new Vector2(70f, 24f));
            currentTurnText.color = new Color32(228, 236, 245, 255);
            AddOutline(currentTurnText, Color.black, new Vector2(1f, -1f));

            Image activeGem = CreateImage("Battle Current Actor Gem", currentPlate.transform, new Vector2(44f, 44f), new Vector2(0f, 2f), new Color32(20, 107, 208, 224));
            activeGem.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(20, 107, 208, 224));
            activeGem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            AddOutline(activeGem, new Color32(118, 212, 255, 255), new Vector2(2f, -2f));

            currentActorPortrait = CreateImage("Battle Current Actor Portrait", activeGem.transform, new Vector2(48f, 48f), new Vector2(5f, -5f), Color.white);
            currentActorPortrait.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            currentActorPortrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            currentActorPortrait.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            currentActorPortrait.rectTransform.anchoredPosition = Vector2.zero;
            currentActorPortrait.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            currentActorPortrait.preserveAspect = true;
            currentActorPortrait.raycastTarget = false;

            Image rail = CreateImage("Battle Turn Timeline Rail", parent, new Vector2(548f, 2f), new Vector2(164f, -44f), new Color32(157, 164, 172, 88));
            rail.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(157, 164, 172, 120));
            rail.raycastTarget = false;

            Image nextPlate = CreateImage("Battle Next Turn Plate", parent, new Vector2(112f, 28f), new Vector2(404f, -18f), new Color32(5, 8, 12, 154));
            nextPlate.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 188));
            AddOutline(nextPlate, new Color32(80, 88, 105, 210), new Vector2(1f, -1f));

            nextTurnText = CreateText("Battle Next Turn Text", nextPlate.transform, "等待出手", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(112f, 28f));
            nextTurnText.color = new Color32(210, 215, 220, 255);
            AddOutline(nextTurnText, Color.black, new Vector2(1f, -1f));

            for (int i = 0; i < 5; i++)
            {
                Image gem = CreateImage($"Battle Timeline Gem {i + 1}", parent, new Vector2(28f, 28f), new Vector2(188f + i * 42f, -30f), GetTimelineGemColor(i));
                gem.sprite = WitcherSpriteLibrary.GetSolidSprite(GetTimelineGemColor(i));
                gem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                AddOutline(gem, new Color32(26, 33, 43, 255), new Vector2(1f, -1f));
                timelineGems.Add(gem);

                Text label = CreateText($"Battle Timeline Label {i + 1}", gem.transform, string.Empty, 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(34f, 24f));
                label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
                label.color = Color.white;
                AddOutline(label, Color.black, new Vector2(1f, -1f));
                timelineGemLabels.Add(label);
            }
        }

        private void RefreshTurnTimeline()
        {
            if (manager == null)
            {
                return;
            }

            List<TurnBattleTimelineEntry> preview = manager.GetTimelinePreview(timelineGems.Count);
            if (currentTurnText != null)
            {
                currentTurnText.text = $"第{manager.TurnNumber}手";
            }

            if (nextTurnText != null)
            {
                nextTurnText.text = preview.Count > 0 ? $"当前 {preview[0].Name}" : "等待出手";
            }

            if (currentActorPortrait != null)
            {
                currentActorPortrait.sprite = preview.Count > 0 ? GetTimelinePortraitSprite(preview[0]) : null;
                currentActorPortrait.color = currentActorPortrait.sprite == null ? new Color32(255, 255, 255, 0) : Color.white;
            }

            for (int i = 0; i < timelineGems.Count; i++)
            {
                bool hasEntry = i < preview.Count;
                Image gem = timelineGems[i];
                Text label = i < timelineGemLabels.Count ? timelineGemLabels[i] : null;
                if (!hasEntry)
                {
                    gem.color = new Color32(54, 59, 68, 130);
                    if (label != null)
                    {
                        label.text = "-";
                    }
                    continue;
                }

                TurnBattleTimelineEntry entry = preview[i];
                gem.color = GetTimelineEntryColor(entry, i == 0);
                if (label != null)
                {
                    label.text = GetTimelineEntryLabel(entry);
                    label.color = i == 0 ? new Color32(255, 232, 152, 255) : new Color32(230, 238, 245, 255);
                }
            }
        }

        private static Color32 GetTimelineEntryColor(TurnBattleTimelineEntry entry, bool current)
        {
            if (entry.IsPlayer)
            {
                return current ? new Color32(38, 148, 255, 255) : new Color32(28, 96, 188, 225);
            }

            return current ? new Color32(198, 45, 64, 255) : new Color32(104, 33, 52, 225);
        }

        private static string GetTimelineEntryLabel(TurnBattleTimelineEntry entry)
        {
            if (entry.IsPlayer)
            {
                return "猎";
            }

            if (!string.IsNullOrEmpty(entry.Name))
            {
                return entry.Name.Substring(0, 1);
            }

            return "怪";
        }

        private Sprite GetTimelinePortraitSprite(TurnBattleTimelineEntry entry)
        {
            if (entry.IsPlayer)
            {
                return GetPlayerIdleFrame();
            }

            if (visibleEnemies == null || entry.EnemyIndex < 0 || entry.EnemyIndex >= visibleEnemies.Count)
            {
                return null;
            }

            TurnBasedEnemyState enemy = visibleEnemies[entry.EnemyIndex];
            return FirstFrame(enemy.IdleFrames, enemy.Sprite);
        }

        private static Color32 GetTimelineGemColor(int index)
        {
            switch (index)
            {
                case 0:
                    return new Color32(34, 112, 204, 230);
                case 1:
                case 3:
                    return new Color32(124, 25, 40, 220);
                default:
                    return new Color32(70, 38, 88, 220);
            }
        }

        private static Vector2 GetEnemyStagePosition(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector2(-322f, 42f);
                case 1:
                    return new Vector2(-184f, -32f);
                case 2:
                    return new Vector2(-228f, 112f);
                default:
                    return new Vector2(-392f, -42f);
            }
        }

        private IEnumerator PlayEnemyFrames(EnemyVisualSlot slot, Sprite[] frames, Sprite fallback, float frameDuration, bool attackMotion, bool hurtMotion)
        {
            slot.Busy = true;
            Coroutine targetPulse = hurtMotion ? StartCoroutine(PlayTargetPulse(slot)) : null;
            Vector2 home = slot.HomePosition;
            Vector2 motion = attackMotion ? GetEnemyAttackMotion(slot) : new Vector2(-24f, 0f);
            Sprite[] safeFrames = frames != null && frames.Length > 0 ? frames : new[] { fallback };

            for (int i = 0; i < safeFrames.Length; i++)
            {
                if (safeFrames[i] != null)
                {
                    slot.Image.sprite = safeFrames[i];
                }

                float t = safeFrames.Length <= 1 ? 1f : (float)i / (safeFrames.Length - 1);
                float pulse = Mathf.Sin(t * Mathf.PI);
                slot.Rect.anchoredPosition = home + motion * pulse;
                SetEnemyFacingScale(slot, 1f + (attackMotion ? 0.1f : 0.065f) * pulse);
                slot.Image.color = hurtMotion && i % 2 == 0 ? new Color32(255, 235, 222, 255) : Color.white;
                yield return new WaitForSeconds(frameDuration);
            }

            if (targetPulse != null)
            {
                StopCoroutine(targetPulse);
            }

            slot.Rect.anchoredPosition = home;
            SetEnemyFacingScale(slot, 1f);
            slot.Image.color = Color.white;
            slot.TargetReticle?.gameObject.SetActive(false);
            slot.Busy = false;
        }

        private IEnumerator PlayTargetPulse(EnemyVisualSlot slot)
        {
            if (slot == null || slot.TargetReticle == null)
            {
                yield break;
            }

            RectTransform rect = slot.TargetReticle.rectTransform;
            slot.TargetReticle.gameObject.SetActive(true);
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime;
                float pulse = 0.5f + Mathf.Sin(timer * 18f) * 0.5f;
                rect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.18f, pulse);
                slot.TargetReticle.color = new Color32(255, 148, 54, (byte)Mathf.RoundToInt(Mathf.Lerp(64f, 178f, pulse)));
                yield return null;
            }
        }

        private IEnumerator FloatDamageText(Text damageText, int damage, bool counterEnemyFacing)
        {
            if (damageText == null || damage <= 0)
            {
                yield break;
            }

            RectTransform rect = damageText.rectTransform;
            Vector2 start = new Vector2(0f, 66f);
            Vector2 end = new Vector2(0f, 116f);
            damageText.text = $"-{damage}";
            damageText.color = new Color32(255, 72, 42, 255);
            rect.anchoredPosition = start;
            rect.localScale = GetDamageTextScale(1.28f, counterEnemyFacing);
            damageText.gameObject.SetActive(true);

            const float duration = 0.72f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float pop = Mathf.Sin(Mathf.Clamp01(t * 1.8f) * Mathf.PI) * 0.18f;
                rect.anchoredPosition = Vector2.Lerp(start, end, t);
                rect.localScale = GetDamageTextScale(Mathf.Lerp(1.28f + pop, 0.92f, t), counterEnemyFacing);

                Color color = damageText.color;
                color.a = t < 0.45f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.45f) / 0.55f);
                damageText.color = color;
                yield return null;
            }

            damageText.gameObject.SetActive(false);
        }

        private static Sprite GetBattleBackdropSprite()
        {
            if (cachedBattleBackdrop != null)
            {
                return cachedBattleBackdrop;
            }

            Texture2D texture = new Texture2D(8, 96, TextureFormat.RGBA32, false);
            Color top = new Color32(8, 15, 23, 255);
            Color center = new Color32(12, 23, 30, 255);
            Color bottom = new Color32(2, 4, 7, 255);
            for (int y = 0; y < texture.height; y++)
            {
                float t = (float)y / (texture.height - 1);
                Color color = t < 0.58f
                    ? Color.Lerp(bottom, center, t / 0.58f)
                    : Color.Lerp(center, top, (t - 0.58f) / 0.42f);
                for (int x = 0; x < texture.width; x++)
                {
                    float vignette = Mathf.Abs((x / (float)(texture.width - 1)) - 0.5f) * 0.18f;
                    texture.SetPixel(x, y, Color.Lerp(color, Color.black, vignette));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            cachedBattleBackdrop = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
            cachedBattleBackdrop.name = "RuntimeBattleBackdrop";
            return cachedBattleBackdrop;
        }

        private static Sprite GetFloorMistSprite()
        {
            if (cachedFloorMist != null)
            {
                return cachedFloorMist;
            }

            cachedFloorMist = CreateRadialSprite("RuntimeBattleMist", 96, 28, 0.88f);
            return cachedFloorMist;
        }

        private static Sprite GetGroundShadowSprite()
        {
            if (cachedGroundShadow != null)
            {
                return cachedGroundShadow;
            }

            cachedGroundShadow = CreateRadialSprite("RuntimeEnemyGroundShadow", 96, 28, 1f);
            return cachedGroundShadow;
        }

        private static Sprite GetGroundGlowSprite()
        {
            if (cachedGroundGlow != null)
            {
                return cachedGroundGlow;
            }

            cachedGroundGlow = CreateRadialSprite("RuntimeEnemyGroundGlow", 96, 28, 0.78f);
            return cachedGroundGlow;
        }

        private static Sprite CreateRadialSprite(string name, int width, int height, float power)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float ny = ((y + 0.5f) / height - 0.5f) * 2f;
                for (int x = 0; x < width; x++)
                {
                    float nx = ((x + 0.5f) / width - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny * 3.4f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), power);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 96f);
            sprite.name = name;
            return sprite;
        }

        private static Sprite GetCommandButtonSprite(TurnBattleAction action)
        {
            int index = CommandButtonIndex(action);
            Sprite[] sprites = GetCommandButtonSprites();
            if (index >= 0 && index < sprites.Length && sprites[index] != null)
            {
                return sprites[index];
            }

            return WitcherSpriteLibrary.GetSolidSprite(new Color32(9, 10, 12, 235));
        }

        private static Sprite[] GetCommandButtonSprites()
        {
            if (cachedCommandButtonSprites != null)
            {
                return cachedCommandButtonSprites;
            }

            cachedCommandButtonSprites = new Sprite[5];
            string absolutePath = Path.Combine(Application.dataPath, "Art/UI/BattleCommandButtons.png");
            if (!File.Exists(absolutePath))
            {
                return cachedCommandButtonSprites;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return cachedCommandButtonSprites;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Rect[] cropRects =
            {
                TopLeftRect(texture, 33f, 280f, 306f, 410f),
                TopLeftRect(texture, 359f, 280f, 306f, 410f),
                TopLeftRect(texture, 685f, 280f, 306f, 410f),
                TopLeftRect(texture, 1011f, 280f, 306f, 410f),
                TopLeftRect(texture, 1337f, 280f, 306f, 410f)
            };

            for (int i = 0; i < cropRects.Length; i++)
            {
                cachedCommandButtonSprites[i] = Sprite.Create(texture, cropRects[i], new Vector2(0.5f, 0.5f), 100f);
                cachedCommandButtonSprites[i].name = $"BattleCommandButton_{i + 1}";
            }

            return cachedCommandButtonSprites;
        }

        private static Rect TopLeftRect(Texture2D texture, float x, float y, float width, float height)
        {
            return new Rect(x, texture.height - y - height, width, height);
        }

        private static int CommandButtonIndex(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.Attack:
                    return 0;
                case TurnBattleAction.FlameSign:
                    return 1;
                case TurnBattleAction.Defend:
                    return 2;
                case TurnBattleAction.Item:
                    return 3;
                case TurnBattleAction.Escape:
                    return 4;
                default:
                    return -1;
            }
        }

        private static void SetFillWidth(Image fill, float normalized, float fullWidth)
        {
            if (fill == null)
            {
                return;
            }

            Vector2 size = fill.rectTransform.sizeDelta;
            size.x = Mathf.Max(0f, fullWidth * Mathf.Clamp01(normalized));
            fill.rectTransform.sizeDelta = size;
        }

        private bool TryGetSlot(int index, out EnemyVisualSlot slot)
        {
            if (index >= 0 && index < enemySlots.Count)
            {
                slot = enemySlots[index];
                return true;
            }

            slot = null;
            return false;
        }

        private static Sprite FirstFrame(Sprite[] frames, Sprite fallback)
        {
            return frames != null && frames.Length > 0 && frames[0] != null ? frames[0] : fallback;
        }

        // 中文说明：HD-2D 舞台里敌人站在左侧，因此保持朝向右侧玩家。
        private static void SetEnemyFacingScale(EnemyVisualSlot slot, float scale)
        {
            float safeScale = Mathf.Max(0.01f, scale);
            slot.Rect.localScale = new Vector3(safeScale, safeScale, 1f);
            slot.HealthBack.rectTransform.localScale = Vector3.one;
            slot.DamageText.rectTransform.localScale = Vector3.one;
        }

        private static Vector3 GetCounterFacingScale(float scale)
        {
            return new Vector3(-scale, scale, 1f);
        }

        private static Vector3 GetDamageTextScale(float scale, bool counterEnemyFacing)
        {
            return counterEnemyFacing ? GetCounterFacingScale(scale) : Vector3.one * scale;
        }

        // 中文说明：玩家固定站在右侧，战斗立绘翻向左侧敌人。
        private void SetPlayerFacingScale(float scale)
        {
            float safeScale = Mathf.Abs(scale) <= 0.01f ? 1f : Mathf.Abs(scale);
            playerFigure.rectTransform.localScale = new Vector3(-safeScale, safeScale, 1f);
            if (playerDamageText != null && !playerDamageText.gameObject.activeSelf)
            {
                playerDamageText.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        private Vector2 GetPlayerAttackMotion(int enemyIndex)
        {
            if (!TryGetSlot(enemyIndex, out EnemyVisualSlot slot))
            {
                return new Vector2(-92f, -6f);
            }

            Vector2 destination = slot.HomePosition + new Vector2(118f, -10f);
            Vector2 motion = destination - playerFigureHomePosition;
            motion.x = Mathf.Clamp(motion.x, -540f, -92f);
            motion.y = Mathf.Clamp(motion.y, -28f, 24f);
            return motion;
        }

        private Vector2 GetEnemyAttackMotion(EnemyVisualSlot slot)
        {
            Vector2 destination = playerFigureHomePosition + new Vector2(-118f, -10f);
            Vector2 motion = destination - slot.HomePosition;
            motion.x = Mathf.Clamp(motion.x, 96f, 640f);
            motion.y = Mathf.Clamp(motion.y, -34f, 18f);
            return motion;
        }

        private Rect GetLivingEnemyVisualRect()
        {
            bool found = false;
            float minX = 0f;
            float maxX = 0f;
            float minY = 0f;
            float maxY = 0f;

            for (int i = 0; i < enemySlots.Count; i++)
            {
                if (visibleEnemies == null || i >= visibleEnemies.Count || !visibleEnemies[i].IsAlive)
                {
                    continue;
                }

                EnemyVisualSlot slot = enemySlots[i];
                if (!slot.Image.gameObject.activeSelf)
                {
                    continue;
                }

                Vector2 center = slot.Rect.anchoredPosition;
                Vector2 size = slot.Rect.sizeDelta;
                float halfWidth = size.x * 0.5f;
                float halfHeight = size.y * 0.5f;
                if (!found)
                {
                    minX = center.x - halfWidth;
                    maxX = center.x + halfWidth;
                    minY = center.y - halfHeight;
                    maxY = center.y + halfHeight;
                    found = true;
                }
                else
                {
                    minX = Mathf.Min(minX, center.x - halfWidth);
                    maxX = Mathf.Max(maxX, center.x + halfWidth);
                    minY = Mathf.Min(minY, center.y - halfHeight);
                    maxY = Mathf.Max(maxY, center.y + halfHeight);
                }
            }

            return found ? Rect.MinMaxRect(minX, minY, maxX, maxY) : Rect.MinMaxRect(-250f, 2f, 250f, 142f);
        }

        private bool TryGetFirstLivingEnemyImpactPoint(out Vector2 impactPoint, out Rect targetRect)
        {
            for (int i = 0; i < enemySlots.Count; i++)
            {
                if (visibleEnemies == null || i >= visibleEnemies.Count || !visibleEnemies[i].IsAlive)
                {
                    continue;
                }

                EnemyVisualSlot slot = enemySlots[i];
                if (!slot.Image.gameObject.activeSelf)
                {
                    continue;
                }

                Vector2 center = slot.Rect.anchoredPosition;
                Vector2 size = slot.Rect.sizeDelta;
                targetRect = Rect.MinMaxRect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, center.x + size.x * 0.5f, center.y + size.y * 0.5f);
                impactPoint = new Vector2(targetRect.xMin + size.x * 0.28f, center.y + size.y * 0.04f);
                return true;
            }

            targetRect = GetLivingEnemyVisualRect();
            impactPoint = new Vector2(targetRect.xMin + targetRect.width * 0.28f, targetRect.center.y);
            return false;
        }

        private Rect GetSkillTargetRect(BattleSkillId skillId, int targetEnemyIndex)
        {
            if (skillId == BattleSkillId.HunterFocus)
            {
                Vector2 playerCenter = playerFigureHomePosition + new Vector2(0f, -8f);
                return Rect.MinMaxRect(playerCenter.x - 86f, playerCenter.y - 94f, playerCenter.x + 86f, playerCenter.y + 94f);
            }

            if (targetEnemyIndex >= 0 && TryGetSlot(targetEnemyIndex, out EnemyVisualSlot slot) && slot.Image.gameObject.activeSelf)
            {
                Vector2 center = slot.Rect.anchoredPosition;
                Vector2 size = slot.Rect.sizeDelta;
                return Rect.MinMaxRect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, center.x + size.x * 0.5f, center.y + size.y * 0.5f);
            }

            return skillId == BattleSkillId.FlameSign ? GetLivingEnemyVisualRect() : GetFirstLivingEnemyRect();
        }

        private Rect GetFirstLivingEnemyRect()
        {
            TryGetFirstLivingEnemyImpactPoint(out _, out Rect targetRect);
            return targetRect;
        }

        private static Vector2 GetSkillEffectPosition(BattleSkillId skillId, Rect targetRect)
        {
            switch (skillId)
            {
                case BattleSkillId.FlameSign:
                    return new Vector2(targetRect.center.x + 12f, targetRect.center.y - 12f);
                case BattleSkillId.ExecuteSlash:
                    return new Vector2(targetRect.center.x + 8f, targetRect.center.y - 4f);
                case BattleSkillId.ThunderSign:
                    return new Vector2(targetRect.center.x, targetRect.center.y - 8f);
                case BattleSkillId.HunterFocus:
                    return new Vector2(targetRect.center.x, targetRect.center.y + 10f);
                default:
                    return targetRect.center;
            }
        }

        private static Vector2 GetSkillEffectSize(BattleSkillId skillId, Rect targetRect)
        {
            switch (skillId)
            {
                case BattleSkillId.FlameSign:
                    return new Vector2(Mathf.Clamp(targetRect.width + 320f, 360f, 620f), 220f);
                case BattleSkillId.ExecuteSlash:
                    return new Vector2(240f, 190f);
                case BattleSkillId.ThunderSign:
                    return new Vector2(230f, 230f);
                case BattleSkillId.HunterFocus:
                    return new Vector2(250f, 250f);
                default:
                    return new Vector2(220f, 180f);
            }
        }

        private static Vector3 GetSkillEffectScale(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.FlameSign:
                case BattleSkillId.ExecuteSlash:
                    return Vector3.one;
                default:
                    return Vector3.one;
            }
        }

        private static float GetSkillEffectFrameDuration(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.ExecuteSlash:
                    return 0.045f;
                case BattleSkillId.FlameSign:
                    return 0.072f;
                case BattleSkillId.ThunderSign:
                    return 0.082f;
                case BattleSkillId.HunterFocus:
                    return 0.09f;
                default:
                    return 0.074f;
            }
        }

        private static Sprite GetSkillFallbackSprite(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.ThunderSign:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(66, 145, 255, 230));
                case BattleSkillId.HunterFocus:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 220, 88, 210));
                default:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 86, 20, 230));
            }
        }

        private static Color32 GetSkillImpactColor(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.ThunderSign:
                    return new Color32(84, 154, 255, 220);
                case BattleSkillId.HunterFocus:
                    return new Color32(255, 213, 84, 214);
                case BattleSkillId.ExecuteSlash:
                    return new Color32(136, 205, 255, 204);
                default:
                    return new Color32(255, 118, 32, 210);
            }
        }

        private void PrepareFlameImpact(Vector2 impactPoint, Rect targetRect)
        {
            if (flameImpactEffect == null)
            {
                return;
            }

            float impactSize = Mathf.Clamp(targetRect.height * 0.86f, 92f, 158f);
            flameImpactEffect.rectTransform.anchoredPosition = impactPoint;
            flameImpactEffect.rectTransform.sizeDelta = new Vector2(impactSize, impactSize);
            flameImpactEffect.rectTransform.localScale = Vector3.one * 0.65f;
            flameImpactEffect.color = new Color32(skillImpactColor.r, skillImpactColor.g, skillImpactColor.b, 0);
            flameImpactEffect.gameObject.SetActive(false);
        }

        private void UpdateFlameImpact(float normalizedTime, bool visible)
        {
            if (flameImpactEffect == null)
            {
                return;
            }

            if (!visible)
            {
                flameImpactEffect.gameObject.SetActive(false);
                return;
            }

            flameImpactEffect.gameObject.SetActive(true);
            float localTime = Mathf.Clamp01((normalizedTime - 0.35f) / 0.55f);
            float pulse = Mathf.Sin(localTime * Mathf.PI);
            flameImpactEffect.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.32f, pulse);
            flameImpactEffect.color = new Color32(skillImpactColor.r, skillImpactColor.g, skillImpactColor.b, (byte)Mathf.RoundToInt(Mathf.Lerp(skillImpactColor.a, 18f, localTime)));
        }

        private void HideFlameImpact()
        {
            if (flameImpactEffect != null)
            {
                flameImpactEffect.gameObject.SetActive(false);
            }
        }

        private static Sprite[] LoadSkillEffectFrames(BattleSkillId skillId)
        {
            if (cachedSkillEffectFrames.TryGetValue(skillId, out Sprite[] cachedFrames))
            {
                return cachedFrames;
            }

            Sprite[] frames = LoadEffectSpriteSheet(GetSkillEffectSheetName(skillId), 4, 2, skillId.ToString());
            cachedSkillEffectFrames[skillId] = frames;
            return frames;
        }

        private static string GetSkillEffectSheetName(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.ExecuteSlash:
                    return "ExecuteSlashSheet.png";
                case BattleSkillId.FlameSign:
                    return "FlameSignSheet.png";
                case BattleSkillId.ThunderSign:
                    return "ThunderSignSheet.png";
                case BattleSkillId.HunterFocus:
                    return "HunterFocusSheet.png";
                default:
                    return "HunterFlameBeamSheet.png";
            }
        }

        private static Sprite[] LoadEffectSpriteSheet(string fileName, int columns, int rows, string spriteNamePrefix)
        {
            string absolutePath = Path.Combine(Application.dataPath, "Art/Effects", fileName);
            if (!File.Exists(absolutePath))
            {
                return System.Array.Empty<Sprite>();
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                return System.Array.Empty<Sprite>();
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            MakeEffectBackgroundTransparent(texture);
            int frameWidth = texture.width / columns;
            int frameHeight = texture.height / rows;
            Sprite[] frames = new Sprite[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    Rect rect = new Rect(column * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    frames[index] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 256f);
                    frames[index].name = $"TurnBattle{spriteNamePrefix}_{index:00}";
                }
            }

            return frames;
        }

        private static void MakeEffectBackgroundTransparent(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                float saturation = max <= 0.001f ? 0f : (max - min) / max;
                float brightness = max;
                bool whiteBackground = brightness > 0.92f && saturation < 0.18f;
                bool checkerBackground = brightness > 0.62f && saturation < 0.08f;
                if (whiteBackground || checkerBackground)
                {
                    color.a = 0f;
                }
                else
                {
                    color.a *= Mathf.Clamp01(saturation * 4.2f + (1f - brightness) * 1.8f);
                }

                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private IEnumerator PlayPlayerFrames(Sprite[] frames, float frameDuration, Vector2 motion, bool flash)
        {
            if (playerFigure == null)
            {
                yield break;
            }

            playerFigureBusy = true;
            RectTransform rect = playerFigure.rectTransform;
            Sprite[] safeFrames = frames != null && frames.Length > 0 ? frames : LoadGeraltIdleFrames();
            Vector2 home = playerFigureHomePosition;

            for (int i = 0; i < safeFrames.Length; i++)
            {
                if (safeFrames[i] != null)
                {
                    playerFigure.sprite = safeFrames[i];
                }

                float t = safeFrames.Length <= 1 ? 1f : (float)i / (safeFrames.Length - 1);
                float pulse = Mathf.Sin(t * Mathf.PI);
                rect.anchoredPosition = home + motion * pulse;
                SetPlayerFacingScale(1f + 0.045f * pulse);
                playerFigure.color = flash && i % 2 == 0 ? new Color32(255, 228, 214, 255) : Color.white;
                yield return new WaitForSeconds(frameDuration);
            }

            rect.anchoredPosition = home;
            SetPlayerFacingScale(1f);
            playerFigure.color = Color.white;
            playerFigure.sprite = GetPlayerIdleFrame();
            playerIdleIndex = 0;
            playerIdleTimer = 0f;
            playerFigureBusy = false;
        }

        private void UpdatePlayerIdleFigure()
        {
            if (playerFigure == null || playerFigureBusy)
            {
                return;
            }

            Sprite[] frames = GetPlayerFrames(GeraltAnimation.Idle);
            if (frames.Length <= 1)
            {
                return;
            }

            playerIdleTimer += Time.deltaTime;
            if (playerIdleTimer < 0.16f)
            {
                return;
            }

            playerIdleTimer = 0f;
            playerIdleIndex = (playerIdleIndex + 1) % frames.Length;
            playerFigure.sprite = frames[playerIdleIndex];
        }

        private void BuildPartySupportSlots()
        {
            partyVisualSlots.Clear();
            Vector2[] positions =
            {
                new Vector2(184f, -66f),
                new Vector2(238f, -78f),
                new Vector2(392f, -82f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Image supportImage = CreateCenteredImage($"Battle Party Support {i + 1}", root.transform, new Vector2(88f, 116f), positions[i], Color.white);
                supportImage.preserveAspect = true;
                supportImage.raycastTarget = false;
                supportImage.color = new Color32(255, 255, 255, 218);
                supportImage.gameObject.SetActive(false);
                partyVisualSlots.Add(new PartyVisualSlot { Image = supportImage });
            }
        }

        private void RefreshPartyVisuals()
        {
            if (partyVisualSlots.Count == 0)
            {
                return;
            }

            IReadOnlyList<PartyMember> members = PartyManager.CreateIfMissing().ActiveParty;
            int visualIndex = 0;
            for (int i = 0; i < members.Count && visualIndex < partyVisualSlots.Count; i++)
            {
                PartyMember member = members[i];
                if (member.Name == "猎魔人")
                {
                    continue;
                }

                PartyVisualSlot slot = partyVisualSlots[visualIndex];
                slot.Member = member;
                slot.IdleIndex = 0;
                slot.IdleTimer = 0f;
                slot.Image.sprite = PartyAnimationLibrary.GetIdlePreview(member);
                slot.Image.gameObject.SetActive(slot.Image.sprite != null);
                visualIndex++;
            }

            for (int i = visualIndex; i < partyVisualSlots.Count; i++)
            {
                partyVisualSlots[i].Member = null;
                partyVisualSlots[i].Image.gameObject.SetActive(false);
            }
        }

        private void UpdatePartyIdleFigures()
        {
            for (int i = 0; i < partyVisualSlots.Count; i++)
            {
                PartyVisualSlot slot = partyVisualSlots[i];
                if (slot.Member == null || !slot.Image.gameObject.activeSelf)
                {
                    continue;
                }

                Sprite[] frames = PartyAnimationLibrary.GetFrames(slot.Member, PartyAnimationKind.Idle);
                if (frames.Length <= 1)
                {
                    continue;
                }

                slot.IdleTimer += Time.deltaTime;
                if (slot.IdleTimer < 0.18f)
                {
                    continue;
                }

                slot.IdleTimer = 0f;
                slot.IdleIndex = (slot.IdleIndex + 1) % frames.Length;
                slot.Image.sprite = frames[slot.IdleIndex];
            }
        }

        private Sprite GetPlayerIdleFrame()
        {
            Sprite[] frames = GetPlayerFrames(GeraltAnimation.Idle);
            return frames.Length > 0 ? frames[0] : WitcherSpriteLibrary.GetGeraltFrame(GeraltAnimation.Idle, 0);
        }

        private Sprite[] GetPlayerFrames(GeraltAnimation animation)
        {
            if (playerAnimator != null)
            {
                Sprite[] frames = playerAnimator.GetFramesForBattleHud(animation);
                if (frames != null && frames.Length > 0)
                {
                    return frames;
                }
            }

            switch (animation)
            {
                case GeraltAnimation.Slash:
                    return LoadGeraltSlashFrames();
                case GeraltAnimation.Hurt:
                    return LoadGeraltHurtFrames();
                default:
                    return LoadGeraltFrames(animation);
            }
        }

        private static Sprite[] LoadGeraltIdleFrames()
        {
            if (cachedGeraltIdleFrames != null)
            {
                return cachedGeraltIdleFrames;
            }

            cachedGeraltIdleFrames = LoadGeraltFrames(GeraltAnimation.Idle);
            return cachedGeraltIdleFrames;
        }

        private static Sprite[] LoadGeraltSlashFrames()
        {
            if (cachedGeraltSlashFrames != null)
            {
                return cachedGeraltSlashFrames;
            }

            cachedGeraltSlashFrames = LoadGeraltFrames(GeraltAnimation.Slash);
            return cachedGeraltSlashFrames;
        }

        private static Sprite[] LoadGeraltHurtFrames()
        {
            if (cachedGeraltHurtFrames != null)
            {
                return cachedGeraltHurtFrames;
            }

            cachedGeraltHurtFrames = LoadGeraltFrames(GeraltAnimation.Hurt);
            return cachedGeraltHurtFrames;
        }

        private static Sprite[] LoadGeraltFrames(GeraltAnimation animation)
        {
            if (cachedGeraltFrames.TryGetValue(animation, out Sprite[] cachedFrames))
            {
                return cachedFrames;
            }

            string folderPath = Path.Combine(Application.dataPath, "Art/Geralt/Frames", animation.ToString());
            if (!Directory.Exists(folderPath))
            {
                Sprite[] fallbackFrames = CreateGeraltFallbackFrames(animation);
                cachedGeraltFrames[animation] = fallbackFrames;
                return fallbackFrames;
            }

            string[] filePaths = Directory.GetFiles(folderPath, "*.png");
            System.Array.Sort(filePaths, System.StringComparer.OrdinalIgnoreCase);

            List<Sprite> frames = new List<Sprite>();
            for (int i = 0; i < filePaths.Length; i++)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(filePaths[i])))
                {
                    continue;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite frame = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f),
                    82f);
                frame.name = Path.GetFileNameWithoutExtension(filePaths[i]);
                frames.Add(frame);
            }

            Sprite[] loadedFrames = frames.Count > 0 ? frames.ToArray() : CreateGeraltFallbackFrames(animation);
            cachedGeraltFrames[animation] = loadedFrames;
            return loadedFrames;
        }

        private static GeraltAnimation GetGeraltAnimationForSkill(BattleSkillId skillId, BattleSkillAnimationKind animationKind)
        {
            switch (animationKind)
            {
                case BattleSkillAnimationKind.Flame:
                    return GeraltAnimation.FlameSign;
                case BattleSkillAnimationKind.Defend:
                    return GeraltAnimation.ShieldSign;
                case BattleSkillAnimationKind.Cast:
                    return GeraltAnimation.PurpleSign;
                case BattleSkillAnimationKind.Slash:
                    return GeraltAnimation.Slash;
                default:
                    return skillId == BattleSkillId.Potion ? GeraltAnimation.PurpleSign : GeraltAnimation.Idle;
            }
        }

        private static Vector2 GetPlayerSkillMotion(GeraltAnimation animation)
        {
            switch (animation)
            {
                case GeraltAnimation.FlameSign:
                    return new Vector2(34f, -5f);
                case GeraltAnimation.PurpleSign:
                    return new Vector2(24f, -4f);
                case GeraltAnimation.ShieldSign:
                    return Vector2.zero;
                default:
                    return new Vector2(54f, -6f);
            }
        }

        private static float GetPlayerSkillFrameDuration(GeraltAnimation animation)
        {
            switch (animation)
            {
                case GeraltAnimation.FlameSign:
                    return 0.075f;
                case GeraltAnimation.PurpleSign:
                    return 0.08f;
                case GeraltAnimation.ShieldSign:
                    return 0.085f;
                default:
                    return 0.085f;
            }
        }

        private static Sprite[] CreateGeraltFallbackFrames(GeraltAnimation animation)
        {
            int frameCount = WitcherSpriteLibrary.GetGeraltFrameCount(animation);
            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = WitcherSpriteLibrary.GetGeraltFrame(animation, i);
            }

            return frames;
        }

        private void AddCommandButton(Transform parent, string label, TurnBattleAction action, Vector2 position)
        {
            Vector2 buttonSize = new Vector2(190f, 25f);
            GameObject buttonObject = CreateUiObject(label + " Button", parent, buttonSize, position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(12, 14, 18, 210));
            image.color = new Color32(12, 14, 18, 210);
            image.type = Image.Type.Simple;
            AddOutline(image, new Color32(90, 97, 118, 210), new Vector2(1f, -1f));

            Image accent = CreateImage(label + " Accent", buttonObject.transform, new Vector2(4f, 20f), new Vector2(0f, -4f), action == TurnBattleAction.FlameSign ? new Color32(255, 134, 62, 220) : new Color32(64, 154, 255, 210));
            accent.sprite = WitcherSpriteLibrary.GetSolidSprite(accent.color);
            accent.raycastTarget = false;

            Image keyBack = CreateImage(label + " Key", buttonObject.transform, new Vector2(30f, 24f), new Vector2(3f, -2f), new Color32(22, 31, 44, 230));
            keyBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(22, 31, 44, 230));
            AddOutline(keyBack, new Color32(96, 150, 216, 220), new Vector2(1f, -1f));

            Text keyText = CreateText(label + " Key Text", keyBack.transform, label.Substring(0, 1), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30f, 24f));
            keyText.color = new Color32(238, 247, 255, 255);
            AddOutline(keyText, Color.black, new Vector2(1f, -1f));

            Text nameText = CreateText(label + " Name", buttonObject.transform, GetCommandDisplayName(action), 15, TextAnchor.MiddleLeft, new Vector2(42f, -2f), new Vector2(86f, 22f));
            nameText.color = action == TurnBattleAction.FlameSign ? new Color32(255, 179, 105, 255) : new Color32(226, 236, 244, 255);
            AddOutline(nameText, Color.black, new Vector2(1f, -1f));

            Text costText = CreateText(label + " Cost", buttonObject.transform, GetCommandCostLabel(action), 13, TextAnchor.MiddleRight, new Vector2(126f, -2f), new Vector2(56f, 22f));
            costText.color = new Color32(190, 204, 220, 255);
            AddOutline(costText, Color.black, new Vector2(1f, -1f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => manager.SelectAction(action));
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(255, 255, 255, 255);
            colors.highlightedColor = new Color32(108, 132, 255, 255);
            colors.pressedColor = new Color32(255, 178, 86, 255);
            colors.disabledColor = new Color32(78, 78, 78, 118);
            button.colors = colors;
            commandButtons.Add(button);
            commandButtonImages.Add(image);
        }

        private void BuildSkillPanel(Transform parent)
        {
            skillButtons.Clear();
            Image panelImage = CreateImage("Battle Skill Panel", parent, new Vector2(398f, 112f), new Vector2(18f, -4f), new Color32(4, 7, 12, 224));
            panelImage.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(4, 7, 12, 224));
            AddOutline(panelImage, new Color32(86, 136, 182, 190), new Vector2(1f, -1f));
            skillPanel = panelImage.gameObject;

            Text title = CreateText("Battle Skill Panel Title", skillPanel.transform, "猎魔技能", 17, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(112f, 24f));
            title.color = new Color32(255, 218, 138, 255);
            AddOutline(title, Color.black, new Vector2(1f, -1f));

            Text hint = CreateText("Battle Skill Panel Hint", skillPanel.transform, "Esc 返回", 13, TextAnchor.MiddleRight, new Vector2(300f, -10f), new Vector2(78f, 20f));
            hint.color = new Color32(180, 202, 224, 255);
            AddOutline(hint, Color.black, new Vector2(1f, -1f));

            AddSkillButton(skillPanel.transform, "1 连续斩杀", "三连银剑", "MP 12", BattleSkillId.ExecuteSlash, new Vector2(12f, -34f));
            AddSkillButton(skillPanel.transform, "2 火焰法印", "群体火焰", "MP 18", BattleSkillId.FlameSign, new Vector2(204f, -34f));
            AddSkillButton(skillPanel.transform, "3 雷霆法印", "双段闪电", "MP 20", BattleSkillId.ThunderSign, new Vector2(12f, -72f));
            AddSkillButton(skillPanel.transform, "4 猎魔专注", "攻击提升", "MP 10", BattleSkillId.HunterFocus, new Vector2(204f, -72f));
            skillPanel.SetActive(false);
        }

        private void AddSkillButton(Transform parent, string title, string description, string cost, BattleSkillId skillId, Vector2 position)
        {
            GameObject buttonObject = CreateUiObject(title + " Skill Button", parent, new Vector2(178f, 30f), position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(12, 18, 28, 220));
            image.color = new Color32(12, 18, 28, 220);
            AddOutline(image, new Color32(59, 88, 125, 220), new Vector2(1f, -1f));

            Text titleText = CreateText(title + " Title", buttonObject.transform, title, 13, TextAnchor.MiddleLeft, new Vector2(8f, -3f), new Vector2(72f, 20f));
            titleText.color = skillId == BattleSkillId.FlameSign ? new Color32(255, 178, 92, 255) : new Color32(230, 238, 246, 255);
            AddOutline(titleText, Color.black, new Vector2(1f, -1f));

            Text descriptionText = CreateText(title + " Desc", buttonObject.transform, description, 11, TextAnchor.MiddleLeft, new Vector2(82f, -3f), new Vector2(52f, 20f));
            descriptionText.color = new Color32(183, 205, 222, 255);
            AddOutline(descriptionText, Color.black, new Vector2(1f, -1f));

            Text costText = CreateText(title + " Cost", buttonObject.transform, cost, 11, TextAnchor.MiddleRight, new Vector2(132f, -3f), new Vector2(38f, 20f));
            costText.color = new Color32(141, 197, 255, 255);
            AddOutline(costText, Color.black, new Vector2(1f, -1f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => manager.SelectSkill(skillId));
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(116, 145, 255, 255);
            colors.pressedColor = new Color32(255, 178, 86, 255);
            colors.disabledColor = new Color32(78, 78, 78, 118);
            button.colors = colors;
            skillButtons.Add(button);
        }

        private static string GetCommandDisplayName(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.Attack:
                    return "攻击";
                case TurnBattleAction.FlameSign:
                    return "技能";
                case TurnBattleAction.Defend:
                    return "防御";
                case TurnBattleAction.Item:
                    return "道具";
                case TurnBattleAction.Escape:
                    return "撤离战场";
                default:
                    return action.ToString();
            }
        }

        private static string GetCommandCostLabel(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.FlameSign:
                    return "展开";
                case TurnBattleAction.Item:
                    return "药剂";
                case TurnBattleAction.Defend:
                    return "护身";
                case TurnBattleAction.Attack:
                    return "普通";
                default:
                    return string.Empty;
            }
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return obj;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateCenteredImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0.5f, 0.5f));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Text label = obj.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.raycastTarget = false;
            return label;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
