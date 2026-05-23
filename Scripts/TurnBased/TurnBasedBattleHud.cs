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
        private static Sprite[] cachedFlameFrames;
        private static Sprite cachedBattleBackdrop;
        private static Sprite cachedFloorMist;
        private static Sprite cachedGroundShadow;
        private static Sprite cachedGroundGlow;
        private static Sprite[] cachedCommandButtonSprites;
        private static Sprite[] cachedGeraltIdleFrames;
        private static Sprite[] cachedGeraltSlashFrames;
        private static Sprite[] cachedGeraltHurtFrames;

        private TurnBasedBattleManager manager;
        private GeraltAnimator playerAnimator;
        private GameObject root;
        private Text messageText;
        private Text playerText;
        private Text potionText;
        private Image playerFigure;
        private Vector2 playerFigureHomePosition;
        private bool playerFigureBusy;
        private Text playerDamageText;
        private Image playerHealthFill;
        private Image playerManaFill;
        private Image flameEffect;
        private IReadOnlyList<TurnBasedEnemyState> visibleEnemies;
        private int playerIdleIndex;
        private float playerIdleTimer;

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

        private void Update()
        {
            if (root == null || !root.activeSelf || visibleEnemies == null)
            {
                return;
            }

            UpdatePlayerIdleFigure();

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
            Refresh(enemies, player, potionCount);
            SetCommandsEnabled(true);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void SetCommandsEnabled(bool enabled)
        {
            for (int i = 0; i < commandButtons.Count; i++)
            {
                commandButtons[i].interactable = enabled;
            }
        }

        public void Refresh(IReadOnlyList<TurnBasedEnemyState> enemies, GeraltController player, int potionCount)
        {
            visibleEnemies = enemies;
            if (playerText != null && player != null)
            {
                playerText.text = $"猎魔人  HP {player.CurrentHealth}/{player.MaxHealth}   MP {player.CurrentMana}/{player.MaxMana}";
                SetFillWidth(playerHealthFill, player.MaxHealth <= 0 ? 0f : (float)player.CurrentHealth / player.MaxHealth, 206f);
                SetFillWidth(playerManaFill, player.MaxMana <= 0 ? 0f : (float)player.CurrentMana / player.MaxMana, 206f);
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
                    SetFillWidth(slot.HealthFill, normalizedHealth, 106f);
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
            if (flameEffect == null)
            {
                yield break;
            }

            Sprite[] frames = LoadFlameFrames();
            Rect targetRect = GetLivingEnemyVisualRect();
            float startX = playerFigureHomePosition.x + 76f;
            float endX = Mathf.Min(targetRect.xMin - 92f, targetRect.xMax - 260f);
            float width = Mathf.Clamp(startX - endX, 360f, 720f);
            float centerX = startX - width * 0.5f;
            float centerY = Mathf.Clamp(targetRect.center.y - 4f, -12f, 122f);
            float height = Mathf.Clamp(targetRect.height * 0.86f, 96f, 180f);

            flameEffect.gameObject.SetActive(true);
            flameEffect.color = Color.white;
            flameEffect.rectTransform.anchoredPosition = new Vector2(centerX, centerY);
            flameEffect.rectTransform.sizeDelta = new Vector2(width, height);
            flameEffect.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            if (frames.Length == 0)
            {
                flameEffect.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 86, 20, 230));
                yield return new WaitForSeconds(0.28f);
                flameEffect.gameObject.SetActive(false);
                yield break;
            }

            for (int i = 0; i < frames.Length; i++)
            {
                flameEffect.sprite = frames[i];
                float t = frames.Length <= 1 ? 1f : (float)i / (frames.Length - 1);
                flameEffect.rectTransform.localScale = new Vector3(-Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(t * 1.45f)), 1f + Mathf.Sin(t * Mathf.PI) * 0.13f, 1f);
                Color color = Color.white;
                color.a = t > 0.72f ? Mathf.Lerp(1f, 0.18f, (t - 0.72f) / 0.28f) : 1f;
                flameEffect.color = color;
                yield return new WaitForSeconds(0.045f);
            }

            flameEffect.gameObject.SetActive(false);
        }

        public IEnumerator PlayPlayerAttack()
        {
            yield return PlayPlayerCast();
        }

        public IEnumerator PlayPlayerAttack(int enemyIndex)
        {
            yield return PlayPlayerFrames(GetPlayerFrames(GeraltAnimation.Slash), 0.085f, GetPlayerAttackMotion(enemyIndex), false);
        }

        public IEnumerator PlayPlayerCast()
        {
            yield return PlayPlayerFrames(GetPlayerFrames(GeraltAnimation.Slash), 0.085f, new Vector2(-54f, -6f), false);
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

            Image horizonGlow = CreateCenteredImage("Battle Horizon Glow", root.transform, new Vector2(1060f, 190f), new Vector2(0f, 78f), new Color32(42, 79, 101, 82));
            horizonGlow.sprite = GetFloorMistSprite();
            horizonGlow.raycastTarget = false;

            Image stageWash = CreateCenteredImage("Stage Wash", root.transform, new Vector2(720f, 250f), new Vector2(-108f, 42f), new Color32(183, 48, 24, 46));
            stageWash.sprite = GetFloorMistSprite();
            stageWash.raycastTarget = false;

            Image floorPlate = CreateCenteredImage("Battle Floor Plate", root.transform, new Vector2(720f, 124f), new Vector2(-116f, -20f), new Color32(10, 15, 18, 192));
            floorPlate.sprite = GetFloorMistSprite();
            floorPlate.raycastTarget = false;

            Image frontFog = CreateCenteredImage("Battle Front Fog", root.transform, new Vector2(1260f, 122f), new Vector2(0f, -84f), new Color32(92, 119, 127, 54));
            frontFog.sprite = GetFloorMistSprite();
            frontFog.raycastTarget = false;

            Image titlePlate = CreateCenteredImage("Battle Title Plate", root.transform, new Vector2(420f, 44f), new Vector2(0f, 236f), new Color32(12, 15, 19, 228));
            titlePlate.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(12, 15, 19, 228));
            AddOutline(titlePlate, new Color32(139, 101, 54, 255), new Vector2(2f, -2f));

            Text battleTitle = CreateText("Battle Title", titlePlate.transform, "遭遇战", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(420f, 44f));
            battleTitle.color = new Color32(255, 211, 123, 255);
            AddOutline(battleTitle, Color.black, new Vector2(2f, -2f));

            Image playerGlow = CreateCenteredImage("Battle Player Ground Glow", root.transform, new Vector2(196f, 48f), new Vector2(320f, 0f), new Color32(226, 70, 36, 82));
            playerGlow.sprite = GetGroundGlowSprite();
            playerGlow.raycastTarget = false;

            Image playerShadow = CreateCenteredImage("Battle Player Ground Shadow", root.transform, new Vector2(168f, 36f), new Vector2(320f, -8f), new Color32(0, 0, 0, 172));
            playerShadow.sprite = GetGroundShadowSprite();
            playerShadow.raycastTarget = false;

            playerFigure = CreateCenteredImage("Battle Player Figure", root.transform, new Vector2(176f, 194f), new Vector2(320f, 96f), Color.white);
            playerFigure.sprite = GetPlayerIdleFrame();
            playerFigureHomePosition = playerFigure.rectTransform.anchoredPosition;
            playerFigure.preserveAspect = true;
            playerFigure.raycastTarget = false;
            SetPlayerFacingScale(1f);
            playerDamageText = CreateText("Player Damage Text", playerFigure.transform, string.Empty, 34, TextAnchor.MiddleCenter, new Vector2(0f, 72f), new Vector2(170f, 56f));
            playerDamageText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            playerDamageText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            playerDamageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            playerDamageText.color = new Color32(255, 82, 74, 255);
            playerDamageText.gameObject.SetActive(false);
            AddOutline(playerDamageText, new Color32(0, 0, 0, 255), new Vector2(3f, -3f));
            SetPlayerFacingScale(1f);

            Image playerPanel = CreateImage("Player Battle Plate", root.transform, new Vector2(260f, 92f), new Vector2(670f, -12f), new Color32(7, 10, 13, 204));
            playerPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(7, 10, 13, 226));
            AddOutline(playerPanel, new Color32(151, 111, 61, 255), new Vector2(3f, -3f));

            Text playerName = CreateText("Player Battle Name", playerPanel.transform, "猎魔人", 24, TextAnchor.MiddleLeft, new Vector2(18f, -12f), new Vector2(180f, 28f));
            playerName.color = new Color32(255, 218, 138, 255);
            AddOutline(playerName, Color.black, new Vector2(1f, -1f));

            Image playerHealthBack = CreateImage("Player Battle HP Back", playerPanel.transform, new Vector2(214f, 12f), new Vector2(20f, -50f), new Color32(42, 5, 8, 245));
            playerHealthBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(42, 5, 8, 245));
            playerHealthFill = CreateImage("Player Battle HP Fill", playerHealthBack.transform, new Vector2(206f, 6f), new Vector2(4f, -3f), new Color32(221, 31, 40, 255));
            playerHealthFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            Image playerManaBack = CreateImage("Player Battle MP Back", playerPanel.transform, new Vector2(214f, 12f), new Vector2(20f, -72f), new Color32(4, 18, 52, 245));
            playerManaBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(4, 18, 52, 245));
            playerManaFill = CreateImage("Player Battle MP Fill", playerManaBack.transform, new Vector2(206f, 6f), new Vector2(4f, -3f), new Color32(36, 138, 255, 255));
            playerManaFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            playerText = CreateText("Battle Player Stats", playerPanel.transform, "HP 100/100   MP 100/100", 14, TextAnchor.MiddleLeft, new Vector2(18f, -82f), new Vector2(228f, 20f));
            playerText.color = new Color32(226, 241, 238, 255);
            AddOutline(playerText, Color.black, new Vector2(1f, -1f));

            enemySlots.Clear();
            enemyRows.Clear();
            for (int i = 0; i < 4; i++)
            {
                Vector2 slotPosition = new Vector2(-350f + i * 120f, 82f + (i % 2) * 18f);
                Image groundGlow = CreateCenteredImage($"Battle Enemy Ground Glow {i + 1}", root.transform, new Vector2(190f, 46f), slotPosition + new Vector2(0f, -69f), new Color32(226, 72, 34, 72));
                groundGlow.sprite = GetGroundGlowSprite();
                groundGlow.raycastTarget = false;
                groundGlow.gameObject.SetActive(false);

                Image groundShadow = CreateCenteredImage($"Battle Enemy Ground Shadow {i + 1}", root.transform, new Vector2(150f, 34f), slotPosition + new Vector2(0f, -75f), new Color32(0, 0, 0, 164));
                groundShadow.sprite = GetGroundShadowSprite();
                groundShadow.raycastTarget = false;
                groundShadow.gameObject.SetActive(false);

                Image targetReticle = CreateCenteredImage($"Battle Target Reticle {i + 1}", root.transform, new Vector2(174f, 56f), slotPosition + new Vector2(0f, -65f), new Color32(255, 160, 62, 0));
                targetReticle.sprite = GetGroundGlowSprite();
                targetReticle.raycastTarget = false;
                targetReticle.gameObject.SetActive(false);

                Image enemyImage = CreateCenteredImage($"Battle Enemy Sprite {i + 1}", root.transform, new Vector2(182f, 182f), slotPosition, Color.white);
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

                Image healthBack = CreateCenteredImage($"Battle Enemy HP Back {i + 1}", enemyImage.transform, new Vector2(116f, 12f), new Vector2(0f, -64f), new Color32(12, 6, 7, 230));
                healthBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(12, 6, 7, 230));
                healthBack.raycastTarget = false;
                AddOutline(healthBack, new Color32(0, 0, 0, 220), new Vector2(1f, -1f));

                GameObject healthFillObject = CreateUiObject($"Battle Enemy HP Fill {i + 1}", healthBack.transform, new Vector2(106f, 5f), new Vector2(-53f, 0f), new Vector2(0.5f, 0.5f));
                RectTransform healthFillRect = healthFillObject.GetComponent<RectTransform>();
                healthFillRect.pivot = new Vector2(0f, 0.5f);
                Image healthFill = healthFillObject.AddComponent<Image>();
                healthFill.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(226, 34, 43, 255));
                healthFill.color = new Color32(226, 34, 43, 255);
                healthFill.raycastTarget = false;
                Image nameplateBack = CreateCenteredImage($"Battle Enemy Nameplate {i + 1}", root.transform, new Vector2(124f, 46f), slotPosition + new Vector2(0f, -102f), new Color32(8, 10, 13, 226));
                nameplateBack.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 10, 13, 226));
                nameplateBack.raycastTarget = false;
                nameplateBack.gameObject.SetActive(false);
                AddOutline(nameplateBack, new Color32(122, 72, 43, 255), new Vector2(2f, -2f));

                Text row = CreateText($"Enemy Stage Label {i + 1}", nameplateBack.transform, string.Empty, 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(118f, 42f));
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

            Image commandPanel = CreateImage("Command Panel", root.transform, new Vector2(238f, 414f), new Vector2(694f, -108f), new Color32(10, 13, 18, 216));
            commandPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(10, 13, 18, 244));
            AddOutline(commandPanel, new Color32(151, 111, 61, 255), new Vector2(3f, -3f));

            Text commandTitle = CreateText("Battle Command Title", commandPanel.transform, "行动", 20, TextAnchor.MiddleLeft, new Vector2(24f, -14f), new Vector2(120f, 28f));
            commandTitle.color = new Color32(255, 214, 132, 255);
            AddOutline(commandTitle, Color.black, new Vector2(1f, -1f));

            potionText = CreateText("Battle Potion Count", commandPanel.transform, "药剂 x3", 16, TextAnchor.MiddleRight, new Vector2(34f, -366f), new Vector2(176f, 28f));
            potionText.color = new Color32(183, 219, 255, 255);
            AddOutline(potionText, Color.black, new Vector2(1f, -1f));

            messageText = CreateText("Battle Message", commandPanel.transform, "选择行动。", 16, TextAnchor.UpperLeft, new Vector2(24f, -42f), new Vector2(190f, 48f));
            messageText.color = new Color32(255, 246, 214, 255);
            AddOutline(messageText, Color.black, new Vector2(1f, -1f));

            commandButtons.Clear();
            AddCommandButton(commandPanel.transform, "1 攻击", TurnBattleAction.Attack, new Vector2(20f, -96f));
            AddCommandButton(commandPanel.transform, "2 火焰", TurnBattleAction.FlameSign, new Vector2(122f, -96f));
            AddCommandButton(commandPanel.transform, "3 防御", TurnBattleAction.Defend, new Vector2(20f, -224f));
            AddCommandButton(commandPanel.transform, "4 物品", TurnBattleAction.Item, new Vector2(122f, -224f));
            AddCommandButton(commandPanel.transform, "5 逃跑", TurnBattleAction.Escape, new Vector2(71f, -322f));

            root.SetActive(false);
        }

        private IEnumerator PlayEnemyFrames(EnemyVisualSlot slot, Sprite[] frames, Sprite fallback, float frameDuration, bool attackMotion, bool hurtMotion)
        {
            slot.Busy = true;
            Coroutine targetPulse = hurtMotion ? StartCoroutine(PlayTargetPulse(slot)) : null;
            Vector2 home = slot.HomePosition;
            Vector2 motion = attackMotion ? GetEnemyAttackMotion(slot) : new Vector2(24f, 0f);
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

        // 中文说明：战斗舞台的敌人固定站在左侧，因此统一朝向右侧的玩家。
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

        // 中文说明：玩家固定站在右侧，战斗立绘需要翻向左侧敌人。
        private void SetPlayerFacingScale(float scale)
        {
            float safeScale = Mathf.Max(0.01f, scale);
            playerFigure.rectTransform.localScale = new Vector3(-safeScale, safeScale, 1f);
            if (playerDamageText != null && !playerDamageText.gameObject.activeSelf)
            {
                playerDamageText.rectTransform.localScale = GetCounterFacingScale(1f);
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

        private static Sprite[] LoadFlameFrames()
        {
            if (cachedFlameFrames != null)
            {
                return cachedFlameFrames;
            }

            string absolutePath = Path.Combine(Application.dataPath, "Art/Effects/HunterFlameBeamSheet.png");
            if (!File.Exists(absolutePath))
            {
                cachedFlameFrames = System.Array.Empty<Sprite>();
                return cachedFlameFrames;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                cachedFlameFrames = System.Array.Empty<Sprite>();
                return cachedFlameFrames;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            const int columns = 4;
            const int rows = 4;
            int frameWidth = texture.width / columns;
            int frameHeight = texture.height / rows;
            cachedFlameFrames = new Sprite[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    Rect rect = new Rect(column * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    cachedFlameFrames[index] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 256f);
                    cachedFlameFrames[index].name = $"TurnBattleFlame_{index:00}";
                }
            }

            return cachedFlameFrames;
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
                    return LoadGeraltIdleFrames();
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
            string folderPath = Path.Combine(Application.dataPath, "Art/Geralt/Frames", animation.ToString());
            if (!Directory.Exists(folderPath))
            {
                return CreateGeraltFallbackFrames(animation);
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

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite frame = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f),
                    96f);
                frame.name = Path.GetFileNameWithoutExtension(filePaths[i]);
                frames.Add(frame);
            }

            return frames.Count > 0 ? frames.ToArray() : CreateGeraltFallbackFrames(animation);
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
            Vector2 buttonSize = new Vector2(92f, 122f);
            GameObject buttonObject = CreateUiObject(label + " Button", parent, buttonSize, position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = GetCommandButtonSprite(action);
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => manager.SelectAction(action));
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(255, 255, 255, 255);
            colors.highlightedColor = new Color32(255, 224, 150, 255);
            colors.pressedColor = new Color32(202, 92, 54, 255);
            colors.disabledColor = new Color32(78, 78, 78, 150);
            button.colors = colors;
            commandButtons.Add(button);
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
