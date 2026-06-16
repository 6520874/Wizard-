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
        private static readonly Color32 JrpgPanelColor = new Color32(8, 24, 15, 202);
        private static readonly Color32 JrpgPanelStrongColor = new Color32(5, 15, 10, 230);
        private static readonly Color32 JrpgBorderColor = new Color32(235, 244, 232, 238);
        private static readonly Color32 JrpgRuleColor = new Color32(230, 238, 222, 146);
        private static readonly Color32 JrpgTextColor = new Color32(246, 248, 239, 255);
        private static readonly Color32 JrpgMutedTextColor = new Color32(205, 216, 201, 255);
        private static readonly Color32 JrpgSelectedColor = new Color32(52, 75, 40, 246);
        private static readonly Color32 JrpgGoldColor = new Color32(255, 226, 136, 255);
        private static readonly Vector2 PartyFigureSize = new Vector2(116f, 112f);
        private const float PartyFigureMaxWidth = 136f;
        private const float PartyFigureGroundY = -92f;

        private readonly List<Text> enemyRows = new List<Text>();
        private readonly List<EnemyVisualSlot> enemySlots = new List<EnemyVisualSlot>();
        private readonly List<Button> commandButtons = new List<Button>();
        private readonly List<Image> commandButtonImages = new List<Image>();
        private readonly List<Button> skillButtons = new List<Button>();
        private readonly List<Text> skillTitleTexts = new List<Text>();
        private readonly List<Text> skillDescriptionTexts = new List<Text>();
        private readonly List<Text> skillCostTexts = new List<Text>();
        private readonly List<PartyVisualSlot> partyVisualSlots = new List<PartyVisualSlot>();
        private static Sprite cachedBattleBackdrop;
        private static Sprite cachedFloorMist;
        private static Sprite cachedGroundShadow;
        private static Sprite cachedGroundGlow;
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
        private GameObject victoryRewardPanel;
        private Text victoryRewardText;
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

        // 中文说明：保存我方队伍成员在战斗场景中的图片和待机动画状态。
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
                victoryRewardText.text = GameText.Battle.VictoryReward(Mathf.Max(0, experience), Mathf.Max(0, gold), BuildLootLine(loot));
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
                    ? JrpgSelectedColor
                    : JrpgPanelStrongColor;
                commandButtonImages[i].rectTransform.localScale = selected ? new Vector3(1.045f, 1.045f, 1f) : Vector3.one;
            }
        }

        public void ShowSkillMenu(string actorName, IReadOnlyList<SkillDefinition> skills)
        {
            if (skillPanel == null)
            {
                return;
            }

            skillPanel.SetActive(true);
            for (int i = 0; i < skillButtons.Count; i++)
            {
                bool hasSkill = skills != null && i < skills.Count && skills[i] != null;
                skillButtons[i].gameObject.SetActive(hasSkill);
                skillButtons[i].interactable = hasSkill;
                if (!hasSkill)
                {
                    continue;
                }

                SkillDefinition skill = skills[i];
                if (i < skillTitleTexts.Count)
                {
                    skillTitleTexts[i].text = $"{i + 1} {skill.DisplayName}";
                    skillTitleTexts[i].color = GetSkillNameColor(skill.Id);
                }

                if (i < skillDescriptionTexts.Count)
                {
                    skillDescriptionTexts[i].text = GetSkillShortDescription(skill);
                }

                if (i < skillCostTexts.Count)
                {
                    skillCostTexts[i].text = GameText.Battle.SkillCost(skill.ManaCost);
                }
            }

            SetMessage(GameText.Battle.SkillSelect(actorName));
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
                skillButtons[i].interactable = enabled && skillButtons[i].gameObject.activeSelf;
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
            RefreshPartyVisuals();
            EnsureHdBattleHud();
            hdBattleHud?.RefreshFromBattle(manager, enemies, player, GetEnemyHudPositions());
            if (playerText != null && player != null)
            {
                playerText.text = $"{GameText.Stats.CompactHp(player.CurrentHealth, player.MaxHealth)}    {GameText.Stats.CompactMp(player.CurrentMana, player.MaxMana)}";
                SetFillWidth(playerHealthFill, player.MaxHealth <= 0 ? 0f : (float)player.CurrentHealth / player.MaxHealth, 190f);
                SetFillWidth(playerManaFill, player.MaxMana <= 0 ? 0f : (float)player.CurrentMana / player.MaxMana, 190f);
            }

            if (potionText != null)
            {
                potionText.text = GameText.Battle.PotionCount(potionCount);
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
                string state = enemy.IsAlive ? GameText.Battle.EnemyHp(enemy.Health, enemy.MaxHealth) : GameText.Battle.Defeated;
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
                    slot.Rect.sizeDelta = (enemy.VisualKind == TurnBasedEnemyVisualKind.BlackMoonKnight || enemy.VisualKind == TurnBasedEnemyVisualKind.BlackNailPuppet) && i == 0
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

        public IEnumerator PlayEnemySkillEffect(BattleSkillId skillId, int enemyIndex)
        {
            if (root == null)
            {
                yield break;
            }

            Color32 color = GetEnemySkillColor(skillId);
            Image flash = CreateImage("Enemy Skill Flash", root.transform, new Vector2(1220f, 220f), new Vector2(-110f, -116f), color);
            flash.transform.SetAsLastSibling();
            flash.raycastTarget = false;
            flash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GetEnemySkillAngle(skillId));

            if (TryGetSlot(enemyIndex, out EnemyVisualSlot slot))
            {
                slot.GroundGlow.color = color;
                slot.GroundGlow.gameObject.SetActive(true);
            }

            for (int i = 0; i < 10; i++)
            {
                float t = i / 9f;
                Color c = color;
                c.a = Mathf.Lerp(0.42f, 0f, t);
                flash.color = c;
                flash.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-260f, 80f, t), -116f + Mathf.Sin(t * Mathf.PI) * 18f);
                flash.rectTransform.localScale = new Vector3(1f + t * 0.16f, 1f - t * 0.22f, 1f);
                yield return new WaitForSeconds(0.026f);
            }

            Destroy(flash.gameObject);
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

        public IEnumerator PlayEnemyDefeat(int enemyIndex, float startDelay = 0f, int damage = 0)
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

            Sprite[] frames = enemy.DefeatFrames != null && enemy.DefeatFrames.Length > 0
                ? enemy.DefeatFrames
                : enemy.HurtFrames;
            yield return PlayEnemyFrames(slot, frames, enemy.Sprite, 0.095f, false, true);
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

        public IEnumerator PlayPartyMemberSkill(PartyMember member, SkillDefinition skill, int targetEnemyIndex)
        {
            if (!TryGetPartySlot(member, out PartyVisualSlot slot) || slot.Image == null)
            {
                yield return PlaySkillEffect(skill.Id, targetEnemyIndex);
                yield break;
            }

            PartyAnimationKind animation = skill.AnimationKind == BattleSkillAnimationKind.Slash
                ? PartyAnimationKind.Attack
                : PartyAnimationKind.Cast;
            if (skill.Id == BattleSkillId.TrissMeteorFlare
                || skill.Id == BattleSkillId.TrissFlameWard
                || skill.Id == BattleSkillId.YenneferObsidianStorm
                || skill.Id == BattleSkillId.YenneferAegis)
            {
                animation = PartyAnimationKind.Special;
            }

            Sprite[] frames = PartyAnimationLibrary.GetFrames(member, animation);
            if (frames.Length == 0)
            {
                frames = PartyAnimationLibrary.GetFrames(member, PartyAnimationKind.Idle);
            }

            RectTransform rect = slot.Image.rectTransform;
            Vector2 home = rect.anchoredPosition;
            Vector2 motion = GetPartySkillMotion(slot, targetEnemyIndex);
            for (int i = 0; i < Mathf.Max(1, frames.Length); i++)
            {
                if (frames.Length > 0 && frames[i] != null)
                {
                    slot.Image.sprite = frames[i];
                }

                float t = frames.Length <= 1 ? 1f : (float)i / (frames.Length - 1);
                float pulse = Mathf.Sin(t * Mathf.PI);
                rect.anchoredPosition = home + motion * pulse;
                rect.localScale = new Vector3(-1f - pulse * 0.05f, 1f + pulse * 0.05f, 1f);
                slot.Image.color = skill.AnimationKind == BattleSkillAnimationKind.Defend && i % 2 == 0
                    ? new Color32(226, 204, 255, 255)
                    : Color.white;
                yield return new WaitForSeconds(0.07f);
            }

            rect.anchoredPosition = home;
            rect.localScale = new Vector3(-1f, 1f, 1f);
            slot.Image.color = new Color32(255, 255, 255, 218);
            slot.Image.sprite = PartyAnimationLibrary.GetIdlePreview(member);
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

            Image playerGlow = CreateCenteredImage("Battle Player Ground Glow", root.transform, new Vector2(146f, 34f), new Vector2(304f, -108f), new Color32(42, 143, 255, 78));
            playerGlow.sprite = GetGroundGlowSprite();
            playerGlow.raycastTarget = false;

            Image playerShadow = CreateCenteredImage("Battle Player Ground Shadow", root.transform, new Vector2(124f, 28f), new Vector2(304f, -116f), new Color32(0, 0, 0, 178));
            playerShadow.sprite = GetGroundShadowSprite();
            playerShadow.raycastTarget = false;

            BuildPartySupportSlots();

            playerFigure = CreateCenteredImage("Battle Player Figure", root.transform, PartyFigureSize, GetFigureAlignedPosition(304f, GetPlayerIdleFrame(), PartyFigureSize), Color.white);
            playerFigure.sprite = GetPlayerIdleFrame();
            AlignFigureToGround(playerFigure, 304f);
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

            Image playerPanel = CreateImage("Player Battle Plate", root.transform, new Vector2(246f, 118f), new Vector2(22f, -78f), JrpgPanelColor);
            playerPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelColor);
            AddOutline(playerPanel, JrpgBorderColor, new Vector2(2f, -2f));

            Text playerName = CreateText("Player Battle Name", playerPanel.transform, GameText.HunterName, 24, TextAnchor.MiddleLeft, new Vector2(18f, -12f), new Vector2(176f, 28f));
            playerName.color = JrpgTextColor;
            AddOutline(playerName, Color.black, new Vector2(2f, -2f));

            Text bpText = CreateText("Player Battle BP", playerPanel.transform, GameText.Battle.RoundLabel, 15, TextAnchor.MiddleRight, new Vector2(132f, -16f), new Vector2(88f, 24f));
            bpText.color = JrpgMutedTextColor;
            AddOutline(bpText, Color.black, new Vector2(1f, -1f));

            Image playerHealthBack = CreateImage("Player Battle HP Back", playerPanel.transform, new Vector2(198f, 13f), new Vector2(20f, -50f), new Color32(42, 5, 8, 245));
            playerHealthBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(42, 5, 8, 245));
            playerHealthFill = CreateImage("Player Battle HP Fill", playerHealthBack.transform, new Vector2(190f, 7f), new Vector2(4f, -3f), new Color32(232, 34, 45, 255));
            playerHealthFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            Image playerManaBack = CreateImage("Player Battle MP Back", playerPanel.transform, new Vector2(198f, 13f), new Vector2(20f, -74f), new Color32(4, 18, 52, 245));
            playerManaBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(4, 18, 52, 245));
            playerManaFill = CreateImage("Player Battle MP Fill", playerManaBack.transform, new Vector2(190f, 7f), new Vector2(4f, -3f), new Color32(46, 145, 255, 255));
            playerManaFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            playerText = CreateText("Battle Player Stats", playerPanel.transform, $"{GameText.Stats.CompactHp(100, 100)}    {GameText.Stats.CompactMp(100, 100)}", 15, TextAnchor.MiddleLeft, new Vector2(18f, -94f), new Vector2(220f, 20f));
            playerText.color = JrpgTextColor;
            AddOutline(playerText, Color.black, new Vector2(2f, -2f));
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
                Image nameplateBack = CreateCenteredImage($"Battle Enemy Nameplate {i + 1}", root.transform, new Vector2(164f, 64f), slotPosition + new Vector2(0f, 126f), JrpgPanelColor);
                nameplateBack.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelColor);
                nameplateBack.raycastTarget = false;
                nameplateBack.gameObject.SetActive(false);
                AddOutline(nameplateBack, JrpgBorderColor, new Vector2(2f, -2f));

                Text row = CreateText($"Enemy Stage Label {i + 1}", nameplateBack.transform, string.Empty, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(154f, 58f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                row.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                row.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                row.color = JrpgTextColor;
                AddOutline(row, Color.black, new Vector2(2f, -2f));
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

            Image commandPanel = CreateImage("Command Panel", root.transform, new Vector2(432f, 122f), new Vector2(224f, -394f), JrpgPanelColor);
            commandPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelColor);
            AddOutline(commandPanel, JrpgBorderColor, new Vector2(2f, -2f));

            Image commandInnerGlow = CreateImage("Command Panel Inner Glow", commandPanel.transform, new Vector2(418f, 108f), new Vector2(7f, -7f), new Color32(21, 47, 28, 50));
            commandInnerGlow.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(21, 47, 28, 50));
            commandInnerGlow.raycastTarget = false;

            Image commandTopRule = CreateImage("Command Panel Top Rule", commandPanel.transform, new Vector2(408f, 1f), new Vector2(12f, -44f), JrpgRuleColor);
            commandTopRule.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgRuleColor);
            commandTopRule.raycastTarget = false;

            Image commandGoldRule = CreateImage("Command Panel Gold Rule", commandPanel.transform, new Vector2(82f, 1f), new Vector2(22f, -42f), new Color32(255, 226, 136, 170));
            commandGoldRule.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 226, 136, 170));
            commandGoldRule.raycastTarget = false;

            Text commandTitle = CreateText("Battle Command Title", commandPanel.transform, GameText.Battle.Action, 20, TextAnchor.MiddleLeft, new Vector2(22f, -12f), new Vector2(86f, 28f));
            commandTitle.color = JrpgTextColor;
            AddOutline(commandTitle, Color.black, new Vector2(2f, -2f));

            potionText = CreateText("Battle Potion Count", commandPanel.transform, GameText.Battle.PotionCount(3), 14, TextAnchor.MiddleRight, new Vector2(322f, -14f), new Vector2(86f, 24f));
            potionText.color = JrpgMutedTextColor;
            AddOutline(potionText, Color.black, new Vector2(1f, -1f));

            messageText = CreateText("Battle Message", commandPanel.transform, GameText.Battle.SelectAction, 14, TextAnchor.UpperLeft, new Vector2(104f, -18f), new Vector2(210f, 32f));
            messageText.color = JrpgTextColor;
            AddOutline(messageText, Color.black, new Vector2(2f, -2f));

            commandButtons.Clear();
            commandButtonImages.Clear();
            AddCommandButton(commandPanel.transform, "1 " + GameText.Battle.Attack, TurnBattleAction.Attack, new Vector2(18f, -58f));
            AddCommandButton(commandPanel.transform, "2 " + GameText.Battle.Skill, TurnBattleAction.FlameSign, new Vector2(222f, -58f));
            AddCommandButton(commandPanel.transform, "3 " + GameText.Battle.Item, TurnBattleAction.Item, new Vector2(18f, -90f));
            AddCommandButton(commandPanel.transform, "4 " + GameText.Battle.Defend, TurnBattleAction.Defend, new Vector2(222f, -90f));
            BuildSkillPanel(commandPanel.transform);
            BuildVictoryRewardPanel(root.transform);
            EnsureHdBattleHud();

            root.SetActive(false);
        }

        private void BuildVictoryRewardPanel(Transform parent)
        {
            Image panel = CreateCenteredImage("Battle Victory Reward Panel", parent, new Vector2(356f, 128f), new Vector2(0f, 36f), JrpgPanelStrongColor);
            panel.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelStrongColor);
            panel.raycastTarget = false;
            AddOutline(panel, JrpgBorderColor, new Vector2(3f, -3f));
            victoryRewardPanel = panel.gameObject;

            Image inner = CreateImage("Battle Victory Reward Inner", panel.transform, new Vector2(336f, 106f), new Vector2(10f, -10f), JrpgPanelColor);
            inner.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelColor);
            inner.raycastTarget = false;

            Image topRule = CreateImage("Battle Victory Reward Gold Rule", panel.transform, new Vector2(292f, 2f), new Vector2(32f, -42f), new Color32(255, 204, 101, 180));
            topRule.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 204, 101, 210));
            topRule.raycastTarget = false;

            victoryRewardText = CreateText("Battle Victory Reward Text", panel.transform, GameText.Battle.BattleVictory, 18, TextAnchor.MiddleCenter, new Vector2(12f, -12f), new Vector2(332f, 100f));
            victoryRewardText.color = JrpgTextColor;
            AddOutline(victoryRewardText, Color.black, new Vector2(2f, -2f));
            victoryRewardPanel.SetActive(false);
        }

        private static string BuildLootLine(IReadOnlyList<string> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return GameText.Battle.LootNone;
            }

            string line = GameText.Battle.LootPrefix;
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

            return line.Length > GameText.Battle.LootPrefix.Length ? line : GameText.Battle.LootNone;
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
            if (skillId == BattleSkillId.HunterFocus || skillId == BattleSkillId.TrissFlameWard || skillId == BattleSkillId.YenneferAegis)
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
                case BattleSkillId.TrissFirebolt:
                case BattleSkillId.YenneferArcaneBolt:
                    return new Vector2(targetRect.center.x, targetRect.center.y - 8f);
                case BattleSkillId.HunterFocus:
                case BattleSkillId.TrissFlameWard:
                case BattleSkillId.YenneferAegis:
                    return new Vector2(targetRect.center.x, targetRect.center.y + 10f);
                case BattleSkillId.TrissMeltingSigil:
                case BattleSkillId.TrissMeteorFlare:
                case BattleSkillId.YenneferCursePulse:
                case BattleSkillId.YenneferObsidianStorm:
                    return new Vector2(targetRect.center.x + 8f, targetRect.center.y - 10f);
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
                case BattleSkillId.TrissFirebolt:
                case BattleSkillId.YenneferArcaneBolt:
                    return new Vector2(230f, 230f);
                case BattleSkillId.HunterFocus:
                case BattleSkillId.TrissFlameWard:
                case BattleSkillId.YenneferAegis:
                    return new Vector2(250f, 250f);
                case BattleSkillId.TrissMeltingSigil:
                case BattleSkillId.YenneferCursePulse:
                    return new Vector2(Mathf.Clamp(targetRect.width + 220f, 320f, 560f), 210f);
                case BattleSkillId.TrissMeteorFlare:
                case BattleSkillId.YenneferObsidianStorm:
                    return new Vector2(Mathf.Clamp(targetRect.width + 360f, 420f, 680f), 260f);
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
                case BattleSkillId.TrissFirebolt:
                case BattleSkillId.YenneferArcaneBolt:
                    return 0.082f;
                case BattleSkillId.HunterFocus:
                case BattleSkillId.TrissFlameWard:
                case BattleSkillId.YenneferAegis:
                    return 0.09f;
                case BattleSkillId.TrissMeteorFlare:
                case BattleSkillId.YenneferObsidianStorm:
                    return 0.064f;
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
                case BattleSkillId.TrissFirebolt:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 102, 34, 230));
                case BattleSkillId.YenneferArcaneBolt:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(155, 94, 255, 230));
                case BattleSkillId.HunterFocus:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 220, 88, 210));
                case BattleSkillId.TrissFlameWard:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 145, 48, 210));
                case BattleSkillId.YenneferAegis:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(156, 114, 255, 210));
                case BattleSkillId.TrissMeltingSigil:
                case BattleSkillId.TrissMeteorFlare:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 76, 28, 220));
                case BattleSkillId.YenneferCursePulse:
                case BattleSkillId.YenneferObsidianStorm:
                    return WitcherSpriteLibrary.GetSolidSprite(new Color32(118, 65, 218, 224));
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
                case BattleSkillId.TrissFirebolt:
                    return new Color32(255, 125, 44, 220);
                case BattleSkillId.YenneferArcaneBolt:
                    return new Color32(170, 112, 255, 224);
                case BattleSkillId.TrissMeltingSigil:
                    return new Color32(255, 82, 36, 218);
                case BattleSkillId.TrissFlameWard:
                    return new Color32(255, 174, 76, 214);
                case BattleSkillId.TrissMeteorFlare:
                    return new Color32(255, 58, 24, 225);
                case BattleSkillId.YenneferCursePulse:
                    return new Color32(124, 68, 226, 220);
                case BattleSkillId.YenneferAegis:
                    return new Color32(183, 139, 255, 214);
                case BattleSkillId.YenneferObsidianStorm:
                    return new Color32(96, 50, 190, 228);
                case BattleSkillId.ExecuteSlash:
                    return new Color32(136, 205, 255, 204);
                default:
                    return new Color32(255, 118, 32, 210);
            }
        }

        private static Color32 GetEnemySkillColor(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.PlagueHowl:
                    return new Color32(120, 210, 76, 160);
                case BattleSkillId.BloodDrain:
                    return new Color32(210, 28, 62, 168);
                case BattleSkillId.Moonbreaker:
                    return new Color32(116, 154, 255, 176);
                default:
                    return new Color32(130, 54, 32, 148);
            }
        }

        private static float GetEnemySkillAngle(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.PlagueHowl:
                    return 0f;
                case BattleSkillId.BloodDrain:
                    return -8f;
                case BattleSkillId.Moonbreaker:
                    return -14f;
                default:
                    return 6f;
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
                case BattleSkillId.TrissFirebolt:
                case BattleSkillId.TrissMeteorFlare:
                    return "FlameSignSheet.png";
                case BattleSkillId.ThunderSign:
                case BattleSkillId.YenneferArcaneBolt:
                case BattleSkillId.YenneferObsidianStorm:
                    return "ThunderSignSheet.png";
                case BattleSkillId.HunterFocus:
                case BattleSkillId.TrissFlameWard:
                case BattleSkillId.YenneferAegis:
                    return "HunterFocusSheet.png";
                case BattleSkillId.TrissMeltingSigil:
                case BattleSkillId.YenneferCursePulse:
                    return "HunterFlameBeamSheet.png";
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
                new Vector2(184f, PartyFigureGroundY),
                new Vector2(244f, PartyFigureGroundY),
                new Vector2(364f, PartyFigureGroundY)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Image supportImage = CreateCenteredImage($"Battle Party Support {i + 1}", root.transform, PartyFigureSize, positions[i], Color.white);
                supportImage.preserveAspect = true;
                supportImage.raycastTarget = false;
                supportImage.color = new Color32(255, 255, 255, 218);
                supportImage.gameObject.SetActive(false);
                partyVisualSlots.Add(new PartyVisualSlot { Image = supportImage });
            }
        }

        private static float GetPartyFigureX(int visualIndex)
        {
            switch (visualIndex)
            {
                case 0:
                    return 184f;
                case 1:
                    return 244f;
                default:
                    return 364f;
            }
        }

        private static Vector2 GetFigureLayoutSize(Sprite sprite)
        {
            if (sprite == null || sprite.rect.height <= 0.01f)
            {
                return PartyFigureSize;
            }

            float targetHeight = PartyFigureSize.y;
            float width = targetHeight * sprite.rect.width / sprite.rect.height;
            if (width > PartyFigureMaxWidth)
            {
                width = PartyFigureMaxWidth;
                targetHeight = width * sprite.rect.height / sprite.rect.width;
            }

            return new Vector2(width, targetHeight);
        }

        private static Vector2 GetFigureAlignedPosition(float x, Sprite sprite, Vector2 fallbackSize)
        {
            Vector2 size = sprite == null ? fallbackSize : GetFigureLayoutSize(sprite);
            return new Vector2(x, PartyFigureGroundY + size.y * 0.5f);
        }

        private static void AlignFigureToGround(Image image, float x)
        {
            if (image == null)
            {
                return;
            }

            Vector2 size = GetFigureLayoutSize(image.sprite);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = new Vector2(x, PartyFigureGroundY + size.y * 0.5f);
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
                if (ShouldHideBattleSupportMember(member))
                {
                    continue;
                }

                PartyVisualSlot slot = partyVisualSlots[visualIndex];
                slot.Member = member;
                slot.IdleIndex = 0;
                slot.IdleTimer = 0f;
                slot.Image.sprite = PartyAnimationLibrary.GetIdlePreview(member);
                AlignFigureToGround(slot.Image, GetPartyFigureX(visualIndex));
                slot.Image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                slot.Image.gameObject.SetActive(slot.Image.sprite != null);
                visualIndex++;
            }

            for (int i = visualIndex; i < partyVisualSlots.Count; i++)
            {
                partyVisualSlots[i].Member = null;
                partyVisualSlots[i].Image.gameObject.SetActive(false);
            }
        }

        private static bool ShouldHideBattleSupportMember(PartyMember member)
        {
            return member == null
                || member.Name == GameText.HunterName;
        }

        private bool TryGetPartySlot(PartyMember member, out PartyVisualSlot slot)
        {
            for (int i = 0; i < partyVisualSlots.Count; i++)
            {
                if (partyVisualSlots[i].Member == member && partyVisualSlots[i].Image != null && partyVisualSlots[i].Image.gameObject.activeSelf)
                {
                    slot = partyVisualSlots[i];
                    return true;
                }
            }

            slot = null;
            return false;
        }

        private Vector2 GetPartySkillMotion(PartyVisualSlot slot, int targetEnemyIndex)
        {
            if (slot?.Image == null || !TryGetSlot(targetEnemyIndex, out EnemyVisualSlot enemySlot))
            {
                return new Vector2(-38f, 0f);
            }

            Vector2 destination = enemySlot.HomePosition + new Vector2(116f, -2f);
            Vector2 motion = destination - slot.Image.rectTransform.anchoredPosition;
            motion.x = Mathf.Clamp(motion.x, -260f, -34f);
            motion.y = Mathf.Clamp(motion.y, -18f, 26f);
            return motion;
        }

        private void UpdatePartyIdleFigures()
        {
            // 队友素材的 idle 帧裁切尺寸不完全一致，循环播放会像角色在变大缩小。
            // 战斗待机阶段先固定使用预览站姿，技能释放时再播放动作序列帧。
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
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelStrongColor);
            image.color = JrpgPanelStrongColor;
            image.type = Image.Type.Simple;
            AddOutline(image, JrpgBorderColor, new Vector2(1f, -1f));

            Image accent = CreateImage(label + " Accent", buttonObject.transform, new Vector2(4f, 20f), new Vector2(0f, -4f), action == TurnBattleAction.FlameSign ? JrpgGoldColor : JrpgRuleColor);
            accent.sprite = WitcherSpriteLibrary.GetSolidSprite(accent.color);
            accent.raycastTarget = false;

            Image keyBack = CreateImage(label + " Key", buttonObject.transform, new Vector2(30f, 24f), new Vector2(3f, -2f), JrpgSelectedColor);
            keyBack.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgSelectedColor);
            AddOutline(keyBack, JrpgBorderColor, new Vector2(1f, -1f));

            Text keyText = CreateText(label + " Key Text", keyBack.transform, label.Substring(0, 1), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30f, 24f));
            keyText.color = JrpgTextColor;
            AddOutline(keyText, Color.black, new Vector2(2f, -2f));

            Text nameText = CreateText(label + " Name", buttonObject.transform, GetCommandDisplayName(action), 15, TextAnchor.MiddleLeft, new Vector2(42f, -2f), new Vector2(86f, 22f));
            nameText.color = action == TurnBattleAction.FlameSign ? JrpgGoldColor : JrpgTextColor;
            AddOutline(nameText, Color.black, new Vector2(2f, -2f));

            Text costText = CreateText(label + " Cost", buttonObject.transform, GetCommandCostLabel(action), 13, TextAnchor.MiddleRight, new Vector2(126f, -2f), new Vector2(56f, 22f));
            costText.color = JrpgMutedTextColor;
            AddOutline(costText, Color.black, new Vector2(1f, -1f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => manager.SelectAction(action));
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(255, 255, 255, 255);
            colors.highlightedColor = new Color32(118, 148, 82, 255);
            colors.pressedColor = new Color32(255, 226, 136, 255);
            colors.disabledColor = new Color32(78, 78, 78, 118);
            button.colors = colors;
            commandButtons.Add(button);
            commandButtonImages.Add(image);
        }

        private void BuildSkillPanel(Transform parent)
        {
            skillButtons.Clear();
            skillTitleTexts.Clear();
            skillDescriptionTexts.Clear();
            skillCostTexts.Clear();
            Image panelImage = CreateImage("Battle Skill Panel", parent, new Vector2(398f, 112f), new Vector2(18f, -4f), JrpgPanelStrongColor);
            panelImage.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelStrongColor);
            AddOutline(panelImage, JrpgBorderColor, new Vector2(2f, -2f));
            skillPanel = panelImage.gameObject;

            Text title = CreateText("Battle Skill Panel Title", skillPanel.transform, GameText.Battle.CharacterSkills, 17, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(112f, 24f));
            title.color = JrpgTextColor;
            AddOutline(title, Color.black, new Vector2(2f, -2f));

            Text hint = CreateText("Battle Skill Panel Hint", skillPanel.transform, GameText.Battle.BackHint, 13, TextAnchor.MiddleRight, new Vector2(300f, -10f), new Vector2(78f, 20f));
            hint.color = JrpgMutedTextColor;
            AddOutline(hint, Color.black, new Vector2(1f, -1f));

            AddSkillButton(skillPanel.transform, 0, new Vector2(12f, -34f));
            AddSkillButton(skillPanel.transform, 1, new Vector2(204f, -34f));
            AddSkillButton(skillPanel.transform, 2, new Vector2(12f, -72f));
            AddSkillButton(skillPanel.transform, 3, new Vector2(204f, -72f));
            skillPanel.SetActive(false);
        }

        private void AddSkillButton(Transform parent, int slotIndex, Vector2 position)
        {
            GameObject buttonObject = CreateUiObject($"Skill Slot {slotIndex + 1} Button", parent, new Vector2(178f, 30f), position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(JrpgPanelColor);
            image.color = JrpgPanelColor;
            AddOutline(image, JrpgBorderColor, new Vector2(1f, -1f));

            Text titleText = CreateText($"Skill Slot {slotIndex + 1} Title", buttonObject.transform, string.Empty, 13, TextAnchor.MiddleLeft, new Vector2(8f, -3f), new Vector2(76f, 20f));
            titleText.color = JrpgTextColor;
            AddOutline(titleText, Color.black, new Vector2(2f, -2f));
            skillTitleTexts.Add(titleText);

            Text descriptionText = CreateText($"Skill Slot {slotIndex + 1} Desc", buttonObject.transform, string.Empty, 11, TextAnchor.MiddleLeft, new Vector2(84f, -3f), new Vector2(52f, 20f));
            descriptionText.color = JrpgMutedTextColor;
            AddOutline(descriptionText, Color.black, new Vector2(1f, -1f));
            skillDescriptionTexts.Add(descriptionText);

            Text costText = CreateText($"Skill Slot {slotIndex + 1} Cost", buttonObject.transform, string.Empty, 11, TextAnchor.MiddleRight, new Vector2(130f, -3f), new Vector2(42f, 20f));
            costText.color = JrpgGoldColor;
            AddOutline(costText, Color.black, new Vector2(1f, -1f));
            skillCostTexts.Add(costText);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            int capturedSlot = slotIndex;
            button.onClick.AddListener(() => manager.SelectSkillSlot(capturedSlot));
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(118, 148, 82, 255);
            colors.pressedColor = new Color32(255, 226, 136, 255);
            colors.disabledColor = new Color32(78, 78, 78, 118);
            button.colors = colors;
            skillButtons.Add(button);
        }

        private static Color32 GetSkillNameColor(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.FlameSign:
                    return JrpgGoldColor;
                case BattleSkillId.TrissFirebolt:
                case BattleSkillId.TrissMeltingSigil:
                case BattleSkillId.TrissFlameWard:
                case BattleSkillId.TrissMeteorFlare:
                    return new Color32(255, 188, 112, 255);
                case BattleSkillId.YenneferArcaneBolt:
                case BattleSkillId.YenneferCursePulse:
                case BattleSkillId.YenneferAegis:
                case BattleSkillId.YenneferObsidianStorm:
                    return new Color32(205, 170, 255, 255);
                default:
                    return JrpgTextColor;
            }
        }

        private static string GetSkillShortDescription(SkillDefinition skill)
        {
            switch (skill.Id)
            {
                case BattleSkillId.ExecuteSlash:
                    return GameText.Battle.ComboSlashShort;
                case BattleSkillId.FlameSign:
                    return GameText.Battle.GroupFlameShort;
                case BattleSkillId.ThunderSign:
                    return GameText.Battle.DoubleLightningShort;
                case BattleSkillId.HunterFocus:
                    return GameText.Battle.AttackUpShort;
                case BattleSkillId.TrissFirebolt:
                    return GameText.Battle.SingleFlameShort;
                case BattleSkillId.TrissMeltingSigil:
                    return GameText.Battle.GroupDebuffShort;
                case BattleSkillId.TrissFlameWard:
                    return GameText.Battle.FrontShieldShort;
                case BattleSkillId.TrissMeteorFlare:
                    return GameText.Battle.FireGroupShort;
                case BattleSkillId.YenneferArcaneBolt:
                    return GameText.Battle.ArcaneSingleShort;
                case BattleSkillId.YenneferCursePulse:
                    return GameText.Battle.GroupDebuffShort;
                case BattleSkillId.YenneferAegis:
                    return GameText.Battle.FrontShieldShort;
                case BattleSkillId.YenneferObsidianStorm:
                    return GameText.Battle.ArcaneGroupShort;
                default:
                    return skill.TargetKind == BattleSkillTargetKind.AllLivingEnemies ? GameText.Battle.GroupTargetShort : GameText.Battle.SingleTargetShort;
            }
        }

        private static string GetCommandDisplayName(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.Attack:
                    return GameText.Battle.Attack;
                case TurnBattleAction.FlameSign:
                    return GameText.Battle.Skill;
                case TurnBattleAction.Defend:
                    return GameText.Battle.Defend;
                case TurnBattleAction.Item:
                    return GameText.Battle.Item;
                case TurnBattleAction.Escape:
                    return GameText.Battle.Escape;
                default:
                    return action.ToString();
            }
        }

        private static string GetCommandCostLabel(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.FlameSign:
                    return GameText.Battle.Expand;
                case TurnBattleAction.Item:
                    return GameText.Battle.Potion;
                case TurnBattleAction.Defend:
                    return GameText.Battle.Guard;
                case TurnBattleAction.Attack:
                    return GameText.Battle.Normal;
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
